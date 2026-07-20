using EthanTcm.Application.Abstractions;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize]
public sealed class AccountController(ICurrentUserService currentUserService) : Controller
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult AccessDenied(int statusCode = StatusCodes.Status403Forbidden)
    {
        var reExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var requestedPath = reExecuteFeature is null
            ? string.Empty
            : $"{reExecuteFeature.OriginalPathBase}{reExecuteFeature.OriginalPath}";

        var effectiveStatusCode = statusCode is >= 400 and <= 599
            ? statusCode
            : StatusCodes.Status500InternalServerError;

        Response.StatusCode = effectiveStatusCode;

        return View(new AccessDeniedViewModel
        {
            StatusCode = effectiveStatusCode,
            RequestedPath = requestedPath,
            TraceIdentifier = HttpContext.TraceIdentifier,
            DisplayName = currentUserService.DisplayName ?? currentUserService.Login ?? string.Empty,
            Roles = currentUserService.Roles.Order(StringComparer.OrdinalIgnoreCase).ToArray()
        });
    }

    public IActionResult Profile()
    {
        var model = new AccountProfileViewModel
        {
            Login = currentUserService.Login ?? string.Empty,
            DisplayName = currentUserService.DisplayName ?? currentUserService.Login ?? string.Empty,
            Email = currentUserService.Email,
            IsAuthenticated = currentUserService.IsAuthenticated,
            IsActive = currentUserService.IsActive,
            Roles = currentUserService.Roles.Order(StringComparer.OrdinalIgnoreCase).ToArray()
        };

        return View(model);
    }
}
