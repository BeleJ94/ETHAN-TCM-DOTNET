using EthanTcm.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[AllowAnonymous]
public sealed class DevUsersController(IWebHostEnvironment environment) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Switch(string profileKey, string? returnUrl)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var profile = DevelopmentUserProfiles.Find(profileKey);
        if (profile is null)
        {
            return BadRequest("Unknown development user profile.");
        }

        Response.Cookies.Append(
            DevelopmentUserProfiles.CookieName,
            profile.Key,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
