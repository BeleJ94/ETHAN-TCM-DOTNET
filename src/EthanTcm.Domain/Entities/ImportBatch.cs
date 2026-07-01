using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class ImportBatch : AuditableEntity
{
    private readonly List<ImportError> _errors = [];

    private ImportBatch()
    {
    }

    public ImportBatch(string fileName, Guid importedByUserId, DateTimeOffset importedAt)
    {
        FileName = EntityGuards.Required(fileName, nameof(FileName));
        ImportedByUserId = EntityGuards.Required(importedByUserId, nameof(ImportedByUserId));
        ImportedAt = importedAt;
        CreatedAt = importedAt;
    }

    public string FileName { get; private set; } = string.Empty;
    public Guid ImportedByUserId { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }
    public ImportBatchStatus Status { get; private set; } = ImportBatchStatus.Pending;
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int InvalidRows { get; private set; }
    public string? ErrorReportPath { get; private set; }
    public IReadOnlyCollection<ImportError> Errors => _errors.AsReadOnly();

    public void RegisterValidationResult(int totalRows, int validRows, int invalidRows, DateTimeOffset timestamp)
    {
        if (totalRows < 0 || validRows < 0 || invalidRows < 0)
        {
            throw new DomainException("Import row counts cannot be negative.");
        }

        TotalRows = totalRows;
        ValidRows = validRows;
        InvalidRows = invalidRows;
        Status = invalidRows == 0 ? ImportBatchStatus.Validating : ImportBatchStatus.FailedValidation;
        MarkUpdated(timestamp);
    }

    public void AddError(int rowNumber, string columnName, string message, DateTimeOffset timestamp)
    {
        _errors.Add(new ImportError(Id, rowNumber, columnName, message));
        InvalidRows++;
        Status = ImportBatchStatus.FailedValidation;
        MarkUpdated(timestamp);
    }

    public void MarkImported(DateTimeOffset timestamp)
    {
        if (_errors.Count > 0)
        {
            throw new DomainException("An import batch with errors cannot be marked as imported.");
        }

        Status = ImportBatchStatus.Imported;
        MarkUpdated(timestamp);
    }
}
