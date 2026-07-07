using System.Security.Cryptography;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EthanTcm.Infrastructure.Services;

public sealed class TaxDocumentService(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    IOptions<TaxDocumentStorageOptions> options)
    : ITaxDocumentService
{
    private const string AuditModule = "Tax Documents";
    private readonly TaxDocumentStorageOptions storageOptions = options.Value;

    public async Task<TaxDocumentUploadResult> UploadAsync(TaxDocumentUploadCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TaxDeclarationId == Guid.Empty)
        {
            return FailedUpload("Tax declaration is required.");
        }

        if (command.Content.Length == 0 || command.FileSizeBytes <= 0)
        {
            return FailedUpload("Document file is required.");
        }

        if (command.FileSizeBytes > storageOptions.MaxFileSizeBytes)
        {
            return FailedUpload($"Document exceeds the configured maximum size of {storageOptions.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(command.FileName).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) ||
            !storageOptions.AllowedExtensions.Any(item => item.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            return FailedUpload("Document extension is not allowed.");
        }

        if (!IsAllowedContentType(extension, command.ContentType))
        {
            return FailedUpload("Document content type is not allowed.");
        }

        var declaration = await dbContext.TaxDeclarations
            .AsNoTracking()
            .Include(item => item.TaxObligation)
            .ThenInclude(obligation => obligation!.Responsibles)
            .FirstOrDefaultAsync(item => item.Id == command.TaxDeclarationId, cancellationToken);
        if (declaration is null)
        {
            return FailedUpload("Tax declaration was not found.");
        }

        var userId = currentUserService.UserId;
        if (!userId.HasValue)
        {
            return FailedUpload("Current user is required.");
        }

        if (!CanUpload(declaration, command.DocumentType))
        {
            return FailedUpload("The current user is not allowed to upload this document.");
        }

        var version = await dbContext.TaxDocuments
            .Where(document =>
                document.TaxDeclarationId == command.TaxDeclarationId &&
                document.DocumentType == command.DocumentType)
            .MaxAsync(document => (int?)document.Version, cancellationToken) ?? 0;
        version++;

        var rootPath = ResolveRootPath();
        var declarationFolder = EnsureInsideRoot(rootPath, Path.Combine(rootPath, command.TaxDeclarationId.ToString("N")));
        Directory.CreateDirectory(declarationFolder);

        var storedFileName = $"{command.DocumentType}_v{version}_{RandomNumberGenerator.GetHexString(12)}.{extension}";
        var physicalPath = EnsureInsideRoot(rootPath, Path.Combine(declarationFolder, storedFileName));

        await using (var fileStream = File.Create(physicalPath))
        {
            await command.Content.CopyToAsync(fileStream, cancellationToken);
        }

        var document = new TaxDocument(
            command.TaxDeclarationId,
            command.DocumentType,
            SanitizeFileName(command.FileName),
            physicalPath,
            string.IsNullOrWhiteSpace(command.ContentType) ? "application/octet-stream" : command.ContentType,
            userId.Value,
            DateTimeOffset.UtcNow,
            command.FileSizeBytes,
            version);

        dbContext.TaxDocuments.Add(document);
        AddAudit("Upload", document, null, ToAuditPayload(document));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryDeleteFile(physicalPath);
            throw;
        }

        return new TaxDocumentUploadResult(true, document.Id, null);
    }

    public async Task<TaxDocumentDownloadResult?> GetDownloadAsync(Guid taxDocumentId, CancellationToken cancellationToken = default)
    {
        var authorization = await CanDownloadAsync(taxDocumentId, cancellationToken);
        if (!authorization.Exists || !authorization.IsAllowed)
        {
            return null;
        }

        var document = await dbContext.TaxDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taxDocumentId && !item.IsDeleted, cancellationToken);

        if (document is null || !IsStoredPathAllowed(document.FilePath) || !File.Exists(document.FilePath))
        {
            return null;
        }

        return new TaxDocumentDownloadResult(document.Id, document.FileName, document.ContentType, document.FilePath);
    }

    public async Task<TaxDocumentAuthorizationResult> CanDownloadAsync(Guid taxDocumentId, CancellationToken cancellationToken = default)
    {
        var documentAccess = await (
            from document in dbContext.TaxDocuments.AsNoTracking()
            join declaration in dbContext.TaxDeclarations.AsNoTracking()
                    .Include(declaration => declaration.TaxObligation)
                    .ThenInclude(obligation => obligation!.Responsibles)
                on document.TaxDeclarationId equals declaration.Id
            where document.Id == taxDocumentId && !document.IsDeleted
            select new
            {
                document.UploadedByUserId,
                document.DocumentType,
                Declaration = declaration
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (documentAccess is null)
        {
            return new TaxDocumentAuthorizationResult(false, false);
        }

        return new TaxDocumentAuthorizationResult(
            true,
            CanDownload(documentAccess.Declaration, documentAccess.UploadedByUserId, documentAccess.DocumentType));
    }

    public async Task<TaxDocumentDeleteResult> DeleteAsync(Guid taxDocumentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.TaxDocuments
            .FirstOrDefaultAsync(item => item.Id == taxDocumentId && !item.IsDeleted, cancellationToken);

        if (document is null)
        {
            return new TaxDocumentDeleteResult(false, "Document was not found.");
        }

        if (!currentUserService.UserId.HasValue)
        {
            return new TaxDocumentDeleteResult(false, "Current user is required.");
        }

        var declaration = await dbContext.TaxDeclarations
            .AsNoTracking()
            .Include(item => item.TaxObligation)
            .ThenInclude(obligation => obligation!.Responsibles)
            .FirstOrDefaultAsync(item => item.Id == document.TaxDeclarationId, cancellationToken);

        if (declaration is null || !CanDelete(declaration.AssignedToUserId, document.UploadedByUserId))
        {
            return new TaxDocumentDeleteResult(false, "The current user is not allowed to delete this document.");
        }

        var oldValue = ToAuditPayload(document);
        document.SoftDelete(currentUserService.UserId.Value, DateTimeOffset.UtcNow);
        AddAudit("Delete", document, oldValue, ToAuditPayload(document));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaxDocumentDeleteResult(true, null);
    }

    private string ResolveRootPath()
    {
        var rootPath = string.IsNullOrWhiteSpace(storageOptions.RootPath)
            ? "App_Data/Documents"
            : storageOptions.RootPath;

        return Path.GetFullPath(rootPath);
    }

    private string EnsureInsideRoot(string rootPath, string targetPath)
    {
        var fullRoot = Path.GetFullPath(rootPath);
        var fullTarget = Path.GetFullPath(targetPath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!fullTarget.StartsWith(fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison) &&
            !fullTarget.Equals(fullRoot, comparison))
        {
            throw new InvalidOperationException("Document storage path is outside the configured root.");
        }

        return fullTarget;
    }

    private bool IsStoredPathAllowed(string filePath)
    {
        try
        {
            EnsureInsideRoot(ResolveRootPath(), filePath);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private bool CanUpload(TaxDeclaration declaration, DocumentType documentType)
    {
        if (currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager))
        {
            return true;
        }

        if (documentType == DocumentType.PaymentProof)
        {
            if (currentUserService.IsInRole(ApplicationRoles.FinanceManager))
            {
                return true;
            }

            return currentUserService.UserId.HasValue &&
                declaration.TaxObligation?.Responsibles.Any(responsible =>
                    responsible.Type == ResponsibleType.PaymentProcessOwner &&
                    responsible.UserId == currentUserService.UserId.Value) == true;
        }

        return currentUserService.IsInRole(ApplicationRoles.Preparer) &&
            IsAssignedOrConfiguredPreparer(declaration);
    }

    private bool CanDownload(TaxDeclaration declaration, Guid uploadedByUserId, DocumentType documentType)
    {
        if (currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager) ||
            currentUserService.IsInRole(ApplicationRoles.Auditor))
        {
            return true;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return false;
        }

        var currentUserId = currentUserService.UserId.Value;
        if (declaration.AssignedToUserId == currentUserId || uploadedByUserId == currentUserId)
        {
            return true;
        }

        if (declaration.TaxObligation?.Responsibles.Any(responsible => responsible.UserId == currentUserId) == true)
        {
            return true;
        }

        return currentUserService.IsInRole(ApplicationRoles.FinanceManager) &&
            documentType == DocumentType.PaymentProof;
    }

    private bool IsAssignedOrConfiguredPreparer(TaxDeclaration declaration)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return false;
        }

        var currentUserId = currentUserService.UserId.Value;
        return declaration.AssignedToUserId == currentUserId ||
            declaration.TaxObligation?.Responsibles.Any(responsible =>
                responsible.Type == ResponsibleType.Preparer &&
                responsible.UserId == currentUserId) == true;
    }

    private bool CanDelete(Guid assignedToUserId, Guid uploadedByUserId)
    {
        if (currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager))
        {
            return true;
        }

        return currentUserService.IsInRole(ApplicationRoles.Preparer) &&
            currentUserService.UserId == assignedToUserId &&
            currentUserService.UserId == uploadedByUserId;
    }

    private bool IsAllowedContentType(string extension, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        var normalized = contentType.Split(';', StringSplitOptions.TrimEntries)[0].ToLowerInvariant();
        if (storageOptions.AllowedContentTypes.Length > 0 &&
            !storageOptions.AllowedContentTypes.Any(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return extension switch
        {
            "pdf" => normalized == "application/pdf",
            "xlsx" => normalized == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "xls" => normalized == "application/vnd.ms-excel",
            "docx" => normalized == "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "jpg" or "jpeg" => normalized == "image/jpeg",
            "png" => normalized == "image/png",
            _ => false
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }

        return string.IsNullOrWhiteSpace(safeName) ? "document" : safeName;
    }

    private static void TryDeleteFile(string physicalPath)
    {
        try
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static TaxDocumentUploadResult FailedUpload(string message)
    {
        return new TaxDocumentUploadResult(false, null, message);
    }

    private void AddAudit(string action, TaxDocument document, object? oldValue, object? newValue)
    {
        auditService.Add(new AuditEntry(
            action,
            nameof(TaxDocument),
            document.Id.ToString(),
            oldValue,
            newValue,
            AuditModule,
            "Web"));
    }

    private static object ToAuditPayload(TaxDocument document)
    {
        return new
        {
            document.Id,
            document.TaxDeclarationId,
            document.DocumentType,
            document.FileName,
            document.FilePath,
            document.ContentType,
            document.FileSizeBytes,
            document.Version,
            document.IsDeleted
        };
    }
}
