using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class CorrespondenceActionNotification : AuditableEntity
{
    private CorrespondenceActionNotification() { }

    public CorrespondenceActionNotification(
        Guid actionId,
        Guid recipientUserId,
        string trigger,
        DateOnly processingDate,
        string subject,
        string message)
    {
        CorrespondenceActionId = EntityGuards.Required(actionId, nameof(actionId));
        RecipientUserId = EntityGuards.Required(recipientUserId, nameof(recipientUserId));
        Trigger = EntityGuards.Required(trigger, nameof(trigger));
        ProcessingDate = processingDate;
        Subject = EntityGuards.Required(subject, nameof(subject));
        Message = EntityGuards.Required(message, nameof(message));
    }

    public Guid CorrespondenceActionId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Trigger { get; private set; } = string.Empty;
    public DateOnly ProcessingDate { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public DateTimeOffset? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkSent(DateTimeOffset at) { Status = NotificationStatus.Sent; SentAt = at; MarkUpdated(at); }
    public void MarkFailed(string error, DateTimeOffset at) { Status = NotificationStatus.Failed; ErrorMessage = EntityGuards.Required(error, nameof(error)); MarkUpdated(at); }
}
