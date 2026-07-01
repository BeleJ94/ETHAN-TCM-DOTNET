using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.RunAdministrationTasks)]
public sealed class AdminSeedController(IInitialTaxObligationSeeder seeder) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new AdminSeedViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunInitialTaxObligations(CancellationToken cancellationToken)
    {
        var result = await seeder.SeedAsync(cancellationToken);
        return View("Index", new AdminSeedViewModel { Result = result });
    }
}
