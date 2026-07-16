using EthanTcm.Domain.Common;
using EthanTcm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Persistence;

public sealed class EthanTcmDbContext(DbContextOptions<EthanTcmDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<TaxType> TaxTypes => Set<TaxType>();
    public DbSet<TaxCategory> TaxCategories => Set<TaxCategory>();
    public DbSet<TaxFrequency> TaxFrequencies => Set<TaxFrequency>();
    public DbSet<TaxScheduleRule> TaxScheduleRules => Set<TaxScheduleRule>();
    public DbSet<TaxObligation> TaxObligations => Set<TaxObligation>();
    public DbSet<TaxObligationResponsible> TaxObligationResponsibles => Set<TaxObligationResponsible>();
    public DbSet<TaxObligationAlias> TaxObligationAliases => Set<TaxObligationAlias>();
    public DbSet<TaxSourceReference> TaxSourceReferences => Set<TaxSourceReference>();
    public DbSet<TaxObligationVersion> TaxObligationVersions => Set<TaxObligationVersion>();
    public DbSet<TaxAuthority> TaxAuthorities => Set<TaxAuthority>();
    public DbSet<TaxRateRule> TaxRateRules => Set<TaxRateRule>();
    public DbSet<TaxLegalReference> TaxLegalReferences => Set<TaxLegalReference>();
    public DbSet<TaxPenaltyRule> TaxPenaltyRules => Set<TaxPenaltyRule>();
    public DbSet<TaxProcessTemplate> TaxProcessTemplates => Set<TaxProcessTemplate>();
    public DbSet<TaxRequiredDocument> TaxRequiredDocuments => Set<TaxRequiredDocument>();
    public DbSet<TaxCatalogConflict> TaxCatalogConflicts => Set<TaxCatalogConflict>();
    public DbSet<TaxAllocationRule> TaxAllocationRules => Set<TaxAllocationRule>();
    public DbSet<TaxPeriod> TaxPeriods => Set<TaxPeriod>();
    public DbSet<TaxDeclaration> TaxDeclarations => Set<TaxDeclaration>();
    public DbSet<TaxDeclarationApproval> TaxDeclarationApprovals => Set<TaxDeclarationApproval>();
    public DbSet<TaxPayment> TaxPayments => Set<TaxPayment>();
    public DbSet<TaxDocument> TaxDocuments => Set<TaxDocument>();
    public DbSet<DeclarationDocument> DeclarationDocuments => Set<DeclarationDocument>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportError> ImportErrors => Set<ImportError>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Correspondence> Correspondences => Set<Correspondence>();
    public DbSet<CorrespondenceHistory> CorrespondenceHistory => Set<CorrespondenceHistory>();
    public DbSet<CorrespondenceDocument> CorrespondenceDocuments => Set<CorrespondenceDocument>();
    public DbSet<CorrespondenceSequence> CorrespondenceSequences => Set<CorrespondenceSequence>();
    public DbSet<CorrespondenceOrganization> CorrespondenceOrganizations => Set<CorrespondenceOrganization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EthanTcmDbContext).Assembly);
        ConfigureAuditableEntities(modelBuilder);
    }

    private static void ConfigureAuditableEntities(ModelBuilder modelBuilder)
    {
        var auditableEntityTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(entityType => typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType));

        foreach (var entityType in auditableEntityTypes)
        {
            var entity = modelBuilder.Entity(entityType.ClrType);

            entity.Property(nameof(AuditableEntity.CreatedAt))
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
            entity.Property(nameof(AuditableEntity.CreatedByUserId));
            entity.Property(nameof(AuditableEntity.UpdatedAt));
            entity.Property(nameof(AuditableEntity.UpdatedByUserId));
            entity.Property(nameof(AuditableEntity.RowVersion))
                .IsRowVersion();
            entity.HasIndex(nameof(AuditableEntity.CreatedAt));
            entity.HasIndex(nameof(AuditableEntity.CreatedByUserId), nameof(AuditableEntity.CreatedAt));
            entity.HasIndex(nameof(AuditableEntity.UpdatedByUserId), nameof(AuditableEntity.UpdatedAt));
        }
    }
}
