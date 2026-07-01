using EthanTcm.Application.Abstractions;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize]
public sealed class AccountController(ICurrentUserService currentUserService) : Controller
{
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
