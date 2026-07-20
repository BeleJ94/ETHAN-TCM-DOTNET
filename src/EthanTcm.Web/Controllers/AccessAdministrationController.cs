using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ViewUserAdministration)]
public sealed class AccessAdministrationController(IAccessAdministrationService service) : Controller
{
    public async Task<IActionResult> Index(string? search, bool? active, int page = 1, CancellationToken ct = default)
        => View(new AccessUsersViewModel { Search = search, Active = active, Page = await service.SearchUsersAsync(search, active, page, 20, ct) });

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var user = await service.GetUserAsync(id, ct); if (user is null) return NotFound();
        return View(new AccessUserEditViewModel { User = user, IsActive = user.IsActive, RoleIds = user.Roles.Where(x => x.Selected).Select(x => x.Id).ToArray() });
    }

    [HttpPost, Authorize(Policy = ApplicationPermissions.ManageUsers)]
    public async Task<IActionResult> UpdateUser(AccessUserEditViewModel model, CancellationToken ct)
    {
        var result = await service.UpdateUserAsync(model.User.Id, model.IsActive, model.RoleIds, model.Reason, ct);
        TempData["StatusMessage"] = result.Message; TempData["StatusType"] = result.Success ? "success" : "danger";
        return RedirectToAction(nameof(Details), new { id = model.User.Id });
    }

    public async Task<IActionResult> Roles(CancellationToken ct) => View(new AccessRolesViewModel { Roles = await service.GetRoleMatrixAsync(ct) });

    [HttpPost, Authorize(Policy = ApplicationPermissions.ManageRoles)]
    public async Task<IActionResult> UpdateRole(AccessRoleEditViewModel model, CancellationToken ct)
    {
        var result = await service.UpdateRolePermissionsAsync(model.RoleId, model.PermissionIds, model.Reason, ct);
        TempData["StatusMessage"] = result.Message; TempData["StatusType"] = result.Success ? "success" : "danger";
        return RedirectToAction(nameof(Roles), new { roleId = model.RoleId });
    }
}
