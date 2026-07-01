using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class TaxDeclarationApproval : AuditableEntity
{
    private TaxDeclarationApproval()
    {
    }

    public TaxDeclarationApproval(
        Guid taxDeclarationId,
        Guid approverUserId,
        int approvalCycleNumber,
        int approvalLevel,
        ApprovalDecision decision,
        string? comment,
        DateTimeOffset decidedAt)
    {
        TaxDeclarationId = EntityGuards.Required(taxDeclarationId, nameof(TaxDeclarationId));
        ApproverUserId = EntityGuards.Required(approverUserId, nameof(ApproverUserId));
        if (approvalCycleNumber <= 0)
        {
            throw new DomainException("Approval cycle number must be positive.");
        }

        if (approvalLevel is < 1 or > 3)
        {
            throw new DomainException("Approval level must be between 1 and 3.");
        }

        ApprovalCycleNumber = approvalCycleNumber;
        ApprovalLevel = approvalLevel;
        Decision = decision;
        Comment = comment;
        DecidedAt = decidedAt;
    }

    public Guid TaxDeclarationId { get; private set; }
    public Guid ApproverUserId { get; private set; }
    public int ApprovalCycleNumber { get; private set; }
    public int ApprovalLevel { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset DecidedAt { get; private set; }
}
