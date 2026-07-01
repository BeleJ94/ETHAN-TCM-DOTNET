using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class LegalEntity : AuditableEntity
{
    private LegalEntity()
    {
    }

    public LegalEntity(string code, string name, string country, string? taxIdentificationNumber)
    {
        Code = code;
        Name = name;
        Country = country;
        TaxIdentificationNumber = taxIdentificationNumber;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? TaxIdentificationNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
}
