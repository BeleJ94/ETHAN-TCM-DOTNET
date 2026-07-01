using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Tests;

public sealed class InitialTaxObligationSeederTests
{
    [Fact]
    public async Task Seed_creates_expected_obligations()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        var result = await seeder.SeedAsync();

        Assert.Empty(result.Errors);
        Assert.Equal(37, result.Created);
        Assert.Equal(0, result.Updated);
        Assert.Equal(37, await dbContext.TaxObligations.CountAsync());
        Assert.True(await dbContext.Departments.AnyAsync(department => department.Name == "Finance"));
        Assert.True(await dbContext.TaxCategories.AnyAsync(category => category.Name == "Value Added Tax"));
        Assert.True(await dbContext.TaxFrequencies.AnyAsync(frequency => frequency.Name == "Monthly"));
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        var options = CreateOptions();

        await using (var firstRunContext = CreateDbContext(options))
        {
            var seeder = CreateSeeder(firstRunContext);
            await seeder.SeedAsync();
        }

        InitialTaxObligationSeedResult secondResult;
        await using (var secondRunContext = CreateDbContext(options))
        {
            var seeder = CreateSeeder(secondRunContext);
            secondResult = await seeder.SeedAsync();
        }

        await using var dbContext = CreateDbContext(options);

        Assert.Empty(secondResult.Errors);
        Assert.Equal(0, secondResult.Created);
        Assert.Equal(37, secondResult.Updated);
        Assert.Equal(37, await dbContext.TaxObligations.CountAsync());

        var duplicateKeys = await dbContext.TaxObligations
            .GroupBy(obligation => new { obligation.DepartmentId, obligation.Name, obligation.TaxCategoryId })
            .Where(group => group.Count() > 1)
            .CountAsync();

        Assert.Equal(0, duplicateKeys);
    }

    [Fact]
    public async Task Seed_marks_incomplete_obligations_requires_review()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync();

        var arsp = await dbContext.TaxObligations.SingleAsync(obligation => obligation.Name == "ARSP (QST) Return");
        var transferPricing = await dbContext.TaxObligations.SingleAsync(obligation => obligation.Name == "TP light return");

        Assert.True(arsp.RequiresReview);
        Assert.Contains("Frequency", arsp.ReviewReason);
        Assert.Contains("Legal deadline", arsp.ReviewReason);
        Assert.True(transferPricing.RequiresReview);
        Assert.Contains("Number missing", transferPricing.ReviewReason);
    }

    [Fact]
    public async Task Monthly_obligations_create_twelve_rules()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync();

        var vat = await dbContext.TaxObligations
            .Include(obligation => obligation.ScheduleRules)
            .SingleAsync(obligation => obligation.Name == "VAT Return");

        Assert.Equal(12, vat.ScheduleRules.Count);
        Assert.Equal(Enumerable.Range(1, 12), vat.ScheduleRules.OrderBy(rule => rule.DueMonth).Select(rule => rule.DueMonth!.Value));
    }

    [Fact]
    public async Task Annual_obligations_create_only_active_month_rules()
    {
        await using var dbContext = CreateDbContext();
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync();

        var cit = await dbContext.TaxObligations
            .Include(obligation => obligation.ScheduleRules)
            .SingleAsync(obligation => obligation.Name == "Corporate Income Tax - CIT Return");

        var rule = Assert.Single(cit.ScheduleRules);
        Assert.Equal(4, rule.DueMonth);
        Assert.Equal("01/04\n15/04\n25/04", rule.RawReminderText);
        Assert.Equal("1,4,15,25", rule.ReminderDays);
    }

    [Fact]
    public async Task Existing_obligation_is_updated_instead_of_duplicated()
    {
        var options = CreateOptions();

        await using (var setupContext = CreateDbContext(options))
        {
            var legalEntity = new LegalEntity("ETHAN", "ETHAN TCM", "CD", null);
            var department = new Department("FINANCE", "Finance");
            var category = new TaxCategory("VALUE_ADDED_TAX", "Value Added Tax");
            var oldFrequency = new TaxFrequency("OLD", "Old", 1);
            var user = new User("seed.system", "Seed System User", "seed.system@local");
            var existing = new TaxObligation(
                legalEntity.Id,
                department.Id,
                category.Id,
                oldFrequency.Id,
                user.Id,
                "VAT Return",
                Domain.Enums.RiskLevel.Low,
                requiresPayment: false,
                DateTimeOffset.UtcNow);

            existing.UpdateSeedMetadata(999, "Old deadline", requiresReview: true, "Old review", DateTimeOffset.UtcNow);
            setupContext.AddRange(legalEntity, department, category, oldFrequency, user, existing);
            await setupContext.SaveChangesAsync();
        }

        InitialTaxObligationSeedResult result;
        await using (var seedContext = CreateDbContext(options))
        {
            var seeder = CreateSeeder(seedContext);
            result = await seeder.SeedAsync();
        }

        await using var dbContext = CreateDbContext(options);

        Assert.Empty(result.Errors);
        Assert.Equal(36, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Equal(37, await dbContext.TaxObligations.CountAsync());

        var vat = await dbContext.TaxObligations
            .Include(obligation => obligation.ScheduleRules)
            .SingleAsync(obligation => obligation.Name == "VAT Return");
        var monthlyFrequency = await dbContext.TaxFrequencies.SingleAsync(frequency => frequency.Name == "Monthly");

        Assert.Equal(monthlyFrequency.Id, vat.TaxFrequencyId);
        Assert.Equal("15th M+1", vat.LegalDeadline);
        Assert.False(vat.RequiresReview);
        Assert.Equal(12, vat.ScheduleRules.Count);
    }

    private static EthanTcmDbContext CreateDbContext()
    {
        return new EthanTcmDbContext(CreateOptions());
    }

    private static EthanTcmDbContext CreateDbContext(DbContextOptions<EthanTcmDbContext> options)
    {
        return new EthanTcmDbContext(options);
    }

    private static DbContextOptions<EthanTcmDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static InitialTaxObligationSeeder CreateSeeder(EthanTcmDbContext dbContext)
    {
        return new InitialTaxObligationSeeder(dbContext, new TestCurrentUserService());
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Login => null;
        public string? DisplayName => null;
        public string? Email => null;
        public Guid? DepartmentId => null;
        public bool IsAuthenticated => false;
        public bool IsActive => false;
        public IReadOnlyCollection<string> Roles => [];

        public bool IsInRole(string role)
        {
            return false;
        }
    }
}
