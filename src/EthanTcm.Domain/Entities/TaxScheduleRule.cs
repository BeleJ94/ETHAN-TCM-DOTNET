using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class TaxScheduleRule : AuditableEntity
{
    private TaxScheduleRule()
    {
    }

    public TaxScheduleRule(
        Guid taxObligationId,
        int dueDay,
        int? dueMonth,
        bool moveToNextBusinessDay,
        string? rawReminderText = null,
        string? reminderDays = null,
        bool isActive = true)
    {
        if (dueDay is < 1 or > 31)
        {
            throw new DomainException("Due day must be between 1 and 31.");
        }

        if (dueMonth is < 1 or > 12)
        {
            throw new DomainException("Due month must be between 1 and 12.");
        }

        TaxObligationId = EntityGuards.Required(taxObligationId, nameof(TaxObligationId));
        DueDay = dueDay;
        DueMonth = dueMonth;
        MoveToNextBusinessDay = moveToNextBusinessDay;
        RawReminderText = rawReminderText;
        ReminderDays = reminderDays;
        IsActive = isActive;
    }

    public Guid TaxObligationId { get; private set; }
    public int DueDay { get; private set; }
    public int? DueMonth { get; private set; }
    public bool MoveToNextBusinessDay { get; private set; }
    public string? RawReminderText { get; private set; }
    public string? ReminderDays { get; private set; }
    public bool IsActive { get; private set; } = true;
}
