namespace EthanTcm.Domain.Enums;

public enum ImportBatchStatus
{
    Pending = 0,
    Validating = 1,
    FailedValidation = 2,
    Imported = 3,
    Failed = 4,
    Cancelled = 5
}
