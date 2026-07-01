using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class NotificationTemplate : AuditableEntity
{
    private NotificationTemplate()
    {
    }

    public NotificationTemplate(string code, string name, NotificationChannel channel, string subject, string body)
    {
        Code = EntityGuards.Required(code, nameof(Code));
        Name = EntityGuards.Required(name, nameof(Name));
        Channel = channel;
        Subject = EntityGuards.Required(subject, nameof(Subject));
        Body = EntityGuards.Required(body, nameof(Body));
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}
