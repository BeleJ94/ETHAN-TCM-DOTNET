using System.Security.Claims;
using System.Text.Encodings.Web;
using EthanTcm.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EthanTcm.Web.Authentication;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptions<EthanTcmAuthenticationOptions> authenticationOptions,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var selectedProfile = DevelopmentUserProfiles.Find(
            Context.Request.Cookies.TryGetValue(DevelopmentUserProfiles.CookieName, out var profileKey)
                ? profileKey
                : null);
        var localUser = authenticationOptions.Value.LocalAuth;
        var login = selectedProfile?.Login ?? localUser.Login;
        var displayName = selectedProfile?.DisplayName ?? localUser.DisplayName;
        var email = selectedProfile?.Email ?? localUser.Email;
        var roles = selectedProfile?.Roles ?? localUser.Roles;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, login),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email),
            new Claim(EthanTcmClaimTypes.Login, login)
        }.Concat(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
