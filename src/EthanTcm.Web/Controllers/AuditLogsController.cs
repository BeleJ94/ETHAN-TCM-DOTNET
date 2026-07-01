using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ViewAuditLogs)]
public sealed class AuditLogsController(IAuditService auditService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(AuditLogIndexViewModel model, CancellationToken cancellationToken)
    {
        var items = await auditService.ListAsync(
            new AuditLogQuery(
                model.Search,
                model.Action,
                model.EntityName,
                model.From,
                model.To),
            cancellationToken);

        return View(new AuditLogIndexViewModel
        {
            Search = model.Search,
            Action = model.Action,
            EntityName = model.EntityName,
            From = model.From,
            To = model.To,
            Items = items
        });
    }
}
