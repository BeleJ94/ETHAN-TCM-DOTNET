using EthanTcm.Application.TaxDeclarations;

namespace EthanTcm.Tests;

public sealed class TaxDeclarationGenerationCalendarTests
{
    [Fact]
    public void Monthly_generation_uses_active_months_from_matrix()
    {
        var plan = TaxDeclarationGenerationCalendar.PlanAnnual(new TaxDeclarationPlanInput(
            2026,
            "MONTHLY",
            "Monthly",
            12,
            [
                new TaxDeclarationScheduleRuleInput(20, 1, false),
                new TaxDeclarationScheduleRuleInput(20, 3, false)
            ]));

        Assert.Equal(2, plan.Count);
        Assert.Collection(
            plan,
            item =>
            {
                Assert.Equal("2026-01", item.PeriodLabel);
                Assert.Equal(new DateOnly(2026, 1, 20), item.DueDate);
            },
            item =>
            {
                Assert.Equal("2026-03", item.PeriodLabel);
                Assert.Equal(new DateOnly(2026, 3, 20), item.DueDate);
            });
    }

    [Fact]
    public void Annual_generation_creates_one_full_year_period()
    {
        var plan = TaxDeclarationGenerationCalendar.PlanAnnual(new TaxDeclarationPlanInput(
            2026,
            "ANNUAL",
            "Annual",
            1,
            [new TaxDeclarationScheduleRuleInput(31, 12, false)]));

        var item = Assert.Single(plan);
        Assert.Equal("Annual", item.PeriodType);
        Assert.Equal("2026", item.PeriodLabel);
        Assert.Equal(new DateOnly(2026, 1, 1), item.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 31), item.EndDate);
        Assert.Equal(new DateOnly(2026, 12, 31), item.DueDate);
    }

    [Fact]
    public void Weekly_generation_creates_one_period_per_iso_week()
    {
        var plan = TaxDeclarationGenerationCalendar.PlanAnnual(new TaxDeclarationPlanInput(
            2026,
            "WEEKLY",
            "Weekly",
            52,
            []));

        Assert.Equal(53, plan.Count);
        Assert.Equal("2026-W01", plan.First().PeriodLabel);
        Assert.Equal("2026-W53", plan.Last().PeriodLabel);
    }

    [Fact]
    public void Due_date_moves_to_next_business_day_when_configured()
    {
        var plan = TaxDeclarationGenerationCalendar.PlanAnnual(new TaxDeclarationPlanInput(
            2026,
            "MONTHLY",
            "Monthly",
            12,
            [new TaxDeclarationScheduleRuleInput(31, 1, true)]));

        var item = Assert.Single(plan);
        Assert.Equal(new DateOnly(2026, 2, 2), item.DueDate);
    }

    [Fact]
    public void As_needed_frequency_is_not_generated_automatically()
    {
        var plan = TaxDeclarationGenerationCalendar.PlanAnnual(new TaxDeclarationPlanInput(
            2026,
            "AS_NEEDED",
            "As needed",
            1,
            []));

        Assert.Empty(plan);
    }
}
