using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class NotificationLog : AuditableEntity
{
    private NotificationLog()
    {
    }

    public NotificationLog(Guid notificationRuleId, Guid taxDeclarationId, Guid recipientUserId, string subject, string message)
    {
        NotificationRuleId = EntityGuards.Required(notificationRuleId, nameof(NotificationRuleId));
        TaxDeclarationId = EntityGuards.Required(taxDeclarationId, nameof(TaxDeclarationId));
        RecipientUserId = EntityGuards.Required(recipientUserId, nameof(RecipientUserId));
        Subject = EntityGuards.Required(subject, nameof(Subject));
        Message = EntityGuards.Required(message, nameof(Message));
    }

    public Guid NotificationRuleId { get; private set; }
    public Guid TaxDeclarationId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public DateTimeOffset? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
        MarkUpdated(sentAt);
    }

    public void MarkFailed(string errorMessage, DateTimeOffset timestamp)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = EntityGuards.Required(errorMessage, nameof(errorMessage));
        MarkUpdated(timestamp);
    }
}
