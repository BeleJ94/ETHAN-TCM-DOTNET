using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ManageTaxPayments)]
public class PaymentsController(IDashboardService dashboardService) : Controller
{
    public Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        return Pending(cancellationToken);
    }

    public async Task<IActionResult> Pending(CancellationToken cancellationToken = default)
    {
        return await PaymentViewAsync(DashboardView.PaymentPending, "Pending payments", cancellationToken);
    }

    public async Task<IActionResult> Late(CancellationToken cancellationToken = default)
    {
        return await PaymentViewAsync(DashboardView.LatePayments, "Late payments", cancellationToken);
    }

    public async Task<IActionResult> MissingProof(CancellationToken cancellationToken = default)
    {
        return await PaymentViewAsync(DashboardView.MissingPaymentProof, "Missing payment proof", cancellationToken);
    }

    private async Task<IActionResult> PaymentViewAsync(
        DashboardView view,
        string title,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAsync(view, cancellationToken);
        ViewData["Title"] = title;
        return View("Index", new DashboardViewModel { Dashboard = dashboard });
    }
}
