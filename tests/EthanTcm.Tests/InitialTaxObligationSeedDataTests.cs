using EthanTcm.Application.Seed;

namespace EthanTcm.Tests;

public sealed class InitialTaxObligationSeedDataTests
{
    [Fact]
    public void Seed_data_contains_expected_obligations()
    {
        Assert.Equal(37, InitialTaxObligationSeedData.Items.Count);
    }

    [Fact]
    public void Seed_data_has_unique_business_keys()
    {
        var keys = InitialTaxObligationSeedData.Items
            .Select(item => $"{item.Department}|{item.ReportType}|{item.TaxCategory}")
            .ToArray();

        Assert.Equal(keys.Length, keys.Distinct().Count());
    }

    [Fact]
    public void Arsp_is_marked_requires_review()
    {
        var item = InitialTaxObligationSeedData.Items.Single(item => item.ReportType == "ARSP (QST) Return");

        Assert.True(item.RequiresReview);
        Assert.Null(item.Frequency);
        Assert.Null(item.LegalDeadline);
        Assert.Contains("Frequency", item.ReviewReason);
    }

    [Fact]
    public void Tp_light_return_is_marked_requires_review_because_number_is_missing()
    {
        var item = InitialTaxObligationSeedData.Items.Single(item => item.ReportType == "TP light return");

        Assert.True(item.RequiresReview);
        Assert.Null(item.Number);
        Assert.Contains("Number missing", item.ReviewReason);
    }

    [Fact]
    public void Monthly_obligations_create_twelve_active_rules()
    {
        var item = InitialTaxObligationSeedData.Items.Single(item => item.ReportType == "VAT Return");

        Assert.Equal(12, item.MonthlySchedule.Count(schedule => schedule.IsActive));
        Assert.Equal(Enumerable.Range(1, 12), item.MonthlySchedule.Select(schedule => schedule.MonthNumber));
    }

    [Fact]
    public void Annual_obligations_create_only_active_months()
    {
        var item = InitialTaxObligationSeedData.Items.Single(item => item.ReportType == "Corporate Income Tax - CIT Return");

        var schedule = Assert.Single(item.MonthlySchedule);
        Assert.Equal(4, schedule.MonthNumber);
        Assert.Equal("April", schedule.MonthName);
    }

    [Fact]
    public void Idempotent_seed_input_has_no_duplicate_report_type_within_department_and_category()
    {
        var duplicates = InitialTaxObligationSeedData.Items
            .GroupBy(item => new { item.Department, item.ReportType, item.TaxCategory })
            .Where(group => group.Count() > 1)
            .ToArray();

        Assert.Empty(duplicates);
    }
}
