using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class Permission : AuditableEntity
{
    private Permission() { }
    public Permission(string code, string name, string domain, string? description = null)
    {
        Code = EntityGuards.Required(code, nameof(code));
        Name = EntityGuards.Required(name, nameof(name));
        Domain = EntityGuards.Required(domain, nameof(domain));
        Description = description?.Trim();
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
}

public sealed class RolePermission : AuditableEntity
{
    private RolePermission() { }
    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = EntityGuards.Required(roleId, nameof(roleId));
        PermissionId = EntityGuards.Required(permissionId, nameof(permissionId));
    }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
}
