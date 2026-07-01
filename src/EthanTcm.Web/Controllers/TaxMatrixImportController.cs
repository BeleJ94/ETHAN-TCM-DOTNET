using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ImportTaxMatrix)]
public sealed class TaxMatrixImportController(
    ITaxMatrixImportService importService,
    ICurrentUserService currentUserService)
    : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new TaxMatrixImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Select an .xlsx file.");
            return View("Index", new TaxMatrixImportViewModel());
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(file), "Only .xlsx files are supported.");
            return View("Index", new TaxMatrixImportViewModel());
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(file), "The import file exceeds the maximum size of 10 MB.");
            return View("Index", new TaxMatrixImportViewModel());
        }

        var currentUserId = currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Forbid();
        }

        await using var stream = file.OpenReadStream();
        var preview = await importService.PreviewAsync(stream, file.FileName, currentUserId.Value, cancellationToken);

        return View("Index", new TaxMatrixImportViewModel
        {
            ImportBatchId = preview.ImportBatchId,
            FileName = preview.FileName,
            TotalRows = preview.TotalRows,
            ValidRows = preview.ValidRows,
            InvalidRows = preview.InvalidRows,
            HasCriticalErrors = preview.HasCriticalErrors,
            Errors = preview.Errors
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Commit(Guid importBatchId, CancellationToken cancellationToken)
    {
        var result = await importService.CommitAsync(importBatchId, cancellationToken);

        return View("Index", new TaxMatrixImportViewModel
        {
            ImportBatchId = result.ImportBatchId,
            ImportedObligations = result.ImportedObligations,
            ErrorMessage = result.ErrorMessage
        });
    }
}
