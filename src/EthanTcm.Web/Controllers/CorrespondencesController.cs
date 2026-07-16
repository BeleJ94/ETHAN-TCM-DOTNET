using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Enums;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ViewCorrespondence)]
public sealed class CorrespondencesController(ICorrespondenceService service) : Controller
{
    public async Task<IActionResult> Index(string? search, CorrespondenceDirection? direction, CorrespondenceStatus? status, bool myItems = false, int page = 1, CancellationToken ct = default)
        => View(new CorrespondenceIndexViewModel { Search = search, Direction = direction, Status = status, MyItems = myItems, Dashboard = await service.GetDashboardAsync(ct), Page = await service.SearchAsync(new(search, direction, status, myItems, page), ct) });
    public async Task<IActionResult> Details(Guid id, CancellationToken ct) { var item = await service.GetAsync(id, ct); if (item is null) return NotFound(); var users = await service.GetUsersAsync(ct); return View(new CorrespondenceDetailsViewModel { Item = item, Users = users.Select(x => new SelectListItem(x.Label, x.Id.ToString())).ToArray() }); }
    [Authorize(Policy = ApplicationPermissions.CreateCorrespondence)] public async Task<IActionResult> Create(CorrespondenceDirection direction = CorrespondenceDirection.Incoming, CancellationToken ct = default) => View(await WithReferences(new CorrespondenceCreateViewModel { Direction = direction }, ct));
    [HttpPost, Authorize(Policy = ApplicationPermissions.CreateCorrespondence)]
    public async Task<IActionResult> Create(CorrespondenceCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(await WithReferences(model, ct));
        var creation = await service.CreateAsync(new(
            model.Direction, model.Subject, model.BusinessReference, model.Summary,
            model.CorrespondenceDate, model.Priority, model.Confidentiality, model.Channel,
            model.SenderName, model.SenderOrganization, model.RecipientName, model.RecipientOrganization,
            model.CorrespondentOrganizationId, model.InternalDepartmentId,
            model.ParentCorrespondenceId, model.TaxDeclarationId, model.TaxObligationId), ct);
        if (!creation.Success || !creation.Id.HasValue)
        {
            ModelState.AddModelError(string.Empty, creation.Message ?? "The correspondence could not be created.");
            return View(await WithReferences(model, ct));
        }
        var registration = await service.RegisterAsync(creation.Id.Value, ct);
        TempData["StatusMessage"] = registration.Success
            ? "Correspondence created. Assign an owner and a due date to continue."
            : registration.Message ?? "The correspondence was created as a draft but could not be registered.";
        TempData["StatusType"] = registration.Success ? "success" : "warning";
        return RedirectToAction(nameof(Details), new { id = creation.Id.Value });
    }
    [HttpPost, Authorize(Policy = ApplicationPermissions.CreateCorrespondence)] public Task<IActionResult> Register(CorrespondenceActionViewModel m, CancellationToken ct) => Respond(m.Id, service.RegisterAsync(m.Id,ct));
    [HttpPost, Authorize(Policy = ApplicationPermissions.AssignCorrespondence)] public Task<IActionResult> Assign(CorrespondenceActionViewModel m, CancellationToken ct) => Respond(m.Id, service.AssignAsync(m.Id,m.AssignedToUserId,m.DueDate,m.Comment,ct));
    [HttpPost, Authorize(Policy = ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult> Advance(CorrespondenceActionViewModel m, CancellationToken ct) => Respond(m.Id, service.AdvanceAsync(m.Id,m.Target,m.Comment,ct));
    [HttpPost, RequestSizeLimit(10*1024*1024), Authorize(Policy = ApplicationPermissions.ProcessCorrespondence)] public async Task<IActionResult> Upload(CorrespondenceActionViewModel m, CancellationToken ct) { if (m.Upload is null) return RedirectToAction(nameof(Details),new{id=m.Id}); await using var s=m.Upload.OpenReadStream(); return await Respond(m.Id,service.UploadAsync(new(m.Id,m.DocumentType,m.Upload.FileName,m.Upload.ContentType,m.Upload.Length,s),ct)); }
    public async Task<IActionResult> Download(Guid id, CancellationToken ct) { var d=await service.DownloadAsync(id,ct); return d is null?NotFound():PhysicalFile(d.PhysicalPath,d.ContentType,d.FileName); }
    private async Task<IActionResult> Respond(Guid id, Task<CorrespondenceResult> task)
    {
        var result = await task;
        TempData["StatusMessage"] = result.Success
            ? result.Message ?? "Action completed successfully."
            : result.Message ?? "The action could not be completed.";
        TempData["StatusType"] = result.Success ? "success" : "danger";
        return RedirectToAction(nameof(Details), new { id });
    }
    private async Task<CorrespondenceCreateViewModel> WithReferences(CorrespondenceCreateViewModel m, CancellationToken ct) { var data=await service.GetReferenceDataAsync(ct); m.Organizations=data.Organizations.Select(x=>new SelectListItem(x.Label,x.Id.ToString())).ToArray(); m.Departments=data.Departments.Select(x=>new SelectListItem(x.Label,x.Id.ToString())).ToArray(); return m; }
}
