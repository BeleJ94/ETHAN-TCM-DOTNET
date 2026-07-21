using System.Security.Claims;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EthanTcm.Infrastructure.Services;

public sealed class ActiveDirectoryUserSyncService(
    EthanTcmDbContext dbContext,
    IAccessAdministrationService accessAdministrationService,
    IOptions<EthanTcmAuthenticationOptions> authenticationOptions)
    : IActiveDirectoryUserSyncService
{
    public async Task<ClaimsIdentity?> SynchronizeAsync(
        ClaimsPrincipal principal,
        IReadOnlyCollection<string> applicationRoles,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var login = NormalizeLogin(FindClaimValue(principal, EthanTcmClaimTypes.Login))
            ?? NormalizeLogin(FindClaimValue(principal, ClaimTypes.NameIdentifier))
            ?? NormalizeLogin(principal.Identity.Name);

        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        var displayName = FindClaimValue(principal, ClaimTypes.Name)
            ?? FindClaimValue(principal, "displayName")
            ?? login;
        var email = FindClaimValue(principal, ClaimTypes.Email)
            ?? FindClaimValue(principal, "mail")
            ?? $"{login}@local";
        var externalId = FindClaimValue(principal, ClaimTypes.Sid)
            ?? FindClaimValue(principal, "objectSid")
            ?? FindClaimValue(principal, ClaimTypes.NameIdentifier);
        var now = DateTimeOffset.UtcNow;

        await accessAdministrationService.EnsureCatalogAsync(cancellationToken);

        var user = await dbContext.Users
            .Include(current => current.Roles)
            .FirstOrDefaultAsync(current => current.Login == login, cancellationToken);

        var userWasCreated = false;
        if (user is null)
        {
            if (!authenticationOptions.Value.ActiveDirectory.AutoProvisionUsers)
            {
                return null;
            }

            user = new User(login, displayName, email);
            dbContext.Users.Add(user);
            userWasCreated = true;
        }

        var profileChanged = user.DisplayName != displayName ||
            user.Email != email ||
            user.ExternalId != externalId ||
            user.DepartmentId is not null;

        if (userWasCreated || profileChanged)
        {
            user.SynchronizeIdentity(displayName, email, externalId, now);
        }

        if (userWasCreated)
        {
            var initialRoles = NormalizeRoles(applicationRoles);
            if (initialRoles.Count == 0) initialRoles.Add(authenticationOptions.Value.ActiveDirectory.DefaultRole);
            var initialRoleIds = await dbContext.Roles.Where(role => initialRoles.Contains(role.Code)).Select(role => role.Id).ToListAsync(cancellationToken);
            user.ReplaceRoles(initialRoleIds, now);
        }

        if (userWasCreated || profileChanged)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!user.IsActive) return null;

        var roleEntities = await (from userRole in dbContext.UserRoles
                                  join role in dbContext.Roles on userRole.RoleId equals role.Id
                                  where userRole.UserId == user.Id && role.IsActive
                                  select role).ToListAsync(cancellationToken);
        var permissionCodes = await (from userRole in dbContext.UserRoles
                                     join rolePermission in dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                                     join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
                                     where userRole.UserId == user.Id && permission.IsActive
                                     select permission.Code).Distinct().ToListAsync(cancellationToken);

        var identity = new System.Security.Claims.ClaimsIdentity("EthanTcmUserSync");
        identity.AddClaim(new System.Security.Claims.Claim(EthanTcmClaimTypes.UserId, user.Id.ToString()));
        identity.AddClaim(new System.Security.Claims.Claim(EthanTcmClaimTypes.Login, user.Login));
        identity.AddClaim(new System.Security.Claims.Claim(EthanTcmClaimTypes.PreferredCulture, user.PreferredCulture));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Login));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Email, user.Email));

        foreach (var role in roleEntities)
        {
            identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role.Code));
        }
        foreach (var permission in permissionCodes)
        {
            identity.AddClaim(new System.Security.Claims.Claim(EthanTcmClaimTypes.Permission, permission));
        }

        return identity;
    }

    private static HashSet<string> NormalizeRoles(IEnumerable<string> roles)
    {
        var validRoles = new HashSet<string>(ApplicationRoles.All, StringComparer.OrdinalIgnoreCase);
        var normalizedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles.Where(role => validRoles.Contains(role)))
        {
            normalizedRoles.Add(ApplicationRoles.All.First(knownRole => knownRole.Equals(role, StringComparison.OrdinalIgnoreCase)));
        }

        return normalizedRoles;
    }

    private static string? FindClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
    }

    private static string? NormalizeLogin(string? login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        return login.Trim();
    }

}
