using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface ITaxDocumentService
{
    Task<TaxDocumentUploadResult> UploadAsync(TaxDocumentUploadCommand command, CancellationToken cancellationToken = default);
    Task<TaxDocumentDownloadResult?> GetDownloadAsync(Guid taxDocumentId, CancellationToken cancellationToken = default);
    Task<TaxDocumentAuthorizationResult> CanDownloadAsync(Guid taxDocumentId, CancellationToken cancellationToken = default);
    Task<TaxDocumentDeleteResult> DeleteAsync(Guid taxDocumentId, CancellationToken cancellationToken = default);
}

public sealed record TaxDocumentUploadCommand(
    Guid TaxDeclarationId,
    DocumentType DocumentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content);

public sealed record TaxDocumentUploadResult(
    bool Success,
    Guid? TaxDocumentId,
    string? ErrorMessage);

public sealed record TaxDocumentDownloadResult(
    Guid TaxDocumentId,
    string FileName,
    string ContentType,
    string PhysicalPath);

public sealed record TaxDocumentAuthorizationResult(
    bool Exists,
    bool IsAllowed);

public sealed record TaxDocumentDeleteResult(
    bool Success,
    string? ErrorMessage);
