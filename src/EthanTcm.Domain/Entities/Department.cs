using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class Department : AuditableEntity
{
    private Department()
    {
    }

    public Department(string code, string name)
    {
        Code = EntityGuards.Required(code, nameof(Code));
        Name = EntityGuards.Required(name, nameof(Name));
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}
