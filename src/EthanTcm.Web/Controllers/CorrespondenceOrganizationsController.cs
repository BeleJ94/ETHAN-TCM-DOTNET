using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;
[Authorize(Policy = ApplicationPermissions.AssignCorrespondence)]
public sealed class CorrespondenceOrganizationsController(ICorrespondenceOrganizationService service) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(new CorrespondenceOrganizationViewModel { Items = await service.ListAsync(ct) });
    [HttpPost] public async Task<IActionResult> Save(CorrespondenceOrganizationViewModel model, CancellationToken ct) { var ajax=string.Equals(Request.Headers["X-Requested-With"],"XMLHttpRequest",StringComparison.OrdinalIgnoreCase);if (!ModelState.IsValid) { if(ajax)return BadRequest(new{success=false,message="Code and organisation name are required."});model.Items=await service.ListAsync(ct); return View("Index",model); } var r=await service.SaveAsync(model.Id,model.Code,model.Name,model.ContactEmail,model.Address,ct);var message=r.Success?"Organisation saved.":r.Message;TempData["StatusMessage"]=message;if(ajax)return Json(new{success=r.Success,message,refreshUrl=Url.Action(nameof(Index))});return RedirectToAction(nameof(Index)); }
}
