using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;
[Authorize(Policy=ApplicationPermissions.ViewCorrespondence)]
public sealed class CorrespondenceActionsController(ICorrespondenceActionService service):Controller
{
 public async Task<IActionResult> Index(bool myActions=true,EthanTcm.Domain.Enums.CorrespondenceActionStatus? status=null,CancellationToken ct=default)=>View(new CorrespondenceActionsViewModel{MyActions=myActions,Status=status,Dashboard=await service.GetDashboardAsync(myActions,ct),Items=await service.ListAsync(myActions,status,ct)});
 [HttpPost,Authorize(Policy=ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult>Create(CorrespondenceActionCommandViewModel m,CancellationToken ct)=>Respond(m.CorrespondenceId,service.CreateAsync(new(m.CorrespondenceId,m.Title,m.Description,m.AssignedToUserId,m.DueDate,m.Priority,m.EscalationUserId),ct));
 [HttpPost,Authorize(Policy=ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult>Start(CorrespondenceActionCommandViewModel m,CancellationToken ct)=>Respond(m.CorrespondenceId,service.StartAsync(m.Id,ct));
 [HttpPost,Authorize(Policy=ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult>Wait(CorrespondenceActionCommandViewModel m,CancellationToken ct)=>Respond(m.CorrespondenceId,service.WaitAsync(m.Id,m.WaitingFor??"Third party response",m.FollowUpDate,ct));
 [HttpPost,Authorize(Policy=ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult>Resume(CorrespondenceActionCommandViewModel m,CancellationToken ct)=>Respond(m.CorrespondenceId,service.ResumeAsync(m.Id,ct));
 [HttpPost,Authorize(Policy=ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult>Complete(CorrespondenceActionCommandViewModel m,CancellationToken ct)=>Respond(m.CorrespondenceId,service.CompleteAsync(m.Id,m.Result??string.Empty,ct));
 [HttpPost,Authorize(Policy=ApplicationPermissions.ProcessCorrespondence)] public Task<IActionResult>Cancel(CorrespondenceActionCommandViewModel m,CancellationToken ct)=>Respond(m.CorrespondenceId,service.CancelAsync(m.Id,m.Result??string.Empty,ct));
 private async Task<IActionResult>Respond(Guid correspondenceId,Task<CorrespondenceResult> task){var r=await task;var message=r.Success?r.Message??"Action updated.":r.Message??"The action could not be updated.";TempData["StatusMessage"]=message;TempData["StatusType"]=r.Success?"success":"danger";var url=Url.Action("Details","Correspondences",new{id=correspondenceId});if(string.Equals(Request.Headers["X-Requested-With"],"XMLHttpRequest",StringComparison.OrdinalIgnoreCase))return Json(new{success=r.Success,message,refreshUrl=url});return RedirectToAction("Details","Correspondences",new{id=correspondenceId});}
}
