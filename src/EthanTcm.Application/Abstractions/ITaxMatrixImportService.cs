namespace EthanTcm.Application.Abstractions;

public interface ITaxMatrixImportService
{
    Task<TaxMatrixPreviewResult> PreviewAsync(
        Stream fileStream,
        string fileName,
        Guid importedByUserId,
        CancellationToken cancellationToken = default);

    Task<TaxMatrixCommitResult> CommitAsync(
        Guid importBatchId,
        CancellationToken cancellationToken = default);
}

public sealed record TaxMatrixPreviewResult(
    Guid ImportBatchId,
    string FileName,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    bool HasCriticalErrors,
    IReadOnlyCollection<TaxMatrixImportErrorDto> Errors);

public sealed record TaxMatrixCommitResult(
    Guid ImportBatchId,
    int ImportedObligations,
    bool Imported,
    string? ErrorMessage);

public sealed record TaxMatrixImportErrorDto(
    int RowNumber,
    string ColumnName,
    string Message);
