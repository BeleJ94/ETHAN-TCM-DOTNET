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

        var normalizedRoles = NormalizeRoles(applicationRoles);
        if (normalizedRoles.Count == 0)
        {
            normalizedRoles.Add(authenticationOptions.Value.ActiveDirectory.DefaultRole);
        }

        var roleEntities = await EnsureRolesAsync(normalizedRoles, now, cancellationToken);

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

        var requiredRoleIds = roleEntities.Select(role => role.Id).Distinct().ToArray();
        var currentRoleIds = user.Roles.Select(role => role.RoleId).Distinct().ToArray();
        var profileChanged = user.DisplayName != displayName ||
            user.Email != email ||
            user.ExternalId != externalId ||
            user.DepartmentId is not null ||
            !user.IsActive;
        var rolesChanged = currentRoleIds.Length != requiredRoleIds.Length ||
            currentRoleIds.Any(roleId => !requiredRoleIds.Contains(roleId));

        if (userWasCreated || profileChanged)
        {
            user.UpdateProfile(displayName, email, externalId, departmentId: null, now);
        }

        if (userWasCreated || rolesChanged)
        {
            user.SynchronizeRoles(requiredRoleIds, now);
        }

        if (userWasCreated || profileChanged || rolesChanged)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var identity = new System.Security.Claims.ClaimsIdentity("EthanTcmUserSync");
        identity.AddClaim(new System.Security.Claims.Claim(EthanTcmClaimTypes.UserId, user.Id.ToString()));
        identity.AddClaim(new System.Security.Claims.Claim(EthanTcmClaimTypes.Login, user.Login));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Login));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Email, user.Email));

        foreach (var role in roleEntities)
        {
            identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role.Code));
        }

        return identity;
    }

    private async Task<List<Role>> EnsureRolesAsync(
        IReadOnlyCollection<string> roleCodes,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var existingRoles = await dbContext.Roles
            .Where(role => roleCodes.Contains(role.Code))
            .ToListAsync(cancellationToken);

        foreach (var roleCode in roleCodes.Where(roleCode => existingRoles.All(role => role.Code != roleCode)))
        {
            var role = new Role(roleCode, ToDisplayName(roleCode), "Application role synchronized from authentication.");
            role.MarkCreatedBy(Guid.Empty);
            dbContext.Roles.Add(role);
            existingRoles.Add(role);
        }

        return existingRoles;
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

    private static string ToDisplayName(string roleCode)
    {
        return roleCode switch
        {
            ApplicationRoles.TaxManager => "Tax Manager",
            ApplicationRoles.FinanceManager => "Finance Manager",
            ApplicationRoles.ReadOnly => "Read Only",
            _ => roleCode
        };
    }
}
