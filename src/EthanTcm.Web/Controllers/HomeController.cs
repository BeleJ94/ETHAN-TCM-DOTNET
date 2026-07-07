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
        DashboardView view,
        DashboardMetric metric,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var details = await dashboardService.GetMetricDetailsAsync(
            view,
            metric,
            page,
            pageSize,
            cancellationToken);
        return PartialView("_KpiDetails", details);
    }

    [HttpGet]
    public async Task<IActionResult> ExportKpiDetails(
        DashboardView view,
        DashboardMetric metric,
        DashboardExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var export = await dashboardService.ExportMetricDetailsAsync(
            view,
            metric,
            format,
            cancellationToken);

        return File(export.Content, export.ContentType, export.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> ChartDetails(
        DashboardView view,
        DashboardChartType chart,
        string segmentKey,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var details = await dashboardService.GetChartSegmentDetailsAsync(
            view,
            chart,
            segmentKey,
            page,
            pageSize,
            cancellationToken);
        return PartialView("_KpiDetails", details);
    }

    [HttpGet]
    public async Task<IActionResult> ExportChartDetails(
        DashboardView view,
        DashboardChartType chart,
        string segmentKey,
        DashboardExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var export = await dashboardService.ExportChartSegmentDetailsAsync(
            view,
            chart,
            segmentKey,
            format,
            cancellationToken);

        return File(export.Content, export.ContentType, export.FileName);
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
