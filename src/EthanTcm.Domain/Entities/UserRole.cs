using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class UserRole : AuditableEntity
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId, DateTimeOffset assignedAt)
    {
        UserId = EntityGuards.Required(userId, nameof(UserId));
        RoleId = EntityGuards.Required(roleId, nameof(RoleId));
        AssignedAt = assignedAt;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
}
