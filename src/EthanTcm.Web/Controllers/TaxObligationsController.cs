using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Enums;
using EthanTcm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EthanTcm.Web.Controllers;

[Authorize(Policy = ApplicationPermissions.ManageTaxObligations)]
public sealed class TaxObligationsController(ITaxObligationReferentialService taxObligationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        Guid? departmentId,
        Guid? taxCategoryId,
        Guid? taxFrequencyId,
        bool? isActive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await taxObligationService.SearchAsync(
            new TaxObligationSearchCriteria(search, departmentId, taxCategoryId, taxFrequencyId, isActive),
            cancellationToken);

        return View(new TaxObligationIndexViewModel
        {
            Search = search,
            DepartmentId = departmentId,
            TaxCategoryId = taxCategoryId,
            TaxFrequencyId = taxFrequencyId,
            IsActive = isActive,
            Items = result.Items,
            Departments = ToSelectList(result.Options.Departments, departmentId),
            TaxCategories = ToSelectList(result.Options.TaxCategories, taxCategoryId),
            TaxFrequencies = ToSelectList(result.Options.TaxFrequencies, taxFrequencyId)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var obligation = await taxObligationService.GetAsync(id, cancellationToken);
        return obligation is null ? NotFound() : View(obligation);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var options = await taxObligationService.GetEditOptionsAsync(cancellationToken);
        return View("Edit", BuildForm(new TaxObligationFormViewModel(), options));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxObligationFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateForm(model);
        var options = await taxObligationService.GetEditOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Edit", BuildForm(model, options));
        }

        try
        {
            var id = await taxObligationService.CreateAsync(ToCommand(model), cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Edit", BuildForm(model, options));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var obligation = await taxObligationService.GetAsync(id, cancellationToken);
        if (obligation is null)
        {
            return NotFound();
        }

        var options = await taxObligationService.GetEditOptionsAsync(cancellationToken);
        return View(BuildForm(ToForm(obligation), options));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TaxObligationFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateForm(model);
        var options = await taxObligationService.GetEditOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(BuildForm(model, options));
        }

        try
        {
            var updated = await taxObligationService.UpdateAsync(id, ToCommand(model), cancellationToken);
            return updated ? RedirectToAction(nameof(Details), new { id }) : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildForm(model, options));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var updated = await taxObligationService.DeactivateAsync(id, cancellationToken);
        return updated ? RedirectToAction(nameof(Details), new { id }) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> CreateVersion(Guid id, CancellationToken cancellationToken)
    {
        var context = await taxObligationService.GetVersionCreationContextAsync(id, cancellationToken);
        if (context is null)
        {
            return NotFound();
        }

        return View(BuildVersionForm(new TaxObligationVersionFormViewModel
        {
            TaxObligationId = id,
            EffectiveFrom = context.MinimumEffectiveFrom,
            TaxAuthorityId = context.CurrentTaxAuthorityId,
            Frequency = context.CurrentFrequency,
            FilingDeadlineRule = context.CurrentFilingDeadlineRule,
            PaymentDeadlineRule = context.CurrentPaymentDeadlineRule,
            RawDeadlineText = context.CurrentRawDeadlineText,
            BusinessCycle = context.CurrentBusinessCycle,
            ValidationStatus = context.CurrentValidationStatus,
            RequiresReview = context.CurrentRequiresReview,
            RateRules = JoinRules(context.CurrentRateRules),
            LegalReferences = JoinRules(context.CurrentLegalReferences),
            PenaltyRules = JoinRules(context.CurrentPenaltyRules),
            ProcessTemplates = JoinRules(context.CurrentProcessTemplates)
        }, context));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVersion(
        Guid id,
        TaxObligationVersionFormViewModel model,
        CancellationToken cancellationToken)
    {
        var context = await taxObligationService.GetVersionCreationContextAsync(id, cancellationToken);
        if (context is null)
        {
            return NotFound();
        }

        if (model.EffectiveFrom < context.MinimumEffectiveFrom)
        {
            ModelState.AddModelError(
                nameof(model.EffectiveFrom),
                $"La date d’effet doit être égale ou postérieure au {context.MinimumEffectiveFrom:dd/MM/yyyy}.");
        }

        if (!ModelState.IsValid)
        {
            return View(BuildVersionForm(model, context));
        }

        try
        {
            var versionId = await taxObligationService.CreateVersionAsync(
                id,
                new TaxObligationVersionCreateCommand(
                    model.EffectiveFrom,
                    model.TaxAuthorityId,
                    model.Frequency,
                    model.FilingDeadlineRule,
                    model.PaymentDeadlineRule,
                    model.RawDeadlineText,
                    model.BusinessCycle,
                    model.ValidationStatus,
                    model.RequiresReview,
                    model.ChangeReason,
                    SplitRules(model.RateRules),
                    SplitRules(model.LegalReferences),
                    SplitRules(model.PenaltyRules),
                    SplitRules(model.ProcessTemplates)),
                cancellationToken);

            return versionId.HasValue
                ? RedirectToAction(
                    actionName: nameof(Details),
                    controllerName: null,
                    routeValues: new { id },
                    fragment: "versions")
                : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildVersionForm(model, context));
        }
    }

    private static TaxObligationUpsertCommand ToCommand(TaxObligationFormViewModel model)
    {
        return new TaxObligationUpsertCommand(
            model.Name,
            model.Description,
            model.DepartmentId,
            model.TaxCategoryId,
            model.TaxFrequencyId,
            model.RiskLevel,
            model.RequiresPayment,
            model.PreparerUserId,
            model.Approver1UserId,
            model.Approver2UserId,
            model.Approver3UserId,
            model.PaymentProcessOwnerUserId,
            model.SubmissionProcessOwnerUserId,
            model.FollowUpOwnerUserId);
    }

    private static TaxObligationFormViewModel ToForm(TaxObligationDetailsDto obligation)
    {
        return new TaxObligationFormViewModel
        {
            Id = obligation.Id,
            Name = obligation.Name,
            Description = obligation.Description,
            DepartmentId = obligation.DepartmentId,
            TaxCategoryId = obligation.TaxCategoryId,
            TaxFrequencyId = obligation.TaxFrequencyId,
            RiskLevel = obligation.RiskLevel,
            RequiresPayment = obligation.RequiresPayment,
            PreparerUserId = FindOptionalUser(obligation, ResponsibleType.Preparer) ?? Guid.Empty,
            Approver1UserId = FindOptionalUser(obligation, ResponsibleType.Approver1),
            Approver2UserId = FindOptionalUser(obligation, ResponsibleType.Approver2),
            Approver3UserId = FindOptionalUser(obligation, ResponsibleType.Approver3),
            PaymentProcessOwnerUserId = FindOptionalUser(obligation, ResponsibleType.PaymentProcessOwner),
            SubmissionProcessOwnerUserId = FindOptionalUser(obligation, ResponsibleType.SubmissionProcessOwner),
            FollowUpOwnerUserId = FindOptionalUser(obligation, ResponsibleType.FollowUpOwner)
        };
    }

    private static Guid? FindOptionalUser(TaxObligationDetailsDto obligation, ResponsibleType type)
    {
        return obligation.Responsibles.FirstOrDefault(responsible => responsible.Type == type)?.UserId;
    }

    private static TaxObligationFormViewModel BuildForm(TaxObligationFormViewModel model, TaxObligationEditOptions options)
    {
        return new TaxObligationFormViewModel
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            DepartmentId = model.DepartmentId,
            TaxCategoryId = model.TaxCategoryId,
            TaxFrequencyId = model.TaxFrequencyId,
            RiskLevel = model.RiskLevel,
            RequiresPayment = model.RequiresPayment,
            PreparerUserId = model.PreparerUserId,
            Approver1UserId = model.Approver1UserId,
            Approver2UserId = model.Approver2UserId,
            Approver3UserId = model.Approver3UserId,
            PaymentProcessOwnerUserId = model.PaymentProcessOwnerUserId,
            SubmissionProcessOwnerUserId = model.SubmissionProcessOwnerUserId,
            FollowUpOwnerUserId = model.FollowUpOwnerUserId,
            Departments = ToSelectList(options.Departments, model.DepartmentId),
            TaxCategories = ToSelectList(options.TaxCategories, model.TaxCategoryId),
            TaxFrequencies = ToSelectList(options.TaxFrequencies, model.TaxFrequencyId),
            Users = ToSelectList(options.Users, null),
            RiskLevels = Enum.GetValues<RiskLevel>()
                .Select(value => new SelectListItem(value.ToString(), value.ToString(), value == model.RiskLevel))
                .ToArray()
        };
    }

    private static TaxObligationVersionFormViewModel BuildVersionForm(
        TaxObligationVersionFormViewModel model,
        TaxObligationVersionCreationContext context)
    {
        return new TaxObligationVersionFormViewModel
        {
            TaxObligationId = context.TaxObligationId,
            TaxObligationName = context.TaxObligationName,
            VersionNumber = context.NextVersionNumber,
            MinimumEffectiveFrom = context.MinimumEffectiveFrom,
            EffectiveFrom = model.EffectiveFrom,
            TaxAuthorityId = model.TaxAuthorityId,
            Frequency = model.Frequency,
            FilingDeadlineRule = model.FilingDeadlineRule,
            PaymentDeadlineRule = model.PaymentDeadlineRule,
            RawDeadlineText = model.RawDeadlineText,
            BusinessCycle = model.BusinessCycle,
            ValidationStatus = model.ValidationStatus,
            RequiresReview = model.RequiresReview,
            ChangeReason = model.ChangeReason,
            RateRules = model.RateRules,
            LegalReferences = model.LegalReferences,
            PenaltyRules = model.PenaltyRules,
            ProcessTemplates = model.ProcessTemplates,
            Authorities = ToSelectList(context.Authorities, model.TaxAuthorityId),
            ValidationStatuses = Enum.GetValues<TaxCatalogValidationStatus>()
                .Select(value => new SelectListItem(
                    ValidationStatusLabel(value),
                    value.ToString(),
                    value == model.ValidationStatus))
                .ToArray()
        };
    }

    private static string[] SplitRules(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? JoinRules(IEnumerable<string> rules)
    {
        var values = rules.ToArray();
        return values.Length == 0 ? null : string.Join(Environment.NewLine, values);
    }

    private static string ValidationStatusLabel(TaxCatalogValidationStatus status) => status switch
    {
        TaxCatalogValidationStatus.Draft => "Brouillon",
        TaxCatalogValidationStatus.PendingValidation => "En attente de validation",
        TaxCatalogValidationStatus.Validated => "Validée",
        _ => status.ToString()
    };

    private static IReadOnlyCollection<SelectListItem> ToSelectList(IEnumerable<LookupItemDto> items, Guid? selectedId)
    {
        return items.Select(item => new SelectListItem(item.Label, item.Id.ToString(), item.Id == selectedId)).ToArray();
    }

    private void ValidateForm(TaxObligationFormViewModel model)
    {
        if (model.DepartmentId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Department is required.");
        }

        if (model.TaxCategoryId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.TaxCategoryId), "Tax category is required.");
        }

        if (model.TaxFrequencyId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.TaxFrequencyId), "Frequency is required.");
        }

        if (model.PreparerUserId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.PreparerUserId), "Preparer is required.");
        }

    }
}
