namespace EthanTcm.Application.Abstractions;

public interface IInitialTaxObligationSeeder
{
    Task<InitialTaxObligationSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}

public sealed record InitialTaxObligationSeedResult(
    int Created,
    int Updated,
    int RequiresReview,
    IReadOnlyCollection<string> Errors);
