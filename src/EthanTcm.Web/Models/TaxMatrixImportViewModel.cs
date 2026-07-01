using EthanTcm.Application.Abstractions;

namespace EthanTcm.Web.Models;

public sealed class TaxMatrixImportViewModel
{
    public Guid? ImportBatchId { get; init; }
    public string? FileName { get; init; }
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public bool HasCriticalErrors { get; init; }
    public int? ImportedObligations { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyCollection<TaxMatrixImportErrorDto> Errors { get; init; } = [];

    public bool HasPreview => ImportBatchId.HasValue;
    public bool CanImport => HasPreview && !HasCriticalErrors;
}
