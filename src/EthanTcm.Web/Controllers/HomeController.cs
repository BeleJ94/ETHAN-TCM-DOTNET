using System.Diagnostics;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EthanTcm.Web.Models;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ViewDashboard)]
public class HomeController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(DashboardView view = DashboardView.MyTasks, CancellationToken cancellationToken = default)
    {
        var dashboard = await dashboardService.GetAsync(view, cancellationToken);
        return View(new DashboardViewModel { Dashboard = dashboard });
    }

    [HttpGet]
    public async Task<IActionResult> KpiDetails(
        DashboardMetric metric,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var details = await dashboardService.GetMetricDetailsAsync(
            metric,
            page,
            pageSize,
            cancellationToken);
        return PartialView("_KpiDetails", details);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
