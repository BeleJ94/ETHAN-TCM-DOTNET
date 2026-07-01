using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class TaxFrequency : AuditableEntity
{
    private TaxFrequency()
    {
    }

    public TaxFrequency(string code, string name, int occurrencesPerYear)
    {
        if (occurrencesPerYear <= 0)
        {
            throw new DomainException("Occurrences per year must be greater than zero.");
        }

        Code = EntityGuards.Required(code, nameof(Code));
        Name = EntityGuards.Required(name, nameof(Name));
        OccurrencesPerYear = occurrencesPerYear;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int OccurrencesPerYear { get; private set; }
    public bool IsActive { get; private set; } = true;
}
