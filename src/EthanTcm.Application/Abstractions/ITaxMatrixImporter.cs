namespace EthanTcm.Application.Abstractions;

public interface ITaxMatrixImporter
{
    Task<TaxMatrixImportResult> ImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);
}

public sealed record TaxMatrixImportResult(int TotalRows, int ValidRows, int InvalidRows);
