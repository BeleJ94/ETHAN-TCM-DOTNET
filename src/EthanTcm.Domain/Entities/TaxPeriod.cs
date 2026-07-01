using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class TaxPeriod : AuditableEntity
{
    private TaxPeriod()
    {
    }

    public TaxPeriod(int year, int? month, int? quarter, DateOnly startDate, DateOnly endDate, string label)
    {
        Year = year;
        Month = month;
        Quarter = quarter;
        StartDate = startDate;
        EndDate = endDate;
        Label = label;
        PeriodType = month.HasValue ? "Monthly" : quarter.HasValue ? "Quarterly" : "Annual";
        Sequence = month ?? quarter ?? 1;
    }

    public int Year { get; private set; }
    public int? Month { get; private set; }
    public int? Quarter { get; private set; }
    public string PeriodType { get; private set; } = "Annual";
    public int? Sequence { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string Label { get; private set; } = string.Empty;

    public static TaxPeriod Weekly(int year, int weekNumber, DateOnly startDate, DateOnly endDate)
    {
        return new TaxPeriod(year, null, null, startDate, endDate, $"{year}-W{weekNumber:00}")
        {
            PeriodType = "Weekly",
            Sequence = weekNumber
        };
    }

    public static TaxPeriod Manual(int year, int sequence, DateOnly periodDate, string label)
    {
        return new TaxPeriod(year, null, null, periodDate, periodDate, label)
        {
            PeriodType = "Manual",
            Sequence = sequence
        };
    }
}
