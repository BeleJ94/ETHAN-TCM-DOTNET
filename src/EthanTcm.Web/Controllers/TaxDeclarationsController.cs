using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Enums;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ViewTaxDeclarations)]
public sealed class TaxDeclarationsController(
    ITaxDeclarationGenerationService generationService,
    ITaxDeclarationWorkflowService workflowService,
    ITaxDocumentService documentService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        TaxDeclarationStatus? status,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await workflowService.SearchAsync(
            new TaxDeclarationWorkflowSearchCriteria(search, status, page, pageSize),
            cancellationToken);
        return View(new TaxDeclarationWorkflowIndexViewModel
        {
            Search = search,
            Status = status,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            Items = result.Items
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var declaration = await workflowService.GetAsync(id, cancellationToken);
        if (declaration is null)
        {
            return NotFound();
        }

        var options = await generationService.GetManualCreationOptionsAsync(cancellationToken);

        return View(new TaxDeclarationWorkflowDetailsViewModel
        {
            Declaration = declaration,
            Action = new TaxDeclarationActionViewModel
            {
                TaxDeclarationId = id,
                AssignedToUserId = declaration.AssignedToUserId
            },
            Users = ToSelectList(options.Users, declaration.AssignedToUserId)
        });
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.GenerateTaxDeclarations)]
    public IActionResult Generate()
    {
        return View(new TaxDeclarationGenerationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.GenerateTaxDeclarations)]
    public async Task<IActionResult> Generate(TaxDeclarationGenerationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await generationService.GenerateAnnualAsync(model.FiscalYear, cancellationToken);
        return View(new TaxDeclarationGenerationViewModel
        {
            FiscalYear = model.FiscalYear,
            Result = result
        });
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.CreateTaxDeclarations)]
    public async Task<IActionResult> Manual(CancellationToken cancellationToken)
    {
        var options = await generationService.GetManualCreationOptionsAsync(cancellationToken);
        return View(BuildManualForm(new ManualTaxDeclarationViewModel(), options));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.CreateTaxDeclarations)]
    public async Task<IActionResult> Manual(ManualTaxDeclarationViewModel model, CancellationToken cancellationToken)
    {
        ValidateManualForm(model);
        var options = await generationService.GetManualCreationOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(BuildManualForm(model, options));
        }

        var result = await generationService.CreateManualAsync(
            new TaxDeclarationManualCreationCommand(
                model.TaxObligationId,
                model.PeriodDate,
                model.DueDate,
                model.ReminderDate,
                model.PeriodLabel,
                model.AssignedToUserId,
                model.IsNotApplicable),
            cancellationToken);

        if (!result.Created)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "The manual declaration could not be created.");
            return View(BuildManualForm(model, options));
        }

        TempData["StatusMessage"] = "Manual tax declaration created.";
        return RedirectToAction(nameof(Manual));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.PrepareTaxDeclarations)]
    public Task<IActionResult> StartPreparation(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        return ExecuteWorkflowAction(model.TaxDeclarationId, () => workflowService.StartPreparationAsync(model.TaxDeclarationId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.PrepareTaxDeclarations)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SubmitForReview(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        if (model.Upload is null || model.Upload.Length == 0)
        {
            return WorkflowResponse(model.TaxDeclarationId, false, "Preparation proof is required before submitting for review.");
        }

        var uploadResult = await UploadDocumentInternalAsync(model, DocumentType.TaxReturnDraft, cancellationToken);
        if (!uploadResult.Success)
        {
            return WorkflowResponse(model.TaxDeclarationId, false, uploadResult.ErrorMessage);
        }

        return await ExecuteWorkflowAction(
            model.TaxDeclarationId,
            () => workflowService.SubmitForReviewAsync(model.TaxDeclarationId, cancellationToken),
            "Declaration submitted for review.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.ViewTaxDeclarations)]
    public Task<IActionResult> Approve(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        return ExecuteWorkflowAction(model.TaxDeclarationId, () => workflowService.ApproveAsync(model.TaxDeclarationId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.ViewTaxDeclarations)]
    public Task<IActionResult> Reject(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Comment))
        {
            return Task.FromResult(WorkflowResponse(model.TaxDeclarationId, false, "Rejection comment is required."));
        }

        return ExecuteWorkflowAction(model.TaxDeclarationId, () => workflowService.RejectAsync(model.TaxDeclarationId, model.Comment, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.PrepareTaxDeclarations)]
    public Task<IActionResult> MarkSubmitted(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        return ExecuteWorkflowAction(model.TaxDeclarationId, () => workflowService.MarkSubmittedAsync(model.TaxDeclarationId, model.SubmissionReference, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.ManageTaxPayments)]
    public Task<IActionResult> MarkPaid(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        if (model.Amount < 0.01m)
        {
            return Task.FromResult(WorkflowResponse(model.TaxDeclarationId, false, "Payment amount must be greater than or equal to 0.01."));
        }

        return ExecuteWorkflowAction(
            model.TaxDeclarationId,
            () => workflowService.MarkPaidAsync(model.TaxDeclarationId, model.Amount, model.Currency, model.PaymentReference, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.UploadTaxDocuments)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<IActionResult> AttachDocument(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        if (model.Upload is null || model.Upload.Length == 0)
        {
            return Task.FromResult(WorkflowResponse(model.TaxDeclarationId, false, "Document file is required."));
        }

        return UploadDocumentAsync(model, cancellationToken);
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.DownloadTaxDocuments)]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken cancellationToken)
    {
        var authorization = await documentService.CanDownloadAsync(id, cancellationToken);
        if (!authorization.Exists)
        {
            return NotFound();
        }

        if (!authorization.IsAllowed)
        {
            return Forbid();
        }

        var download = await documentService.GetDownloadAsync(id, cancellationToken);
        if (download is null)
        {
            return NotFound();
        }

        return PhysicalFile(download.PhysicalPath, download.ContentType, download.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.DeleteTaxDocuments)]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid taxDeclarationId, CancellationToken cancellationToken)
    {
        var result = await documentService.DeleteAsync(id, cancellationToken);
        return WorkflowResponse(taxDeclarationId, result.Success, result.Success ? "Document deleted." : result.ErrorMessage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.ManageTaxDeclarationLifecycle)]
    public Task<IActionResult> Reassign(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        if (model.AssignedToUserId == Guid.Empty)
        {
            return Task.FromResult(WorkflowResponse(model.TaxDeclarationId, false, "Assigned user is required."));
        }

        if (string.IsNullOrWhiteSpace(model.ReassignmentComment))
        {
            return Task.FromResult(WorkflowResponse(model.TaxDeclarationId, false, "Reassignment comment is required."));
        }

        return ExecuteWorkflowAction(
            model.TaxDeclarationId,
            () => workflowService.ReassignAsync(
                new TaxDeclarationReassignmentCommand(
                    model.TaxDeclarationId,
                    model.AssignedToUserId,
                    model.ReassignmentComment),
                cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.ManageTaxDeclarationLifecycle)]
    public Task<IActionResult> Close(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        return ExecuteWorkflowAction(model.TaxDeclarationId, () => workflowService.CloseAsync(model.TaxDeclarationId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = ApplicationPermissions.ManageTaxDeclarationLifecycle)]
    public Task<IActionResult> Cancel(TaxDeclarationActionViewModel model, CancellationToken cancellationToken)
    {
        return ExecuteWorkflowAction(model.TaxDeclarationId, () => workflowService.CancelAsync(model.TaxDeclarationId, cancellationToken));
    }

    private async Task<IActionResult> ExecuteWorkflowAction(
        Guid taxDeclarationId,
        Func<Task<TaxDeclarationWorkflowResult>> action,
        string successMessage = "Workflow action completed.")
    {
        var result = await action();
        return WorkflowResponse(taxDeclarationId, result.Success, result.Success ? successMessage : result.ErrorMessage);
    }

    private async Task<IActionResult> UploadDocumentAsync(
        TaxDeclarationActionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return WorkflowResponse(model.TaxDeclarationId, false, "Invalid document upload request.");
        }

        var result = await UploadDocumentInternalAsync(model, model.DocumentType, cancellationToken);
        return WorkflowResponse(model.TaxDeclarationId, result.Success, result.Success ? "Document uploaded." : result.ErrorMessage);
    }

    private async Task<TaxDocumentUploadResult> UploadDocumentInternalAsync(
        TaxDeclarationActionViewModel model,
        DocumentType documentType,
        CancellationToken cancellationToken)
    {
        await using var stream = model.Upload!.OpenReadStream();
        return await documentService.UploadAsync(
            new TaxDocumentUploadCommand(
                model.TaxDeclarationId,
                documentType,
                model.Upload.FileName,
                string.IsNullOrWhiteSpace(model.Upload.ContentType) ? model.ContentType : model.Upload.ContentType,
                model.Upload.Length,
                stream),
            cancellationToken);
    }

    private IActionResult WorkflowResponse(Guid taxDeclarationId, bool success, string? message)
    {
        var safeMessage = string.IsNullOrWhiteSpace(message)
            ? success ? "Workflow action completed." : "The action could not be completed."
            : message;

        TempData["StatusMessage"] = safeMessage;

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success,
                message = safeMessage,
                refreshUrl = Url.Action(nameof(Details), new { id = taxDeclarationId })
            });
        }

        return RedirectToAction(nameof(Details), new { id = taxDeclarationId });
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private static ManualTaxDeclarationViewModel BuildManualForm(
        ManualTaxDeclarationViewModel model,
        TaxDeclarationManualCreationOptions options)
    {
        return new ManualTaxDeclarationViewModel
        {
            TaxObligationId = model.TaxObligationId,
            AssignedToUserId = model.AssignedToUserId,
            PeriodLabel = model.PeriodLabel,
            PeriodDate = model.PeriodDate,
            DueDate = model.DueDate,
            ReminderDate = model.ReminderDate,
            IsNotApplicable = model.IsNotApplicable,
            Obligations = ToSelectList(options.Obligations, model.TaxObligationId),
            Users = ToSelectList(options.Users, model.AssignedToUserId)
        };
    }

    private static IReadOnlyCollection<SelectListItem> ToSelectList(IEnumerable<LookupItemDto> items, Guid selectedId)
    {
        return items.Select(item => new SelectListItem(item.Label, item.Id.ToString(), item.Id == selectedId)).ToArray();
    }

    private void ValidateManualForm(ManualTaxDeclarationViewModel model)
    {
        if (model.TaxObligationId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.TaxObligationId), "Tax obligation is required.");
        }

        if (model.AssignedToUserId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.AssignedToUserId), "Assigned user is required.");
        }

        if (model.DueDate < model.PeriodDate)
        {
            ModelState.AddModelError(nameof(model.DueDate), "Due date cannot be before period date.");
        }
    }
}
