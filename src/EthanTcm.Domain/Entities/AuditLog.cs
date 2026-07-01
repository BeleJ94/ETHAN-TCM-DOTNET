using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid? userId,
        string entityName,
        string entityId,
        string action,
        string? oldValue,
        string? newValue,
        DateTimeOffset occurredAt,
        string? userDisplayName = null,
        string? correlationId = null,
        string? module = null,
        string? source = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestPath = null)
    {
        EntityName = EntityGuards.Required(entityName, nameof(EntityName));
        EntityId = EntityGuards.Required(entityId, nameof(EntityId));
        Action = EntityGuards.Required(action, nameof(Action));
        UserId = userId;
        UserDisplayName = userDisplayName;
        OldValue = oldValue;
        NewValue = newValue;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        Module = module;
        Source = source;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        RequestPath = requestPath;
        CreatedAt = occurredAt;
    }

    public Guid? UserId { get; private set; }
    public string? UserDisplayName { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? Module { get; private set; }
    public string? Source { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? RequestPath { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}
