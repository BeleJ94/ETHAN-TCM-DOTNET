using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface ITaxObligationReferentialService
{
    Task<TaxObligationListResult> SearchAsync(TaxObligationSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<TaxObligationDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxObligationEditOptions> GetEditOptionsAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TaxObligationUpsertCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, TaxObligationUpsertCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxObligationVersionCreationContext?> GetVersionCreationContextAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<Guid?> CreateVersionAsync(
        Guid id,
        TaxObligationVersionCreateCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record TaxObligationSearchCriteria(
    string? Search,
    Guid? DepartmentId,
    Guid? TaxCategoryId,
    Guid? TaxFrequencyId,
    bool? IsActive);

public sealed record TaxObligationListResult(
    IReadOnlyCollection<TaxObligationListItemDto> Items,
    TaxObligationEditOptions Options);

public sealed record TaxObligationListItemDto(
    Guid Id,
    string Name,
    string Department,
    string TaxCategory,
    string Frequency,
    bool RequiresPayment,
    bool IsActive);

public sealed record TaxObligationDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CanonicalCode { get; init; }
    public string? Description { get; init; }
    public string? ComplianceNotes { get; init; }
    public int? SourceNumber { get; init; }
    public string? LegalDeadline { get; init; }
    public bool RequiresReview { get; init; }
    public string? ReviewReason { get; init; }
    public TaxCatalogValidationStatus CatalogValidationStatus { get; init; }
    public Guid DepartmentId { get; init; }
    public string Department { get; init; } = string.Empty;
    public Guid TaxCategoryId { get; init; }
    public string TaxCategory { get; init; } = string.Empty;
    public Guid TaxFrequencyId { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public RiskLevel RiskLevel { get; init; }
    public bool RequiresPayment { get; init; }
    public bool RequiresSubmissionProof { get; init; }
    public bool RequiresPaymentProof { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int DeclarationCount { get; init; }
    public int OpenDeclarationCount { get; init; }
    public IReadOnlyCollection<TaxObligationResponsibleDto> Responsibles { get; init; } = [];
    public IReadOnlyCollection<TaxScheduleRuleDetailsDto> ScheduleRules { get; init; } = [];
    public IReadOnlyCollection<TaxObligationVersionDetailsDto> Versions { get; init; } = [];
    public IReadOnlyCollection<TaxRequiredDocumentDetailsDto> RequiredDocuments { get; init; } = [];
    public IReadOnlyCollection<TaxAliasDetailsDto> Aliases { get; init; } = [];
    public IReadOnlyCollection<TaxSourceReferenceDetailsDto> SourceReferences { get; init; } = [];
    public IReadOnlyCollection<TaxCatalogConflictDetailsDto> Conflicts { get; init; } = [];
    public IReadOnlyCollection<TaxAllocationDetailsDto> Allocations { get; init; } = [];
}

public sealed record TaxObligationResponsibleDto(
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    ResponsibleType Type);

public sealed record TaxScheduleRuleDetailsDto(
    int DueDay, int? DueMonth, bool MoveToNextBusinessDay,
    string? RawReminderText, string? ReminderDays, bool IsActive);

public sealed record TaxObligationVersionDetailsDto(
    Guid Id, int VersionNumber, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    string? Frequency, string? FilingDeadlineRule, string? PaymentDeadlineRule,
    string? RawDeadlineText, string? BusinessCycle, string? Authority,
    TaxCatalogValidationStatus ValidationStatus, bool RequiresReview, string ChangeReason,
    IReadOnlyCollection<string> RateRules, IReadOnlyCollection<string> LegalReferences,
    IReadOnlyCollection<string> PenaltyRules, IReadOnlyCollection<string> ProcessTemplates);

public sealed record TaxObligationVersionCreationContext(
    Guid TaxObligationId,
    string TaxObligationName,
    int NextVersionNumber,
    DateOnly MinimumEffectiveFrom,
    Guid? CurrentTaxAuthorityId,
    string? CurrentFrequency,
    string? CurrentFilingDeadlineRule,
    string? CurrentPaymentDeadlineRule,
    string? CurrentRawDeadlineText,
    string? CurrentBusinessCycle,
    TaxCatalogValidationStatus CurrentValidationStatus,
    bool CurrentRequiresReview,
    IReadOnlyCollection<string> CurrentRateRules,
    IReadOnlyCollection<string> CurrentLegalReferences,
    IReadOnlyCollection<string> CurrentPenaltyRules,
    IReadOnlyCollection<string> CurrentProcessTemplates,
    IReadOnlyCollection<LookupItemDto> Authorities);

public sealed record TaxObligationVersionCreateCommand(
    DateOnly EffectiveFrom,
    Guid? TaxAuthorityId,
    string? Frequency,
    string? FilingDeadlineRule,
    string? PaymentDeadlineRule,
    string? RawDeadlineText,
    string? BusinessCycle,
    TaxCatalogValidationStatus ValidationStatus,
    bool RequiresReview,
    string ChangeReason,
    IReadOnlyCollection<string> RateRules,
    IReadOnlyCollection<string> LegalReferences,
    IReadOnlyCollection<string> PenaltyRules,
    IReadOnlyCollection<string> ProcessTemplates);

public sealed record TaxRequiredDocumentDetailsDto(
    DocumentType DocumentType, bool IsRequired, string? Condition);

public sealed record TaxAliasDetailsDto(
    string Alias, string SourceName, string? SourceReference, string SourceRow);

public sealed record TaxSourceReferenceDetailsDto(
    string SourceName, string SourceRow, string? ExternalNumber,
    DateTimeOffset ImportedAt, string DataHash);

public sealed record TaxCatalogConflictDetailsDto(
    string FieldName, string? ExistingValue, string? IncomingValue,
    string SourceName, string SourceRow, string Status, string? Resolution);

public sealed record TaxAllocationDetailsDto(
    decimal Percentage, string Beneficiary, string SourceName, string SourceRow);

public sealed record TaxObligationUpsertCommand(
    string Name,
    string? Description,
    Guid DepartmentId,
    Guid TaxCategoryId,
    Guid TaxFrequencyId,
    RiskLevel RiskLevel,
    bool RequiresPayment,
    Guid PreparerUserId,
    Guid? Approver1UserId,
    Guid? Approver2UserId,
    Guid? Approver3UserId,
    Guid? PaymentProcessOwnerUserId,
    Guid? SubmissionProcessOwnerUserId,
    Guid? FollowUpOwnerUserId);

public sealed record TaxObligationEditOptions(
    IReadOnlyCollection<LookupItemDto> Departments,
    IReadOnlyCollection<LookupItemDto> TaxCategories,
    IReadOnlyCollection<LookupItemDto> TaxFrequencies,
    IReadOnlyCollection<LookupItemDto> Users);

public sealed record LookupItemDto(Guid Id, string Label);
