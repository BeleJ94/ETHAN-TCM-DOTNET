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
    [HttpPost] public async Task<IActionResult> Save(CorrespondenceOrganizationViewModel model, CancellationToken ct) { if (!ModelState.IsValid) { model.Items=await service.ListAsync(ct); return View("Index",model); } var r=await service.SaveAsync(model.Id,model.Code,model.Name,model.ContactEmail,model.Address,ct); TempData["StatusMessage"]=r.Success?"Organisation saved.":r.Message; return RedirectToAction(nameof(Index)); }
}
