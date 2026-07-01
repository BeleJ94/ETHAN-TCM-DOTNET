namespace EthanTcm.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedByUserId { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public Guid? UpdatedByUserId { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];

    public void MarkUpdated(DateTimeOffset timestamp)
    {
        UpdatedAt = timestamp;
    }

    public void MarkCreatedBy(Guid userId)
    {
        CreatedByUserId = userId;
    }

    public void MarkUpdatedBy(Guid userId, DateTimeOffset timestamp)
    {
        UpdatedByUserId = userId;
        MarkUpdated(timestamp);
    }
}
