using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class TaxObligationResponsible : AuditableEntity
{
    private TaxObligationResponsible()
    {
    }

    public TaxObligationResponsible(Guid taxObligationId, Guid userId, ResponsibleType type, DateTimeOffset assignedAt)
    {
        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(TaxObligationId));
        UserId = EntityGuards.Required(userId, nameof(UserId));
        Type = type;
        AssignedAt = assignedAt;
    }

    public Guid TaxObligationId { get; private set; }
    public Guid UserId { get; private set; }
    public ResponsibleType Type { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public bool IsPrimary => Type is ResponsibleType.Primary or ResponsibleType.Preparer;
}
