using System.ComponentModel.DataAnnotations;
using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EthanTcm.Web.Models;

public sealed class TaxObligationIndexViewModel
{
    public string? Search { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? TaxCategoryId { get; init; }
    public Guid? TaxFrequencyId { get; init; }
    public bool? IsActive { get; init; } = true;
    public IReadOnlyCollection<TaxObligationListItemDto> Items { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> Departments { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> TaxCategories { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> TaxFrequencies { get; init; } = [];
}

public sealed class TaxObligationFormViewModel
{
    public Guid? Id { get; init; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public Guid TaxCategoryId { get; set; }

    [Required]
    public Guid TaxFrequencyId { get; set; }

    [Required]
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;

    public bool RequiresPayment { get; set; } = true;

    [Required]
    public Guid PreparerUserId { get; set; }

    public Guid? Approver1UserId { get; set; }

    public Guid? Approver2UserId { get; set; }

    public Guid? Approver3UserId { get; set; }

    public Guid? PaymentProcessOwnerUserId { get; set; }

    public Guid? SubmissionProcessOwnerUserId { get; set; }

    public Guid? FollowUpOwnerUserId { get; set; }

    public IReadOnlyCollection<SelectListItem> Departments { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> TaxCategories { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> TaxFrequencies { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> Users { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> RiskLevels { get; init; } = [];
}

public sealed class TaxObligationVersionFormViewModel
{
    public Guid TaxObligationId { get; init; }
    public string TaxObligationName { get; init; } = string.Empty;
    public int VersionNumber { get; init; }
    public DateOnly MinimumEffectiveFrom { get; init; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly EffectiveFrom { get; set; }

    public Guid? TaxAuthorityId { get; set; }

    [StringLength(200)]
    public string? Frequency { get; set; }

    [StringLength(1000)]
    public string? FilingDeadlineRule { get; set; }

    [StringLength(1000)]
    public string? PaymentDeadlineRule { get; set; }

    [StringLength(2000)]
    public string? RawDeadlineText { get; set; }

    [StringLength(500)]
    public string? BusinessCycle { get; set; }

    [Required]
    public TaxCatalogValidationStatus ValidationStatus { get; set; } = TaxCatalogValidationStatus.Draft;

    public bool RequiresReview { get; set; } = true;

    [Required]
    [StringLength(1000)]
    public string ChangeReason { get; set; } = string.Empty;

    [StringLength(20000)]
    public string? RateRules { get; set; }

    [StringLength(20000)]
    public string? LegalReferences { get; set; }

    [StringLength(20000)]
    public string? PenaltyRules { get; set; }

    [StringLength(20000)]
    public string? ProcessTemplates { get; set; }

    public IReadOnlyCollection<SelectListItem> Authorities { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> ValidationStatuses { get; init; } = [];
}
