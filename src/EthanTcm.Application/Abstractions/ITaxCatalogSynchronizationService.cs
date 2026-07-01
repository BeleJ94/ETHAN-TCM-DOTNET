namespace EthanTcm.Application.Abstractions;

public interface ITaxCatalogSynchronizationService
{
    Task<TaxCatalogSynchronizationReport> SynchronizeAsync(
        bool dryRun,
        CancellationToken cancellationToken = default);
}

public sealed record TaxCatalogSynchronizationReport(
    bool DryRun,
    int Linked,
    int Enriched,
    int Created,
    int Versioned,
    int AliasesCreated,
    int SourceReferencesCreated,
    int DuplicatesMerged,
    int Conflicts,
    int Skipped,
    IReadOnlyCollection<string> ActiveCanonicalCodes,
    IReadOnlyCollection<string> InactiveCanonicalCodes,
    IReadOnlyDictionary<Guid, string> PreservedIdentifiers,
    IReadOnlyCollection<string> Messages);
