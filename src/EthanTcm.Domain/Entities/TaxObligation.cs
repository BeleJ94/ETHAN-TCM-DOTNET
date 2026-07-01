using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class TaxObligation : AuditableEntity
{
    private readonly List<TaxObligationResponsible> _responsibles = [];
    private readonly List<TaxScheduleRule> _scheduleRules = [];

    private TaxObligation()
    {
    }

    public TaxObligation(
        Guid legalEntityId,
        Guid departmentId,
        Guid taxCategoryId,
        Guid taxFrequencyId,
        Guid primaryResponsibleUserId,
        string name,
        RiskLevel riskLevel,
        bool requiresPayment,
        DateTimeOffset createdAt)
    {
        LegalEntityId = EntityGuards.Required(legalEntityId, nameof(LegalEntityId));
        DepartmentId = EntityGuards.Required(departmentId, nameof(DepartmentId));
        TaxCategoryId = EntityGuards.Required(taxCategoryId, nameof(TaxCategoryId));
        TaxFrequencyId = EntityGuards.Required(taxFrequencyId, nameof(TaxFrequencyId));
        Name = EntityGuards.Required(name, nameof(Name));
        RiskLevel = riskLevel;
        RequiresPayment = requiresPayment;
        RequiresPaymentProof = requiresPayment;
        CreatedAt = createdAt;

        AddResponsible(primaryResponsibleUserId, ResponsibleType.Primary, createdAt);
    }

    public Guid LegalEntityId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid TaxCategoryId { get; private set; }
    public Guid TaxFrequencyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? CanonicalCode { get; private set; }
    public string? Description { get; private set; }
    public string? ComplianceNotes { get; private set; }
    public int? SourceNumber { get; private set; }
    public string? LegalDeadline { get; private set; }
    public bool RequiresReview { get; private set; }
    public string? ReviewReason { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public bool RequiresPayment { get; private set; }
    public bool RequiresSubmissionProof { get; private set; } = true;
    public bool RequiresPaymentProof { get; private set; }
    public bool IsActive { get; private set; } = true;
    public TaxCatalogValidationStatus CatalogValidationStatus { get; private set; } = TaxCatalogValidationStatus.Validated;
    public IReadOnlyCollection<TaxObligationResponsible> Responsibles => _responsibles.AsReadOnly();
    public IReadOnlyCollection<TaxScheduleRule> ScheduleRules => _scheduleRules.AsReadOnly();

    public LegalEntity? LegalEntity { get; private set; }

    public void AddResponsible(Guid userId, ResponsibleType type, DateTimeOffset assignedAt)
    {
        EntityGuards.Required(userId, nameof(userId));

        if (_responsibles.Any(responsible => responsible.UserId == userId && responsible.Type == type))
        {
            return;
        }

        if (type == ResponsibleType.Primary && _responsibles.Any(responsible => responsible.Type == ResponsibleType.Primary))
        {
            throw new DomainException("A tax obligation can have only one primary responsible user.");
        }

        if (type == ResponsibleType.Preparer && _responsibles.Any(responsible => responsible.Type == ResponsibleType.Preparer))
        {
            throw new DomainException("A tax obligation can have only one assigned preparer.");
        }

        _responsibles.Add(new TaxObligationResponsible(Id, userId, type, assignedAt));
        MarkUpdated(assignedAt);
    }

    public void UpdateDetails(
        Guid departmentId,
        Guid taxCategoryId,
        Guid taxFrequencyId,
        string name,
        string? description,
        RiskLevel riskLevel,
        bool requiresPayment,
        DateTimeOffset timestamp)
    {
        DepartmentId = EntityGuards.Required(departmentId, nameof(departmentId));
        TaxCategoryId = EntityGuards.Required(taxCategoryId, nameof(taxCategoryId));
        TaxFrequencyId = EntityGuards.Required(taxFrequencyId, nameof(taxFrequencyId));
        Name = EntityGuards.Required(name, nameof(name));
        Description = description;
        RiskLevel = riskLevel;
        RequiresPayment = requiresPayment;
        RequiresPaymentProof = requiresPayment;
        MarkUpdated(timestamp);
    }

    public void ReplaceResponsibles(IEnumerable<(Guid UserId, ResponsibleType Type)> responsibles, DateTimeOffset timestamp)
    {
        var desiredResponsibles = responsibles
            .Where(responsible => responsible.UserId != Guid.Empty)
            .Distinct()
            .ToArray();

        foreach (var existingResponsible in _responsibles
            .Where(existingResponsible => !desiredResponsibles.Any(responsible =>
                responsible.UserId == existingResponsible.UserId &&
                responsible.Type == existingResponsible.Type))
            .ToArray())
        {
            _responsibles.Remove(existingResponsible);
        }

        foreach (var responsible in desiredResponsibles)
        {
            AddResponsible(responsible.UserId, responsible.Type, timestamp);
        }

        MarkUpdated(timestamp);
    }

    public void Deactivate(DateTimeOffset timestamp)
    {
        IsActive = false;
        MarkUpdated(timestamp);
    }

    public void Activate(DateTimeOffset timestamp)
    {
        IsActive = true;
        MarkUpdated(timestamp);
    }

    public void AddScheduleRule(int dueDay, int? dueMonth, bool moveToNextBusinessDay, DateTimeOffset timestamp)
    {
        _scheduleRules.Add(new TaxScheduleRule(Id, dueDay, dueMonth, moveToNextBusinessDay));
        MarkUpdated(timestamp);
    }

    public void ReplaceScheduleRules(
        IEnumerable<(int DueDay, int? DueMonth, bool MoveToNextBusinessDay, string? RawReminderText, string? ReminderDays, bool IsActive)> rules,
        DateTimeOffset timestamp)
    {
        _scheduleRules.Clear();

        foreach (var rule in rules)
        {
            _scheduleRules.Add(new TaxScheduleRule(
                Id,
                rule.DueDay,
                rule.DueMonth,
                rule.MoveToNextBusinessDay,
                rule.RawReminderText,
                rule.ReminderDays,
                rule.IsActive));
        }

        MarkUpdated(timestamp);
    }

    public void UpdateSeedMetadata(
        int? sourceNumber,
        string? legalDeadline,
        bool requiresReview,
        string? reviewReason,
        DateTimeOffset timestamp)
    {
        SourceNumber = sourceNumber;
        LegalDeadline = string.IsNullOrWhiteSpace(legalDeadline) ? null : legalDeadline.Trim();
        RequiresReview = requiresReview;
        ReviewReason = string.IsNullOrWhiteSpace(reviewReason) ? null : reviewReason.Trim();
        MarkUpdated(timestamp);
    }

    public void AssignCanonicalCode(string canonicalCode, DateTimeOffset timestamp)
    {
        CanonicalCode = EntityGuards.Required(canonicalCode, nameof(canonicalCode)).ToUpperInvariant();
        MarkUpdated(timestamp);
    }

    public void UpdateCatalogMetadata(
        string? complianceNotes,
        TaxCatalogValidationStatus validationStatus,
        bool requiresReview,
        string? reviewReason,
        DateTimeOffset timestamp)
    {
        ComplianceNotes = string.IsNullOrWhiteSpace(complianceNotes) ? null : complianceNotes.Trim();
        CatalogValidationStatus = validationStatus;
        RequiresReview = requiresReview;
        ReviewReason = string.IsNullOrWhiteSpace(reviewReason) ? null : reviewReason.Trim();
        MarkUpdated(timestamp);
    }

    public void EnsureReadyForUse()
    {
        EntityGuards.Required(DepartmentId, nameof(DepartmentId));
        EntityGuards.Required(TaxCategoryId, nameof(TaxCategoryId));
        EntityGuards.Required(TaxFrequencyId, nameof(TaxFrequencyId));

        if (!_responsibles.Any(responsible => responsible.IsPrimary))
        {
            throw new DomainException("A tax obligation must have a primary responsible user.");
        }
    }
}
