using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class TaxObligationAlias : AuditableEntity
{
    private TaxObligationAlias() { }
    public TaxObligationAlias(Guid taxObligationId, string sourceName, string? sourceReference, string alias, string sourceRow)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(taxObligationId));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceReference = sourceReference?.Trim();
        Alias = EntityGuards.Required(alias, nameof(alias));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public Guid TaxObligationId { get; private set; }
    public string SourceName { get; private set; } = string.Empty;
    public string? SourceReference { get; private set; }
    public string Alias { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
}

public sealed class TaxSourceReference : AuditableEntity
{
    private TaxSourceReference() { }
    public TaxSourceReference(Guid taxObligationId, string sourceName, string sourceRow, string? externalNumber, DateTimeOffset importedAt, string dataHash)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(taxObligationId));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
        ExternalNumber = externalNumber?.Trim();
        ImportedAt = importedAt;
        DataHash = EntityGuards.Required(dataHash, nameof(dataHash));
    }
    public Guid TaxObligationId { get; private set; }
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
    public string? ExternalNumber { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }
    public string DataHash { get; private set; } = string.Empty;
}

public sealed class TaxObligationVersion : AuditableEntity
{
    private TaxObligationVersion() { }
    public TaxObligationVersion(
        Guid taxObligationId, int versionNumber, DateOnly effectiveFrom, string? frequency,
        string? filingDeadlineRule, string? paymentDeadlineRule, string? rawDeadlineText,
        string? businessCycle, TaxCatalogValidationStatus validationStatus, bool requiresReview,
        string changeReason, string dataHash, Guid? taxAuthorityId = null)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(taxObligationId));
        VersionNumber = versionNumber > 0 ? versionNumber : throw new DomainException("Version number must be positive.");
        EffectiveFrom = effectiveFrom;
        Frequency = frequency?.Trim();
        FilingDeadlineRule = filingDeadlineRule?.Trim();
        PaymentDeadlineRule = paymentDeadlineRule?.Trim();
        RawDeadlineText = rawDeadlineText?.Trim();
        BusinessCycle = businessCycle?.Trim();
        ValidationStatus = validationStatus;
        RequiresReview = requiresReview;
        ChangeReason = EntityGuards.Required(changeReason, nameof(changeReason));
        DataHash = EntityGuards.Required(dataHash, nameof(dataHash));
        TaxAuthorityId = taxAuthorityId;
    }
    public Guid TaxObligationId { get; private set; }
    public int VersionNumber { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Frequency { get; private set; }
    public string? FilingDeadlineRule { get; private set; }
    public string? PaymentDeadlineRule { get; private set; }
    public string? RawDeadlineText { get; private set; }
    public string? BusinessCycle { get; private set; }
    public TaxCatalogValidationStatus ValidationStatus { get; private set; }
    public bool RequiresReview { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;
    public string DataHash { get; private set; } = string.Empty;
    public Guid? TaxAuthorityId { get; private set; }
    public void Close(DateOnly effectiveTo) => EffectiveTo = effectiveTo;
}

public sealed class TaxAuthority : AuditableEntity
{
    private TaxAuthority() { }
    public TaxAuthority(string code, string name)
    {
        Code = EntityGuards.Required(code, nameof(code));
        Name = EntityGuards.Required(name, nameof(name));
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}

public sealed class TaxRateRule : AuditableEntity
{
    private TaxRateRule() { }
    public TaxRateRule(Guid taxObligationVersionId, string ruleText, string sourceName, string sourceRow)
    {
        TaxObligationVersionId = EntityGuards.Required(taxObligationVersionId, nameof(taxObligationVersionId));
        RuleText = EntityGuards.Required(ruleText, nameof(ruleText));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public Guid TaxObligationVersionId { get; private set; }
    public string RuleText { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
}

public sealed class TaxLegalReference : AuditableEntity
{
    private TaxLegalReference() { }
    public TaxLegalReference(Guid taxObligationVersionId, string referenceText, string sourceName, string sourceRow)
    {
        TaxObligationVersionId = EntityGuards.Required(taxObligationVersionId, nameof(taxObligationVersionId));
        ReferenceText = EntityGuards.Required(referenceText, nameof(referenceText));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public Guid TaxObligationVersionId { get; private set; }
    public string ReferenceText { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
}

public sealed class TaxPenaltyRule : AuditableEntity
{
    private TaxPenaltyRule() { }
    public TaxPenaltyRule(Guid taxObligationVersionId, string ruleText, string sourceName, string sourceRow)
    {
        TaxObligationVersionId = EntityGuards.Required(taxObligationVersionId, nameof(taxObligationVersionId));
        RuleText = EntityGuards.Required(ruleText, nameof(ruleText));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public Guid TaxObligationVersionId { get; private set; }
    public string RuleText { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
}

public sealed class TaxProcessTemplate : AuditableEntity
{
    private TaxProcessTemplate() { }
    public TaxProcessTemplate(Guid taxObligationVersionId, string processText, string sourceName, string sourceRow)
    {
        TaxObligationVersionId = EntityGuards.Required(taxObligationVersionId, nameof(taxObligationVersionId));
        ProcessText = EntityGuards.Required(processText, nameof(processText));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public Guid TaxObligationVersionId { get; private set; }
    public string ProcessText { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
}

public sealed class TaxRequiredDocument : AuditableEntity
{
    private TaxRequiredDocument() { }
    public TaxRequiredDocument(Guid taxObligationId, DocumentType documentType, bool isRequired, string? condition)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(taxObligationId));
        DocumentType = documentType;
        IsRequired = isRequired;
        Condition = condition?.Trim();
    }
    public Guid TaxObligationId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public bool IsRequired { get; private set; }
    public string? Condition { get; private set; }
}

public sealed class TaxCatalogConflict : AuditableEntity
{
    private TaxCatalogConflict() { }
    public TaxCatalogConflict(string canonicalCode, string fieldName, string? existingValue, string? incomingValue, string sourceName, string sourceRow)
    {
        CanonicalCode = EntityGuards.Required(canonicalCode, nameof(canonicalCode));
        FieldName = EntityGuards.Required(fieldName, nameof(fieldName));
        ExistingValue = existingValue;
        IncomingValue = incomingValue;
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public string CanonicalCode { get; private set; } = string.Empty;
    public string FieldName { get; private set; } = string.Empty;
    public string? ExistingValue { get; private set; }
    public string? IncomingValue { get; private set; }
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PendingValidation";
    public string? Resolution { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
}

public sealed class TaxAllocationRule : AuditableEntity
{
    private TaxAllocationRule() { }
    public TaxAllocationRule(Guid taxObligationId, decimal percentage, string beneficiary, string sourceName, string sourceRow)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(taxObligationId));
        Percentage = percentage;
        Beneficiary = EntityGuards.Required(beneficiary, nameof(beneficiary));
        SourceName = EntityGuards.Required(sourceName, nameof(sourceName));
        SourceRow = EntityGuards.Required(sourceRow, nameof(sourceRow));
    }
    public Guid TaxObligationId { get; private set; }
    public decimal Percentage { get; private set; }
    public string Beneficiary { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public string SourceRow { get; private set; } = string.Empty;
}
