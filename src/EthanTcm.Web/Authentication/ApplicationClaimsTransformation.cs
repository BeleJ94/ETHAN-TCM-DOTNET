using System.Security.Claims;
using System.Security.Principal;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EthanTcm.Web.Authentication;

public sealed class ApplicationClaimsTransformation(
    IActiveDirectoryUserSyncService userSyncService,
    IOptions<EthanTcmAuthenticationOptions> authenticationOptions)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            principal.HasClaim(claim => claim.Type == EthanTcmClaimTypes.UserId))
        {
            return principal;
        }

        var applicationRoles = ResolveApplicationRoles(principal);
        var synchronizedIdentity = await userSyncService.SynchronizeAsync(principal, applicationRoles);

        if (synchronizedIdentity is not null)
        {
            principal.AddIdentity(synchronizedIdentity);
        }

        return principal;
    }

    private IReadOnlyCollection<string> ResolveApplicationRoles(ClaimsPrincipal principal)
    {
        if (authenticationOptions.Value.Mode == AuthMode.LocalAuth)
        {
            var localRoles = principal.Claims
                .Where(claim => claim.Type == ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return localRoles.Length > 0 ? localRoles : authenticationOptions.Value.LocalAuth.Roles;
        }

        var mappedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groupValues = principal.Claims
            .Where(claim =>
                claim.Type == ClaimTypes.GroupSid ||
                claim.Type == ClaimTypes.Role ||
                claim.Type.Equals("groups", StringComparison.OrdinalIgnoreCase) ||
                claim.Type.Equals("group", StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value);

        foreach (var groupValue in groupValues)
        {
            var normalizedGroupValue = NormalizeGroupClaimValue(groupValue);
            if (TryResolveMappedRoles(principal, groupValue, normalizedGroupValue, out var roles))
            {
                foreach (var role in roles)
                {
                    mappedRoles.Add(role);
                }
            }
        }

        foreach (var mapping in authenticationOptions.Value.ActiveDirectory.GroupRoleMappings)
        {
            if (!principal.IsInRole(mapping.Key))
            {
                continue;
            }

            foreach (var role in mapping.Value)
            {
                mappedRoles.Add(role);
            }
        }

        if (mappedRoles.Count == 0)
        {
            mappedRoles.Add(authenticationOptions.Value.ActiveDirectory.DefaultRole);
        }

        return mappedRoles;
    }

    private bool TryResolveMappedRoles(
        ClaimsPrincipal principal,
        string groupValue,
        string? normalizedGroupValue,
        out string[] roles)
    {
        foreach (var mapping in authenticationOptions.Value.ActiveDirectory.GroupRoleMappings)
        {
            if (mapping.Key.Equals(groupValue, StringComparison.OrdinalIgnoreCase) ||
                (normalizedGroupValue is not null && mapping.Key.Equals(normalizedGroupValue, StringComparison.OrdinalIgnoreCase)) ||
                principal.IsInRole(mapping.Key))
            {
                roles = mapping.Value;
                return true;
            }
        }

        roles = [];
        return false;
    }

    private static string? NormalizeGroupClaimValue(string groupValue)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            var sid = new SecurityIdentifier(groupValue);
            return sid.Translate(typeof(NTAccount)).Value;
        }
        catch (Exception ex) when (ex is ArgumentException or IdentityNotMappedException or SystemException)
        {
            return null;
        }
    }
}
