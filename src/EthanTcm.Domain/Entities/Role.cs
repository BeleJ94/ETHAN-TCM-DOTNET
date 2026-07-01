using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class Role : AuditableEntity
{
    private Role()
    {
    }

    public Role(string code, string name, string? description = null)
    {
        Code = EntityGuards.Required(code, nameof(Code));
        Name = EntityGuards.Required(name, nameof(Name));
        Description = description;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
}
