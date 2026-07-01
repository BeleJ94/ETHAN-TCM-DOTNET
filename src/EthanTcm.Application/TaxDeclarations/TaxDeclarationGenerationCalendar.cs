using System.Globalization;

namespace EthanTcm.Application.TaxDeclarations;

public static class TaxDeclarationGenerationCalendar
{
    public static IReadOnlyCollection<TaxDeclarationPlanItem> PlanAnnual(TaxDeclarationPlanInput input)
    {
        var rules = input.ScheduleRules.OrderBy(rule => rule.DueMonth ?? 99).ThenBy(rule => rule.DueDay).ToArray();
        var frequency = Normalize(input.FrequencyCode + " " + input.FrequencyName);

        if (frequency.Contains("ASNEEDED", StringComparison.Ordinal) ||
            frequency.Contains("ADHOC", StringComparison.Ordinal))
        {
            return [];
        }

        if (input.OccurrencesPerYear >= 50 || frequency.Contains("WEEKLY", StringComparison.Ordinal))
        {
            return BuildWeekly(input.FiscalYear, rules);
        }

        if (input.OccurrencesPerYear == 12 || frequency.Contains("MONTHLY", StringComparison.Ordinal))
        {
            return BuildMonthly(input.FiscalYear, rules);
        }

        if (input.OccurrencesPerYear == 4 || frequency.Contains("QUARTER", StringComparison.Ordinal))
        {
            return BuildQuarterly(input.FiscalYear, rules);
        }

        if (input.OccurrencesPerYear == 1 || frequency.Contains("ANNUAL", StringComparison.Ordinal))
        {
            return BuildAnnual(input.FiscalYear, rules);
        }

        if (rules.Any(rule => rule.DueMonth.HasValue))
        {
            return BuildMonthly(input.FiscalYear, rules);
        }

        return BuildAnnual(input.FiscalYear, rules);
    }

    private static IReadOnlyCollection<TaxDeclarationPlanItem> BuildMonthly(
        int fiscalYear,
        IReadOnlyCollection<TaxDeclarationScheduleRuleInput> rules)
    {
        var activeMonths = rules.Where(rule => rule.DueMonth.HasValue)
            .Select(rule => rule.DueMonth!.Value)
            .Distinct()
            .Order()
            .DefaultIfEmpty()
            .ToArray();

        if (activeMonths.Length == 1 && activeMonths[0] == 0)
        {
            activeMonths = Enumerable.Range(1, 12).ToArray();
        }

        return activeMonths
            .Select(month =>
            {
                var rule = FindRule(rules, month);
                var startDate = new DateOnly(fiscalYear, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                var dueDate = BuildDueDate(fiscalYear, month, rule, fallbackDay: endDate.Day);

                return new TaxDeclarationPlanItem(
                    "Monthly",
                    month,
                    month,
                    null,
                    startDate,
                    endDate,
                    $"{fiscalYear}-{month:00}",
                    dueDate,
                    dueDate.AddDays(-15));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<TaxDeclarationPlanItem> BuildQuarterly(
        int fiscalYear,
        IReadOnlyCollection<TaxDeclarationScheduleRuleInput> rules)
    {
        var quarterEndMonths = rules.Where(rule => rule.DueMonth.HasValue)
            .Select(rule => ((rule.DueMonth!.Value - 1) / 3 + 1) * 3)
            .Distinct()
            .Order()
            .DefaultIfEmpty()
            .ToArray();

        if (quarterEndMonths.Length == 1 && quarterEndMonths[0] == 0)
        {
            quarterEndMonths = [3, 6, 9, 12];
        }

        return quarterEndMonths
            .Select(month =>
            {
                var quarter = month / 3;
                var rule = FindRule(rules, month);
                var startDate = new DateOnly(fiscalYear, month - 2, 1);
                var endDate = new DateOnly(fiscalYear, month, DateTime.DaysInMonth(fiscalYear, month));
                var dueDate = BuildDueDate(fiscalYear, month, rule, fallbackDay: endDate.Day);

                return new TaxDeclarationPlanItem(
                    "Quarterly",
                    quarter,
                    null,
                    quarter,
                    startDate,
                    endDate,
                    $"{fiscalYear}-Q{quarter}",
                    dueDate,
                    dueDate.AddDays(-15));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<TaxDeclarationPlanItem> BuildAnnual(
        int fiscalYear,
        IReadOnlyCollection<TaxDeclarationScheduleRuleInput> rules)
    {
        var rule = rules.FirstOrDefault();
        var dueMonth = rule?.DueMonth ?? 12;
        var dueDate = BuildDueDate(fiscalYear, dueMonth, rule, fallbackDay: 31);

        return
        [
            new TaxDeclarationPlanItem(
                "Annual",
                1,
                null,
                null,
                new DateOnly(fiscalYear, 1, 1),
                new DateOnly(fiscalYear, 12, 31),
                fiscalYear.ToString(CultureInfo.InvariantCulture),
                dueDate,
                dueDate.AddDays(-15))
        ];
    }

    private static IReadOnlyCollection<TaxDeclarationPlanItem> BuildWeekly(
        int fiscalYear,
        IReadOnlyCollection<TaxDeclarationScheduleRuleInput> rules)
    {
        var weeks = ISOWeek.GetWeeksInYear(fiscalYear);
        var rule = rules.FirstOrDefault();

        return Enumerable.Range(1, weeks)
            .Select(week =>
            {
                var startDateTime = ISOWeek.ToDateTime(fiscalYear, week, DayOfWeek.Monday);
                var startDate = DateOnly.FromDateTime(startDateTime);
                var endDate = startDate.AddDays(6);
                var offset = rule is not null && rule.DueDay <= 7 ? rule.DueDay - 1 : 6;
                var dueDate = MoveIfNeeded(startDate.AddDays(offset), rule?.MoveToNextBusinessDay ?? false);

                return new TaxDeclarationPlanItem(
                    "Weekly",
                    week,
                    null,
                    null,
                    startDate,
                    endDate,
                    $"{fiscalYear}-W{week:00}",
                    dueDate,
                    dueDate.AddDays(-2));
            })
            .ToArray();
    }

    private static TaxDeclarationScheduleRuleInput? FindRule(
        IReadOnlyCollection<TaxDeclarationScheduleRuleInput> rules,
        int month)
    {
        return rules.FirstOrDefault(rule => rule.DueMonth == month) ?? rules.FirstOrDefault();
    }

    private static DateOnly BuildDueDate(
        int fiscalYear,
        int month,
        TaxDeclarationScheduleRuleInput? rule,
        int fallbackDay)
    {
        var day = Math.Min(rule?.DueDay ?? fallbackDay, DateTime.DaysInMonth(fiscalYear, month));
        return MoveIfNeeded(new DateOnly(fiscalYear, month, day), rule?.MoveToNextBusinessDay ?? false);
    }

    private static DateOnly MoveIfNeeded(DateOnly date, bool moveToNextBusinessDay)
    {
        if (!moveToNextBusinessDay)
        {
            return date;
        }

        return date.DayOfWeek switch
        {
            DayOfWeek.Saturday => date.AddDays(2),
            DayOfWeek.Sunday => date.AddDays(1),
            _ => date
        };
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}

public sealed record TaxDeclarationPlanInput(
    int FiscalYear,
    string FrequencyCode,
    string FrequencyName,
    int OccurrencesPerYear,
    IReadOnlyCollection<TaxDeclarationScheduleRuleInput> ScheduleRules);

public sealed record TaxDeclarationScheduleRuleInput(
    int DueDay,
    int? DueMonth,
    bool MoveToNextBusinessDay);

public sealed record TaxDeclarationPlanItem(
    string PeriodType,
    int Sequence,
    int? Month,
    int? Quarter,
    DateOnly StartDate,
    DateOnly EndDate,
    string PeriodLabel,
    DateOnly DueDate,
    DateOnly? ReminderDate);
