using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class ImportError : AuditableEntity
{
    private ImportError()
    {
    }

    public ImportError(Guid importBatchId, int rowNumber, string columnName, string message)
    {
        if (rowNumber <= 0)
        {
            throw new DomainException("Import row number must be greater than zero.");
        }

        ImportBatchId = EntityGuards.Required(importBatchId, nameof(ImportBatchId));
        RowNumber = rowNumber;
        ColumnName = EntityGuards.Required(columnName, nameof(ColumnName));
        Message = EntityGuards.Required(message, nameof(Message));
    }

    public Guid ImportBatchId { get; private set; }
    public int RowNumber { get; private set; }
    public string ColumnName { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
}
