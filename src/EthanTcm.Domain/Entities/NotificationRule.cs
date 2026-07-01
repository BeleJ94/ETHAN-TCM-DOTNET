using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class NotificationRule : AuditableEntity
{
    private NotificationRule()
    {
    }

    public NotificationRule(
        string code,
        Guid notificationTemplateId,
        NotificationTrigger trigger,
        int offsetDays,
        bool notifyResponsible,
        bool notifyApprover)
    {
        Code = EntityGuards.Required(code, nameof(Code));
        NotificationTemplateId = EntityGuards.Required(notificationTemplateId, nameof(NotificationTemplateId));
        Trigger = trigger;
        OffsetDays = offsetDays;
        NotifyResponsible = notifyResponsible;
        NotifyApprover = notifyApprover;
    }

    public string Code { get; private set; } = string.Empty;
    public Guid NotificationTemplateId { get; private set; }
    public NotificationTrigger Trigger { get; private set; }
    public int OffsetDays { get; private set; }
    public bool NotifyResponsible { get; private set; }
    public bool NotifyApprover { get; private set; }
    public bool IsActive { get; private set; } = true;
}
