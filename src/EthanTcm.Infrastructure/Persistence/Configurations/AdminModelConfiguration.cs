using EthanTcm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EthanTcm.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Code).HasMaxLength(50).IsRequired();
        builder.Property(template => template.Name).HasMaxLength(256).IsRequired();
        builder.Property(template => template.Channel).HasConversion<string>().HasMaxLength(50);
        builder.Property(template => template.Subject).HasMaxLength(500).IsRequired();
        builder.HasIndex(template => template.Code).IsUnique();
    }
}

public sealed class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("NotificationRules");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Code).HasMaxLength(50).IsRequired();
        builder.Property(rule => rule.Trigger).HasConversion<string>().HasMaxLength(50);
        builder.HasOne<NotificationTemplate>()
            .WithMany()
            .HasForeignKey(rule => rule.NotificationTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rule => rule.Code).IsUnique();
        builder.HasIndex(rule => new { rule.Trigger, rule.IsActive });
    }
}

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Subject).HasMaxLength(500).IsRequired();
        builder.Property(log => log.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(log => log.ErrorMessage).HasMaxLength(2000);
        builder.HasOne<NotificationRule>()
            .WithMany()
            .HasForeignKey(log => log.NotificationRuleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxDeclaration>()
            .WithMany()
            .HasForeignKey(log => log.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(log => log.NotificationRuleId);
        builder.HasIndex(log => log.TaxDeclarationId);
        builder.HasIndex(log => log.RecipientUserId);
        builder.HasIndex(log => new { log.Status, log.SentAt });
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.UserDisplayName).HasMaxLength(256);
        builder.Property(log => log.EntityName).HasMaxLength(256).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(log => log.Action).HasMaxLength(128).IsRequired();
        builder.Property(log => log.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(log => log.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(log => log.CorrelationId).HasMaxLength(128);
        builder.Property(log => log.Module).HasMaxLength(128);
        builder.Property(log => log.Source).HasMaxLength(128);
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.Property(log => log.UserAgent).HasMaxLength(512);
        builder.Property(log => log.RequestPath).HasMaxLength(2048);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_AuditLogs_OldValue_Json", "[OldValue] IS NULL OR ISJSON([OldValue]) = 1");
            table.HasCheckConstraint("CK_AuditLogs_NewValue_Json", "[NewValue] IS NULL OR ISJSON([NewValue]) = 1");
        });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.OccurredAt);
        builder.HasIndex(log => log.CorrelationId);
        builder.HasIndex(log => log.Module);
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => new { log.EntityName, log.EntityId });
        builder.HasIndex(log => new { log.EntityName, log.Action, log.OccurredAt });
        builder.HasIndex(log => new { log.UserId, log.OccurredAt });
    }
}

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");
        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.FileName).HasMaxLength(260).IsRequired();
        builder.Property(batch => batch.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(batch => batch.ErrorReportPath).HasMaxLength(1000);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ImportBatches_TotalRows_NonNegative", "[TotalRows] >= 0");
            table.HasCheckConstraint("CK_ImportBatches_ValidRows_NonNegative", "[ValidRows] >= 0");
            table.HasCheckConstraint("CK_ImportBatches_InvalidRows_NonNegative", "[InvalidRows] >= 0");
        });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(batch => batch.ImportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(batch => batch.Errors)
            .WithOne()
            .HasForeignKey(error => error.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(batch => batch.ImportedByUserId);
        builder.HasIndex(batch => new { batch.Status, batch.ImportedAt });
        builder.Metadata.FindNavigation(nameof(ImportBatch.Errors))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ImportErrorConfiguration : IEntityTypeConfiguration<ImportError>
{
    public void Configure(EntityTypeBuilder<ImportError> builder)
    {
        builder.ToTable("ImportErrors");
        builder.HasKey(error => error.Id);
        builder.Property(error => error.ColumnName).HasMaxLength(128).IsRequired();
        builder.Property(error => error.Message).HasMaxLength(2000).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_ImportErrors_RowNumber_Positive", "[RowNumber] > 0"));
        builder.HasOne<ImportBatch>()
            .WithMany(batch => batch.Errors)
            .HasForeignKey(error => error.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(error => error.ImportBatchId);
        builder.HasIndex(error => new { error.ImportBatchId, error.RowNumber });
    }
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(128).IsRequired();
        builder.Property(setting => setting.Value).HasMaxLength(4000).IsRequired();
        builder.Property(setting => setting.ValueType).HasConversion<string>().HasMaxLength(50);
        builder.Property(setting => setting.Description).HasMaxLength(1000);
        builder.HasIndex(setting => setting.Key).IsUnique();
    }
}
