using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class TaxDocument : AuditableEntity
{
    private TaxDocument()
    {
    }

    public TaxDocument(
        Guid taxDeclarationId,
        DocumentType documentType,
        string fileName,
        string filePath,
        string contentType,
        Guid uploadedByUserId,
        DateTimeOffset uploadedAt,
        long fileSizeBytes = 0,
        int version = 1)
    {
        if (fileSizeBytes < 0)
        {
            throw new DomainException("File size cannot be negative.");
        }

        if (version <= 0)
        {
            throw new DomainException("Document version must be greater than zero.");
        }

        TaxDeclarationId = EntityGuards.Required(taxDeclarationId, nameof(TaxDeclarationId));
        DocumentType = documentType;
        FileName = EntityGuards.Required(fileName, nameof(FileName));
        FilePath = EntityGuards.Required(filePath, nameof(FilePath));
        ContentType = EntityGuards.Required(contentType, nameof(ContentType));
        UploadedByUserId = EntityGuards.Required(uploadedByUserId, nameof(UploadedByUserId));
        UploadedAt = uploadedAt;
        FileSizeBytes = fileSizeBytes;
        Version = version;
    }

    public Guid TaxDeclarationId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int Version { get; private set; } = 1;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    public void SoftDelete(Guid deletedByUserId, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        DeletedByUserId = EntityGuards.Required(deletedByUserId, nameof(deletedByUserId));
        DeletedAt = deletedAt;
        IsDeleted = true;
        MarkUpdated(deletedAt);
    }
}
