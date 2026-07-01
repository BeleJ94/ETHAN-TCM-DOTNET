using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EthanTcm.Tests;

public sealed class TaxDocumentServiceTests
{
    [Fact]
    public async Task Upload_stores_file_metadata_and_audit_log()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var service = CreateService(dbContext, setup.UserId);
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await service.UploadAsync(new TaxDocumentUploadCommand(
            setup.DeclarationId,
            DocumentType.SubmissionProof,
            "submission.pdf",
            "application/pdf",
            content.Length,
            content));

        Assert.True(result.Success, result.ErrorMessage);
        var document = await dbContext.TaxDocuments.SingleAsync(document => document.Id == result.TaxDocumentId);
        Assert.Equal(1, document.Version);
        Assert.Equal(4, document.FileSizeBytes);
        Assert.True(File.Exists(document.FilePath));
        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.EntityName == nameof(TaxDocument) && log.Action == "Upload"));
    }

    [Fact]
    public async Task Upload_increments_version_per_declaration_and_type()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var service = CreateService(dbContext, setup.UserId);

        await UploadAsync(service, setup.DeclarationId, "submission-1.pdf");
        await UploadAsync(service, setup.DeclarationId, "submission-2.pdf");

        var versions = await dbContext.TaxDocuments
            .Where(document => document.TaxDeclarationId == setup.DeclarationId)
            .OrderBy(document => document.Version)
            .Select(document => document.Version)
            .ToArrayAsync();

        Assert.Equal([1, 2], versions);
    }

    [Fact]
    public async Task Upload_rejects_disallowed_extension()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var service = CreateService(dbContext, setup.UserId);
        await using var content = new MemoryStream([1]);

        var result = await service.UploadAsync(new TaxDocumentUploadCommand(
            setup.DeclarationId,
            DocumentType.Other,
            "script.exe",
            "application/octet-stream",
            content.Length,
            content));

        Assert.False(result.Success);
        Assert.Contains("extension", result.ErrorMessage);
    }

    [Fact]
    public async Task Upload_rejects_disallowed_content_type()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var service = CreateService(dbContext, setup.UserId);
        await using var content = new MemoryStream([1]);

        var result = await service.UploadAsync(new TaxDocumentUploadCommand(
            setup.DeclarationId,
            DocumentType.SubmissionProof,
            "submission.pdf",
            "application/x-msdownload",
            content.Length,
            content));

        Assert.False(result.Success);
        Assert.Contains("content type", result.ErrorMessage);
    }

    [Fact]
    public async Task Upload_rejects_unassigned_preparer()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var service = CreateService(dbContext, Guid.NewGuid());
        await using var content = new MemoryStream([1]);

        var result = await service.UploadAsync(new TaxDocumentUploadCommand(
            setup.DeclarationId,
            DocumentType.SubmissionProof,
            "submission.pdf",
            "application/pdf",
            content.Length,
            content));

        Assert.False(result.Success);
        Assert.Contains("not allowed", result.ErrorMessage);
    }

    [Fact]
    public async Task Download_ignores_logically_deleted_documents()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var service = CreateService(dbContext, setup.UserId);
        var upload = await UploadAsync(service, setup.DeclarationId, "submission.pdf");

        var beforeDelete = await service.GetDownloadAsync(upload.TaxDocumentId!.Value);
        var delete = await service.DeleteAsync(upload.TaxDocumentId.Value);
        var afterDelete = await service.GetDownloadAsync(upload.TaxDocumentId.Value);

        Assert.NotNull(beforeDelete);
        Assert.True(delete.Success, delete.ErrorMessage);
        Assert.Null(afterDelete);
        Assert.True(await dbContext.TaxDocuments.AnyAsync(document => document.Id == upload.TaxDocumentId && document.IsDeleted));
        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.EntityName == nameof(TaxDocument) && log.Action == "Delete"));
    }

    [Fact]
    public async Task Download_rejects_unrelated_preparer()
    {
        await using var dbContext = CreateDbContext();
        var setup = await CreateDeclarationAsync(dbContext);
        var ownerService = CreateService(dbContext, setup.UserId);
        var upload = await UploadAsync(ownerService, setup.DeclarationId, "submission.pdf");
        var unrelatedService = CreateService(dbContext, Guid.NewGuid());

        var authorization = await unrelatedService.CanDownloadAsync(upload.TaxDocumentId!.Value);
        var download = await unrelatedService.GetDownloadAsync(upload.TaxDocumentId.Value);

        Assert.True(authorization.Exists);
        Assert.False(authorization.IsAllowed);
        Assert.Null(download);
    }

    private static async Task<TaxDocumentUploadResult> UploadAsync(TaxDocumentService service, Guid declarationId, string fileName)
    {
        await using var content = new MemoryStream([1, 2]);
        var result = await service.UploadAsync(new TaxDocumentUploadCommand(
            declarationId,
            DocumentType.SubmissionProof,
            fileName,
            "application/pdf",
            content.Length,
            content));

        Assert.True(result.Success, result.ErrorMessage);
        return result;
    }

    private static async Task<(Guid DeclarationId, Guid UserId)> CreateDeclarationAsync(EthanTcmDbContext dbContext)
    {
        var legalEntity = new LegalEntity("ETHAN", "ETHAN TCM", "CD", null);
        var department = new Department("FINANCE", "Finance");
        var category = new TaxCategory("VAT", "VAT");
        var frequency = new TaxFrequency("MONTHLY", "Monthly", 12);
        var user = new User("preparer", "Preparer", "preparer@local");
        var obligation = new TaxObligation(
            legalEntity.Id,
            department.Id,
            category.Id,
            frequency.Id,
            user.Id,
            "VAT Return",
            RiskLevel.Medium,
            requiresPayment: false,
            DateTimeOffset.UtcNow);
        var period = new TaxPeriod(2026, 1, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "2026-01");
        var declaration = new TaxDeclaration(
            obligation.Id,
            period.Id,
            new DateOnly(2026, 2, 15),
            "2026-01",
            paymentRequired: false,
            user.Id);

        dbContext.AddRange(legalEntity, department, category, frequency, user, obligation, period, declaration);
        await dbContext.SaveChangesAsync();
        return (declaration.Id, user.Id);
    }

    private static EthanTcmDbContext CreateDbContext()
    {
        return new EthanTcmDbContext(new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static TaxDocumentService CreateService(EthanTcmDbContext dbContext, Guid userId)
    {
        return new TaxDocumentService(
            dbContext,
            new TestCurrentUserService(userId),
            new TestAuditService(dbContext, userId),
            Options.Create(new TaxDocumentStorageOptions
            {
                RootPath = Path.Combine(Environment.CurrentDirectory, "TestDocuments", Guid.NewGuid().ToString("N")),
                MaxFileSizeBytes = 1024,
                AllowedExtensions = ["pdf", "xlsx", "xls", "docx", "jpg", "png"],
                AllowedContentTypes =
                [
                    "application/pdf",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/vnd.ms-excel",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "image/jpeg",
                    "image/png"
                ]
            }));
    }

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public string? Login => "test.user";
        public string? DisplayName => "Test User";
        public string? Email => "test.user@local";
        public Guid? DepartmentId => null;
        public bool IsAuthenticated => true;
        public bool IsActive => true;
        public IReadOnlyCollection<string> Roles => [ApplicationRoles.Preparer];

        public bool IsInRole(string role)
        {
            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}
