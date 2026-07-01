using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Tests;

public sealed class TaxDeclarationGenerationServiceTests
{
    [Fact]
    public async Task GenerateAnnual_creates_monthly_and_annual_declarations_from_seeded_obligations()
    {
        var options = CreateOptions();
        await SeedInitialObligationsAsync(options);

        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext);

        var result = await service.GenerateAnnualAsync(2026);

        Assert.DoesNotContain(result.Items, item => !item.Created && item.SkipReason != "Already generated.");
        Assert.Equal(0, result.SkippedDuplicates);

        var vat = await dbContext.TaxObligations.SingleAsync(obligation => obligation.Name == "VAT Return");
        var vatDeclarations = await dbContext.TaxDeclarations
            .Where(declaration => declaration.TaxObligationId == vat.Id)
            .OrderBy(declaration => declaration.PeriodLabel)
            .ToArrayAsync();

        Assert.Equal(12, vatDeclarations.Length);
        Assert.All(vatDeclarations, declaration => Assert.Equal(TaxDeclarationStatus.ToPrepare, declaration.Status));
        Assert.Equal(new DateOnly(2026, 1, 15), vatDeclarations[0].DueDate);
        Assert.Equal(new DateOnly(2025, 12, 31), vatDeclarations[0].ReminderDate);

        var cit = await dbContext.TaxObligations.SingleAsync(obligation => obligation.Name == "Corporate Income Tax - CIT Return");
        var citDeclaration = await dbContext.TaxDeclarations.SingleAsync(declaration => declaration.TaxObligationId == cit.Id);

        Assert.Equal("2026", citDeclaration.PeriodLabel);
        Assert.Equal(new DateOnly(2026, 4, 1), citDeclaration.DueDate);
    }

    [Fact]
    public async Task GenerateAnnual_is_idempotent()
    {
        var options = CreateOptions();
        await SeedInitialObligationsAsync(options);

        int firstCreated;
        await using (var firstContext = CreateDbContext(options))
        {
            var service = CreateService(firstContext);
            firstCreated = (await service.GenerateAnnualAsync(2026)).CreatedDeclarations;
        }

        TaxDeclarationGenerationResult secondResult;
        await using (var secondContext = CreateDbContext(options))
        {
            var service = CreateService(secondContext);
            secondResult = await service.GenerateAnnualAsync(2026);
        }

        await using var dbContext = CreateDbContext(options);

        Assert.True(firstCreated > 0);
        Assert.Equal(0, secondResult.CreatedDeclarations);
        Assert.Equal(firstCreated, secondResult.SkippedDuplicates);

        var duplicateDeclarations = await dbContext.TaxDeclarations
            .GroupBy(declaration => new { declaration.TaxObligationId, declaration.TaxPeriodId })
            .Where(group => group.Count() > 1)
            .CountAsync();

        Assert.Equal(0, duplicateDeclarations);
    }

    [Fact]
    public async Task GenerateAnnual_does_not_auto_generate_as_needed_obligations()
    {
        var options = CreateOptions();
        await SeedInitialObligationsAsync(options);

        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext);

        await service.GenerateAnnualAsync(2026);

        var blastingPermit = await dbContext.TaxObligations.SingleAsync(obligation => obligation.Name == "Tax on Temporary Blasting Permit Return");

        Assert.False(await dbContext.TaxDeclarations.AnyAsync(declaration => declaration.TaxObligationId == blastingPermit.Id));
    }

    [Fact]
    public async Task CreateManual_creates_not_applicable_declaration_for_as_needed_obligation()
    {
        var options = CreateOptions();
        await SeedInitialObligationsAsync(options);

        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext);
        var obligation = await dbContext.TaxObligations.SingleAsync(item => item.Name == "Tax on Temporary Blasting Permit Return");
        var user = await dbContext.Users.SingleAsync(item => item.Login == "seed.system");

        var result = await service.CreateManualAsync(new TaxDeclarationManualCreationCommand(
            obligation.Id,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 6, 12),
            "Blasting permit - June 2026",
            user.Id,
            IsNotApplicable: true));

        Assert.True(result.Created);
        Assert.NotNull(result.TaxDeclarationId);

        var declaration = await dbContext.TaxDeclarations.SingleAsync(declaration => declaration.Id == result.TaxDeclarationId);
        var period = await dbContext.TaxPeriods.SingleAsync(period => period.Id == declaration.TaxPeriodId);

        Assert.Equal(TaxDeclarationStatus.NotApplicable, declaration.Status);
        Assert.Equal("Manual", period.PeriodType);
        Assert.Equal("Blasting permit - June 2026", declaration.PeriodLabel);
    }

    [Fact]
    public async Task CreateManual_rejects_duplicate_label_for_same_obligation()
    {
        var options = CreateOptions();
        await SeedInitialObligationsAsync(options);

        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext);
        var obligation = await dbContext.TaxObligations.SingleAsync(item => item.Name == "Tax on Temporary Blasting Permit Return");
        var user = await dbContext.Users.SingleAsync(item => item.Login == "seed.system");
        var command = new TaxDeclarationManualCreationCommand(
            obligation.Id,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 15),
            null,
            "Duplicate manual label",
            user.Id,
            IsNotApplicable: false);

        var firstResult = await service.CreateManualAsync(command);
        var secondResult = await service.CreateManualAsync(command);

        Assert.True(firstResult.Created);
        Assert.False(secondResult.Created);
        Assert.Contains("same label", secondResult.ErrorMessage);
    }

    private static async Task SeedInitialObligationsAsync(DbContextOptions<EthanTcmDbContext> options)
    {
        await using var dbContext = CreateDbContext(options);
        var seeder = new InitialTaxObligationSeeder(dbContext, new TestCurrentUserService());
        var result = await seeder.SeedAsync();

        Assert.Empty(result.Errors);
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

    private static TaxDeclarationGenerationService CreateService(EthanTcmDbContext dbContext)
    {
        return new TaxDeclarationGenerationService(dbContext, new TestCurrentUserService(), new TestAuditService(dbContext));
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
