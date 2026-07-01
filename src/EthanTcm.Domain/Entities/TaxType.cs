using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class TaxType : AuditableEntity
{
    private TaxType()
    {
    }

    public TaxType(string code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
}
