using System.ComponentModel.DataAnnotations;
using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EthanTcm.Web.Models;

public sealed class TaxDeclarationGenerationViewModel
{
    [Range(2000, 2100)]
    public int FiscalYear { get; set; } = DateTime.UtcNow.Year;

    public TaxDeclarationGenerationResult? Result { get; init; }
}

public sealed class ManualTaxDeclarationViewModel
{
    [Required]
    public Guid TaxObligationId { get; set; }

    [Required]
    public Guid AssignedToUserId { get; set; }

    [Required]
    [StringLength(100)]
    public string PeriodLabel { get; set; } = string.Empty;

    [Required]
    public DateOnly PeriodDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required]
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);

    public DateOnly? ReminderDate { get; set; }

    public bool IsNotApplicable { get; set; }

    public IReadOnlyCollection<SelectListItem> Obligations { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> Users { get; init; } = [];
}

public sealed class TaxDeclarationWorkflowIndexViewModel
{
    public string? Search { get; init; }
    public TaxDeclarationStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; } = 1;
    public IReadOnlyCollection<TaxDeclarationWorkflowListItemDto> Items { get; init; } = [];
}

public sealed class TaxDeclarationWorkflowDetailsViewModel
{
    public TaxDeclarationWorkflowDetailsDto Declaration { get; init; } = default!;
    public TaxDeclarationActionViewModel Action { get; init; } = new();
    public IReadOnlyCollection<SelectListItem> Users { get; init; } = [];
}

public sealed class TaxDeclarationActionViewModel
{
    public Guid TaxDeclarationId { get; set; }

    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;

    [StringLength(256)]
    public string SubmissionReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    [StringLength(256)]
    public string PaymentReference { get; set; } = string.Empty;

    public Guid AssignedToUserId { get; set; }

    [StringLength(1000)]
    public string ReassignmentComment { get; set; } = string.Empty;

    [StringLength(1000)]
    public string CorrectionReason { get; set; } = string.Empty;

    public DocumentType DocumentType { get; set; } = DocumentType.SubmissionProof;

    [StringLength(256)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContentType { get; set; } = "application/pdf";

    public IFormFile? Upload { get; set; }
}
