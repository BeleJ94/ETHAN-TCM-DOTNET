using EthanTcm.Application.Abstractions;
using EthanTcm.Application.TaxCatalog;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Tests;

public sealed class TaxCatalogSynchronizationServiceTests
{
    [Fact]
    public async Task Dry_run_reports_changes_without_modifying_database()
    {
        var options = Options();
        await SeedAsync(options);
        await using var context = new EthanTcmDbContext(options);
        var before = await SnapshotAsync(context);

        var report = await Service(context).SynchronizeAsync(dryRun: true);

        Assert.True(report.DryRun);
        Assert.True(report.Linked >= 37);
        Assert.Equal(0, await context.TaxObligationVersions.CountAsync());
        Assert.Equal(before, await SnapshotAsync(context));
    }

    [Fact]
    public async Task Synchronization_preserves_existing_ids_and_declarations_and_is_idempotent()
    {
        var options = Options();
        await SeedAsync(options);
        Guid vatId;
        Guid declarationId;
        await using (var setup = new EthanTcmDbContext(options))
        {
            var vat = await setup.TaxObligations.Include(x => x.Responsibles).SingleAsync(x => x.SourceNumber == 4);
            vatId = vat.Id;
            var period = new TaxPeriod(2026, 1, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "Jan 2026");
            setup.TaxPeriods.Add(period);
            var declaration = new TaxDeclaration(vat.Id, period.Id, new DateOnly(2026, 2, 15), "Jan 2026", true, vat.Responsibles.First().UserId);
            declarationId = declaration.Id;
            setup.TaxDeclarations.Add(declaration);
            await setup.SaveChangesAsync();
        }

        int obligationCount;
        int aliasCount;
        int versionCount;
        await using (var first = new EthanTcmDbContext(options))
        {
            await Service(first).SynchronizeAsync(dryRun: false);
            obligationCount = await first.TaxObligations.CountAsync();
            aliasCount = await first.TaxObligationAliases.CountAsync();
            versionCount = await first.TaxObligationVersions.CountAsync();
        }

        await using (var second = new EthanTcmDbContext(options))
        {
            var secondReport = await Service(second).SynchronizeAsync(dryRun: false);
            Assert.Equal(0, secondReport.Created);
            Assert.Equal(0, secondReport.Versioned);
            Assert.Equal(obligationCount, await second.TaxObligations.CountAsync());
            Assert.Equal(aliasCount, await second.TaxObligationAliases.CountAsync());
            Assert.Equal(versionCount, await second.TaxObligationVersions.CountAsync());

            var vat = await second.TaxObligations.SingleAsync(x => x.CanonicalCode == "VAT-RETURN");
            var declaration = await second.TaxDeclarations.SingleAsync(x => x.Id == declarationId);
            Assert.Equal(vatId, vat.Id);
            Assert.Equal(vatId, declaration.TaxObligationId);
            Assert.NotNull(declaration.TaxObligationVersionId);
        }
    }

    [Fact]
    public async Task Existing_responsibilities_are_preserved_and_new_obligations_are_inactive()
    {
        var options = Options();
        await SeedAsync(options);
        Guid payrollResponsibleId;
        await using (var before = new EthanTcmDbContext(options))
        {
            payrollResponsibleId = (await before.TaxObligations.Include(x => x.Responsibles)
                .SingleAsync(x => x.SourceNumber == 3)).Responsibles.Single().UserId;
        }

        await using var context = new EthanTcmDbContext(options);
        await Service(context).SynchronizeAsync(false);

        var payroll = await context.TaxObligations.Include(x => x.Responsibles).SingleAsync(x => x.CanonicalCode == "PAYROLL-TAXES");
        Assert.Contains(payroll.Responsibles, x => x.UserId == payrollResponsibleId);
        var payrollPreparer = payroll.Responsibles.Single(x => x.Type == ResponsibleType.Preparer);
        Assert.Equal("mtutonda@katangamining.com", (await context.Users.SingleAsync(x => x.Id == payrollPreparer.UserId)).Email);
        var newTaxes = await context.TaxObligations.Where(x => x.CanonicalCode == "IMPORT-DUTIES" || x.CanonicalCode == "WHT-CAPITAL-GAIN").ToListAsync();
        Assert.All(newTaxes, x =>
        {
            Assert.False(x.IsActive);
            Assert.True(x.RequiresReview);
            Assert.Equal(TaxCatalogValidationStatus.PendingValidation, x.CatalogValidationStatus);
        });
    }

    [Fact]
    public async Task Conceptual_duplicates_and_allocations_do_not_create_tax_obligations()
    {
        var options = Options();
        await SeedAsync(options);
        await using var context = new EthanTcmDbContext(options);
        await Service(context).SynchronizeAsync(false);

        Assert.Equal(1, await context.TaxObligations.CountAsync(x => x.CanonicalCode == "MINING-ROYALTY"));
        Assert.Equal(1, await context.TaxObligations.CountAsync(x => x.CanonicalCode == "MINING-CONCESSION-SURFACE-TAX"));
        Assert.Equal(5, await context.TaxAllocationRules.CountAsync());
        Assert.False(await context.TaxObligations.AnyAsync(x => x.Name.Contains("44%") || x.CanonicalCode == "CUSTOMS-REGIME"));
    }

    [Fact]
    public async Task Conflicts_remain_pending_validation()
    {
        var options = Options();
        await SeedAsync(options);
        await using var context = new EthanTcmDbContext(options);
        await Service(context).SynchronizeAsync(false);

        var conflicts = await context.TaxCatalogConflicts.ToListAsync();
        Assert.Equal(ConsolidatedTaxCatalog.KnownConflicts.Count, conflicts.Count);
        Assert.All(conflicts, x => Assert.Equal("PendingValidation", x.Status));
    }

    [Fact]
    public async Task Source_email_typo_does_not_create_duplicate_login()
    {
        var options = Options();
        await SeedAsync(options);
        await using var context = new EthanTcmDbContext(options);

        await Service(context).SynchronizeAsync(false);

        Assert.Equal(1, await context.Users.CountAsync(x => x.Login == "aelonga"));
    }

    [Fact]
    public void Legal_change_is_represented_by_a_new_version_without_mutating_old_version()
    {
        var obligationId = Guid.NewGuid();
        var oldVersion = new TaxObligationVersion(obligationId, 1, new DateOnly(2025, 1, 1), "Annual",
            "30 April", "30 April", "30 April", null, TaxCatalogValidationStatus.Validated, false, "Initial", "HASH1");
        oldVersion.Close(new DateOnly(2025, 12, 31));
        var newVersion = new TaxObligationVersion(obligationId, 2, new DateOnly(2026, 1, 1), "Annual",
            "31 March", "31 March", "31 March", null, TaxCatalogValidationStatus.PendingValidation, true, "Legal change", "HASH2");

        Assert.Equal("30 April", oldVersion.RawDeadlineText);
        Assert.Equal(new DateOnly(2025, 12, 31), oldVersion.EffectiveTo);
        Assert.Equal(2, newVersion.VersionNumber);
    }

    [Fact]
    public async Task Cancellation_leaves_catalog_unchanged()
    {
        var options = Options();
        await SeedAsync(options);
        await using var context = new EthanTcmDbContext(options);
        var before = await SnapshotAsync(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(context).SynchronizeAsync(false, cancellation.Token));
        Assert.Equal(before, await SnapshotAsync(context));
    }

    private static async Task SeedAsync(DbContextOptions<EthanTcmDbContext> options)
    {
        await using var context = new EthanTcmDbContext(options);
        var result = await new InitialTaxObligationSeeder(context, new TestCurrentUser()).SeedAsync();
        Assert.Empty(result.Errors);
    }

    private static TaxCatalogSynchronizationService Service(EthanTcmDbContext context) =>
        new(context, new TestCurrentUser());

    private static DbContextOptions<EthanTcmDbContext> Options() =>
        new DbContextOptionsBuilder<EthanTcmDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static async Task<string> SnapshotAsync(EthanTcmDbContext context) =>
        $"{await context.TaxObligations.CountAsync()}:{await context.TaxObligationVersions.CountAsync()}:{await context.TaxObligationAliases.CountAsync()}:{await context.TaxCatalogConflicts.CountAsync()}";

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Login => null;
        public string? DisplayName => null;
        public string? Email => null;
        public Guid? DepartmentId => null;
        public bool IsAuthenticated => false;
        public bool IsActive => false;
        public IReadOnlyCollection<string> Roles => [];
        public bool IsInRole(string role) => false;
    }
}
