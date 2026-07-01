using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class TaxDeclaration : AuditableEntity
{
    private readonly List<TaxDeclarationApproval> _approvals = [];
    private readonly List<TaxPayment> _payments = [];
    private readonly List<TaxDocument> _documents = [];

    private TaxDeclaration()
    {
    }

    public TaxDeclaration(
        Guid taxObligationId,
        Guid taxPeriodId,
        DateOnly dueDate,
        string periodLabel,
        bool paymentRequired,
        Guid assignedToUserId,
        DateOnly? reminderDate = null,
        TaxDeclarationStatus status = TaxDeclarationStatus.ToPrepare,
        Guid? taxObligationVersionId = null)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(TaxObligationId));
        TaxPeriodId = EntityGuards.Required(taxPeriodId, nameof(TaxPeriodId));
        TaxObligationVersionId = taxObligationVersionId;
        DueDate = dueDate;
        ReminderDate = reminderDate;
        PeriodLabel = EntityGuards.Required(periodLabel, nameof(PeriodLabel));
        PaymentRequired = paymentRequired;
        AssignedToUserId = EntityGuards.Required(assignedToUserId, nameof(AssignedToUserId));
        Status = status;
    }

    public Guid TaxObligationId { get; private set; }
    public Guid? TaxObligationVersionId { get; private set; }
    public Guid TaxPeriodId { get; private set; }
    public string PeriodLabel { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public DateOnly? ReminderDate { get; private set; }
    public TaxDeclarationStatus Status { get; private set; } = TaxDeclarationStatus.ToPrepare;
    public Guid AssignedToUserId { get; private set; }
    public Guid? ValidatorUserId { get; private set; }
    public bool PaymentRequired { get; private set; }
    public int ApprovalCycleNumber { get; private set; }
    public DateTimeOffset? PreparedAt { get; private set; }
    public DateTimeOffset? ValidatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public string? SubmissionReference { get; private set; }
    public int DelayDays { get; private set; }
    public RiskLevel PenaltyRiskLevel { get; private set; } = RiskLevel.Low;
    public IReadOnlyCollection<TaxDeclarationApproval> Approvals => _approvals.AsReadOnly();
    public IReadOnlyCollection<TaxPayment> Payments => _payments.AsReadOnly();
    public IReadOnlyCollection<TaxDocument> Documents => _documents.AsReadOnly();

    public TaxObligation? TaxObligation { get; private set; }
    public TaxPeriod? TaxPeriod { get; private set; }
    public TaxObligationVersion? TaxObligationVersion { get; private set; }

    public void LinkVersion(Guid taxObligationVersionId, DateTimeOffset timestamp)
    {
        TaxObligationVersionId = EntityGuards.Required(taxObligationVersionId, nameof(taxObligationVersionId));
        MarkUpdated(timestamp);
    }

    public void StartPreparation(DateTimeOffset timestamp)
    {
        EnsureStatus(TaxDeclarationStatus.ToPrepare, "Only declarations to prepare can be started.");
        Status = TaxDeclarationStatus.InPreparation;
        MarkUpdated(timestamp);
    }

    public void MarkReadyForApproval(DateTimeOffset timestamp)
    {
        SubmitForReview(timestamp);
    }

    public void SubmitForReview(DateTimeOffset timestamp)
    {
        if (Status is not (TaxDeclarationStatus.InPreparation or TaxDeclarationStatus.Rejected))
        {
            throw new DomainException("Only declarations in preparation or rejected can be submitted for review.");
        }

        ApprovalCycleNumber++;
        Status = TaxDeclarationStatus.SubmittedForReview;
        PreparedAt = timestamp;
        MarkUpdated(timestamp);
    }

    public void AddApproval(Guid approverUserId, ApprovalDecision decision, string? comment, DateTimeOffset decidedAt)
    {
        if (decision == ApprovalDecision.Rejected)
        {
            Reject(approverUserId, comment, decidedAt);
            return;
        }

        ApproveNextLevel(approverUserId, decidedAt);
    }

    public int NextApprovalLevel()
    {
        return Status switch
        {
            TaxDeclarationStatus.SubmittedForReview => 1,
            TaxDeclarationStatus.ApprovedLevel1 => 2,
            TaxDeclarationStatus.ApprovedLevel2 => 3,
            _ => 0
        };
    }

    public void ApproveNextLevel(Guid approverUserId, DateTimeOffset decidedAt)
    {
        EntityGuards.Required(approverUserId, nameof(approverUserId));

        var nextLevel = NextApprovalLevel();
        if (nextLevel == 0)
        {
            throw new DomainException("This declaration is not waiting for approval.");
        }

        EnsureActiveApprovalCycle();

        _approvals.Add(new TaxDeclarationApproval(
            Id,
            approverUserId,
            ApprovalCycleNumber,
            nextLevel,
            ApprovalDecision.Approved,
            $"Level {nextLevel} approved",
            decidedAt));
        ValidatorUserId = approverUserId;
        ValidatedAt = decidedAt;

        Status = nextLevel switch
        {
            1 => TaxDeclarationStatus.ApprovedLevel1,
            2 => TaxDeclarationStatus.ApprovedLevel2,
            _ => TaxDeclarationStatus.ApprovedLevel3
        };

        if (Status == TaxDeclarationStatus.ApprovedLevel3)
        {
            Status = TaxDeclarationStatus.ReadyForSubmission;
        }

        MarkUpdated(decidedAt);
    }

    public void Reject(Guid approverUserId, string? comment, DateTimeOffset rejectedAt)
    {
        EntityGuards.Required(approverUserId, nameof(approverUserId));

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("A rejection comment is required.");
        }

        if (NextApprovalLevel() == 0)
        {
            throw new DomainException("Only declarations under review can be rejected.");
        }

        var rejectedLevel = NextApprovalLevel();
        EnsureActiveApprovalCycle();

        _approvals.Add(new TaxDeclarationApproval(
            Id,
            approverUserId,
            ApprovalCycleNumber,
            rejectedLevel,
            ApprovalDecision.Rejected,
            comment.Trim(),
            rejectedAt));
        ValidatorUserId = approverUserId;
        Status = TaxDeclarationStatus.Rejected;
        MarkUpdated(rejectedAt);
    }

    public void MarkSubmitted(string submissionReference, DateTimeOffset submittedAt, Guid? submittedByUserId = null)
    {
        EnsureStatus(TaxDeclarationStatus.ReadyForSubmission, "Only declarations ready for submission can be submitted.");
        SubmissionReference = EntityGuards.Required(submissionReference, nameof(submissionReference));
        SubmittedAt = submittedAt;
        SubmittedByUserId = submittedByUserId;
        Status = PaymentRequired ? TaxDeclarationStatus.PaymentPending : TaxDeclarationStatus.Submitted;
        MarkUpdated(submittedAt);
    }

    public void AddPayment(decimal amount, string currency, string paymentReference, DateTimeOffset paidAt, Guid? paidByUserId = null)
    {
        EnsureStatus(TaxDeclarationStatus.PaymentPending, "Only payment pending declarations can be paid.");
        _payments.Add(new TaxPayment(Id, amount, currency, paymentReference, paidAt, paidByUserId));
        Status = TaxDeclarationStatus.Paid;
        MarkUpdated(paidAt);
    }

    public void AddDocument(DocumentType documentType, string fileName, string filePath, string contentType, Guid uploadedByUserId, DateTimeOffset uploadedAt)
    {
        _documents.Add(new TaxDocument(Id, documentType, fileName, filePath, contentType, uploadedByUserId, uploadedAt));
        MarkUpdated(uploadedAt);
    }

    public void Reassign(Guid assignedToUserId, DateTimeOffset timestamp)
    {
        EntityGuards.Required(assignedToUserId, nameof(assignedToUserId));

        if (Status is TaxDeclarationStatus.Closed or TaxDeclarationStatus.Cancelled or TaxDeclarationStatus.NotApplicable)
        {
            throw new DomainException("A closed, cancelled or not applicable declaration cannot be reassigned.");
        }

        if (AssignedToUserId == assignedToUserId)
        {
            return;
        }

        AssignedToUserId = assignedToUserId;
        MarkUpdated(timestamp);
    }

    public void Close(DateTimeOffset closedAt, Guid? closedByUserId = null)
    {
        if (PaymentRequired)
        {
            EnsureStatus(TaxDeclarationStatus.Paid, "A declaration requiring payment must be paid before closing.");
        }
        else
        {
            EnsureStatus(TaxDeclarationStatus.Submitted, "Only submitted declarations can be closed.");
        }

        EnsureHasSubmissionProof();

        if (PaymentRequired)
        {
            EnsureHasPaymentProof();
        }

        Status = TaxDeclarationStatus.Closed;
        ClosedAt = closedAt;
        ClosedByUserId = closedByUserId;
        MarkUpdated(closedAt);
    }

    public void MarkNotApplicable(DateTimeOffset timestamp)
    {
        if (Status == TaxDeclarationStatus.Closed)
        {
            throw new DomainException("A closed tax declaration cannot be marked not applicable.");
        }

        Status = TaxDeclarationStatus.NotApplicable;
        MarkUpdated(timestamp);
    }

    public void Cancel(DateTimeOffset timestamp)
    {
        EnsureNotClosed();
        Status = TaxDeclarationStatus.Cancelled;
        MarkUpdated(timestamp);
    }

    public bool HasDocument(DocumentType documentType)
    {
        return _documents.Any(document => document.DocumentType == documentType && !document.IsDeleted);
    }

    private void EnsureHasSubmissionProof()
    {
        if (!HasDocument(DocumentType.SubmissionProof))
        {
            throw new DomainException("A tax declaration cannot be closed without submission proof.");
        }
    }

    private void EnsureHasPaymentProof()
    {
        if (!HasDocument(DocumentType.PaymentProof))
        {
            throw new DomainException("A tax declaration requiring payment cannot be closed without payment proof.");
        }
    }

    private void EnsureNotClosed()
    {
        if (Status == TaxDeclarationStatus.Closed)
        {
            throw new DomainException("A closed tax declaration cannot be modified.");
        }
    }

    private void EnsureStatus(TaxDeclarationStatus expectedStatus, string message)
    {
        EnsureNotClosed();

        if (Status != expectedStatus)
        {
            throw new DomainException(message);
        }
    }

    private void EnsureActiveApprovalCycle()
    {
        if (ApprovalCycleNumber <= 0)
        {
            throw new DomainException("This declaration has no active approval cycle.");
        }
    }
}
