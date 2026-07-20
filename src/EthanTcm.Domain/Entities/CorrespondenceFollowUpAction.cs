using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class CorrespondenceFollowUpAction : AuditableEntity
{
    private CorrespondenceFollowUpAction() { }
    public CorrespondenceFollowUpAction(Guid correspondenceId, string title, string? description, Guid assignedToUserId,
        DateOnly dueDate, CorrespondencePriority priority, Guid createdByUserId, Guid? escalationUserId = null)
    {
        CorrespondenceId = EntityGuards.Required(correspondenceId, nameof(correspondenceId));
        Title = EntityGuards.Required(title, nameof(title)); Description = description?.Trim();
        AssignedToUserId = EntityGuards.Required(assignedToUserId, nameof(assignedToUserId));
        DueDate = dueDate; Priority = priority; EscalationUserId = escalationUserId;
        Status = CorrespondenceActionStatus.ToDo; MarkCreatedBy(createdByUserId);
    }
    public Guid CorrespondenceId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid AssignedToUserId { get; private set; }
    public Guid? EscalationUserId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateOnly? FollowUpDate { get; private set; }
    public CorrespondencePriority Priority { get; private set; }
    public CorrespondenceActionStatus Status { get; private set; }
    public string? WaitingFor { get; private set; }
    public string? CompletionResult { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsEscalated { get; private set; }

    public void Start(DateTimeOffset at) { Ensure(CorrespondenceActionStatus.ToDo); Status = CorrespondenceActionStatus.InProgress; StartedAt = at; MarkUpdated(at); }
    public void WaitForThirdParty(string waitingFor, DateOnly followUpDate, DateTimeOffset at)
    { if (Status is not (CorrespondenceActionStatus.ToDo or CorrespondenceActionStatus.InProgress)) throw new DomainException("Only an open action can wait for a third party."); WaitingFor = EntityGuards.Required(waitingFor, nameof(waitingFor)); FollowUpDate = followUpDate; Status = CorrespondenceActionStatus.WaitingForThirdParty; MarkUpdated(at); }
    public void Resume(DateTimeOffset at) { Ensure(CorrespondenceActionStatus.WaitingForThirdParty); Status = CorrespondenceActionStatus.InProgress; WaitingFor = null; FollowUpDate = null; MarkUpdated(at); }
    public void Complete(string result, DateTimeOffset at) { if (Status is CorrespondenceActionStatus.Completed or CorrespondenceActionStatus.Cancelled) throw new DomainException("This action is already final."); CompletionResult = EntityGuards.Required(result, nameof(result)); Status = CorrespondenceActionStatus.Completed; CompletedAt = at; MarkUpdated(at); }
    public void Cancel(string reason, DateTimeOffset at) { CompletionResult = EntityGuards.Required(reason, nameof(reason)); Status = CorrespondenceActionStatus.Cancelled; CompletedAt = at; MarkUpdated(at); }
    public void Escalate(DateTimeOffset at) { IsEscalated = true; MarkUpdated(at); }
    private void Ensure(CorrespondenceActionStatus expected) { if (Status != expected) throw new DomainException($"Action must be {expected}."); }
}
