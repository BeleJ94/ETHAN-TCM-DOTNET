using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class DeclarationDocument : AuditableEntity
{
    private DeclarationDocument()
    {
    }

    public DeclarationDocument(Guid taxDeclarationId, string documentType, string fileName, string filePath, string contentType, Guid uploadedByUserId)
    {
        TaxDeclarationId = taxDeclarationId;
        DocumentType = documentType;
        FileName = fileName;
        FilePath = filePath;
        ContentType = contentType;
        UploadedByUserId = uploadedByUserId;
    }

    public Guid TaxDeclarationId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; } = DateTimeOffset.UtcNow;
}
