using EthanTcm.Application.Authentication;
using Microsoft.AspNetCore.Localization;

namespace EthanTcm.Web.Localization;

public sealed class UserClaimRequestCultureProvider : RequestCultureProvider
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "fr"
    };

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var culture = httpContext.User.FindFirst(EthanTcmClaimTypes.PreferredCulture)?.Value;
        if (string.IsNullOrWhiteSpace(culture) || !SupportedCultures.Contains(culture))
        {
            return NullProviderCultureResult;
        }

        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
    }
}
