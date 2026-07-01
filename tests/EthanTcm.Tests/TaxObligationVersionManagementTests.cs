using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Tests;

public sealed class TaxObligationVersionManagementTests
{
    [Fact]
    public async Task New_fiscal_version_closes_previous_version_and_preserves_legal_history()
    {
        var options = Options();
        await SeedCatalogAsync(options);

        await using var context = new EthanTcmDbContext(options);
        var obligation = await context.TaxObligations.SingleAsync(item => item.CanonicalCode == "VAT-RETURN");
        var previous = await context.TaxObligationVersions
            .SingleAsync(item => item.TaxObligationId == obligation.Id);
        var authority = await context.TaxAuthorities.SingleAsync(item => item.Code == "DGI");
        var effectiveFrom = previous.EffectiveFrom.AddYears(1);
        var service = new TaxObligationReferentialService(context, new TestAuditService(context));

        var versionId = await service.CreateVersionAsync(
            obligation.Id,
            Command(effectiveFrom, authority.Id));

        Assert.NotNull(versionId);
        var versions = await context.TaxObligationVersions
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderBy(item => item.VersionNumber)
            .ToArrayAsync();
        Assert.Equal(2, versions.Length);
        Assert.Equal(effectiveFrom.AddDays(-1), versions[0].EffectiveTo);
        Assert.Equal(2, versions[1].VersionNumber);
        Assert.Equal(effectiveFrom, versions[1].EffectiveFrom);
        Assert.Equal("Réforme de la TVA", versions[1].ChangeReason);
        Assert.Equal(authority.Id, versions[1].TaxAuthorityId);
        Assert.Equal(TaxCatalogValidationStatus.PendingValidation, versions[1].ValidationStatus);
        Assert.Equal(TaxCatalogValidationStatus.PendingValidation, obligation.CatalogValidationStatus);
        Assert.True(obligation.RequiresReview);
        Assert.Equal("Réforme de la TVA", obligation.ReviewReason);

        Assert.Equal(2, await context.TaxRateRules.CountAsync(item => item.TaxObligationVersionId == versionId));
        Assert.Single(await context.TaxLegalReferences.Where(item => item.TaxObligationVersionId == versionId).ToArrayAsync());
        Assert.Single(await context.TaxPenaltyRules.Where(item => item.TaxObligationVersionId == versionId).ToArrayAsync());
        Assert.Single(await context.TaxProcessTemplates.Where(item => item.TaxObligationVersionId == versionId).ToArrayAsync());
        Assert.True(await context.AuditLogs.AnyAsync(log =>
            log.EntityId == obligation.Id.ToString() && log.Action == "CreateVersion"));
    }

    [Fact]
    public async Task New_version_rejects_an_effective_date_that_does_not_follow_latest_version()
    {
        var options = Options();
        await SeedCatalogAsync(options);

        await using var context = new EthanTcmDbContext(options);
        var obligation = await context.TaxObligations.SingleAsync(item => item.CanonicalCode == "VAT-RETURN");
        var previous = await context.TaxObligationVersions
            .SingleAsync(item => item.TaxObligationId == obligation.Id);
        var service = new TaxObligationReferentialService(context, new TestAuditService(context));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateVersionAsync(obligation.Id, Command(previous.EffectiveFrom, null)));

        Assert.Contains("postérieure", exception.Message);
        Assert.Single(await context.TaxObligationVersions
            .Where(item => item.TaxObligationId == obligation.Id)
            .ToArrayAsync());
        Assert.Null(previous.EffectiveTo);
    }

    [Fact]
    public async Task Version_creation_context_prefills_current_legal_rules()
    {
        var options = Options();
        await SeedCatalogAsync(options);

        await using var context = new EthanTcmDbContext(options);
        var obligation = await context.TaxObligations.SingleAsync(item => item.CanonicalCode == "VAT-RETURN");
        var service = new TaxObligationReferentialService(context, new TestAuditService(context));

        var creationContext = await service.GetVersionCreationContextAsync(obligation.Id);

        Assert.NotNull(creationContext);
        Assert.Equal(2, creationContext.NextVersionNumber);
        Assert.NotEmpty(creationContext.Authorities);
        Assert.NotEmpty(creationContext.CurrentRateRules);
        Assert.NotEmpty(creationContext.CurrentLegalReferences);
    }

    [Fact]
    public async Task Catalog_resynchronization_does_not_overwrite_a_manual_reform()
    {
        var options = Options();
        await SeedCatalogAsync(options);

        await using var context = new EthanTcmDbContext(options);
        var currentUser = new TestCurrentUser();
        var obligation = await context.TaxObligations.SingleAsync(item => item.CanonicalCode == "VAT-RETURN");
        var previous = await context.TaxObligationVersions
            .SingleAsync(item => item.TaxObligationId == obligation.Id);
        var service = new TaxObligationReferentialService(context, new TestAuditService(context));
        var manualVersionId = await service.CreateVersionAsync(
            obligation.Id,
            Command(previous.EffectiveFrom.AddYears(1), null));

        var report = await new TaxCatalogSynchronizationService(context, currentUser)
            .SynchronizeAsync(false);

        Assert.Equal(0, report.Versioned);
        var versions = await context.TaxObligationVersions
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderBy(item => item.VersionNumber)
            .ToArrayAsync();
        Assert.Equal(2, versions.Length);
        Assert.Equal(manualVersionId, versions[1].Id);
        Assert.Null(versions[1].EffectiveTo);
        Assert.Equal("Réforme de la TVA", versions[1].ChangeReason);
    }

    private static TaxObligationVersionCreateCommand Command(DateOnly effectiveFrom, Guid? authorityId) =>
        new(
            effectiveFrom,
            authorityId,
            "Mensuelle",
            "Dépôt au plus tard le 15 du mois suivant",
            "Paiement au plus tard le 15 du mois suivant",
            "Le 15 du mois suivant",
            "Clôture mensuelle",
            TaxCatalogValidationStatus.PendingValidation,
            true,
            "Réforme de la TVA",
            ["Taux normal : 18 %", "Taux réduit : 8 %"],
            ["Loi de finances 2027, article 12"],
            ["Intérêt de retard applicable"],
            ["Préparer, valider, déposer et payer"]);

    private static async Task SeedCatalogAsync(DbContextOptions<EthanTcmDbContext> options)
    {
        await using var context = new EthanTcmDbContext(options);
        var currentUser = new TestCurrentUser();
        var seedResult = await new InitialTaxObligationSeeder(context, currentUser).SeedAsync();
        Assert.Empty(seedResult.Errors);
        await new TaxCatalogSynchronizationService(context, currentUser).SynchronizeAsync(false);
    }

    private static DbContextOptions<EthanTcmDbContext> Options() =>
        new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

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
