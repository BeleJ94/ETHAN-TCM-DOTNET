using EthanTcm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EthanTcm.Infrastructure.Persistence.Configurations;

public sealed class TaxObligationResponsibleConfiguration : IEntityTypeConfiguration<TaxObligationResponsible>
{
    public void Configure(EntityTypeBuilder<TaxObligationResponsible> builder)
    {
        builder.ToTable("TaxObligationResponsibles");
        builder.HasKey(responsible => responsible.Id);
        builder.Property(responsible => responsible.Type).HasConversion<string>().HasMaxLength(50);
        builder.HasOne<TaxObligation>()
            .WithMany(obligation => obligation.Responsibles)
            .HasForeignKey(responsible => responsible.TaxObligationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(responsible => responsible.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(responsible => new { responsible.TaxObligationId, responsible.Type });
        builder.HasIndex(responsible => new { responsible.TaxObligationId, responsible.UserId, responsible.Type }).IsUnique();
    }
}

public sealed class TaxScheduleRuleConfiguration : IEntityTypeConfiguration<TaxScheduleRule>
{
    public void Configure(EntityTypeBuilder<TaxScheduleRule> builder)
    {
        builder.ToTable("TaxScheduleRules");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.RawReminderText).HasMaxLength(1000);
        builder.Property(rule => rule.ReminderDays).HasMaxLength(200);
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxScheduleRules_DueDay_Range", "[DueDay] BETWEEN 1 AND 31"));
        builder.HasOne<TaxObligation>()
            .WithMany(obligation => obligation.ScheduleRules)
            .HasForeignKey(rule => rule.TaxObligationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rule => rule.TaxObligationId);
        builder.HasIndex(rule => rule.IsActive);
        builder.HasIndex(rule => new { rule.TaxObligationId, rule.DueMonth, rule.DueDay }).IsUnique();
    }
}

public sealed class TaxDeclarationApprovalConfiguration : IEntityTypeConfiguration<TaxDeclarationApproval>
{
    public void Configure(EntityTypeBuilder<TaxDeclarationApproval> builder)
    {
        builder.ToTable("TaxDeclarationApprovals");
        builder.HasKey(approval => approval.Id);
        builder.Property(approval => approval.ApprovalCycleNumber).HasDefaultValue(1);
        builder.Property(approval => approval.ApprovalLevel).HasDefaultValue(1);
        builder.Property(approval => approval.Decision).HasConversion<string>().HasMaxLength(50);
        builder.Property(approval => approval.Comment).HasMaxLength(2000);
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxDeclarationApprovals_ApprovalCycleNumber_Positive", "[ApprovalCycleNumber] > 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxDeclarationApprovals_ApprovalLevel_Range", "[ApprovalLevel] BETWEEN 1 AND 3"));
        builder.HasOne<TaxDeclaration>()
            .WithMany(declaration => declaration.Approvals)
            .HasForeignKey(approval => approval.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(approval => approval.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(approval => approval.TaxDeclarationId);
        builder.HasIndex(approval => new { approval.TaxDeclarationId, approval.ApprovalCycleNumber, approval.ApprovalLevel });
        builder.HasIndex(approval => approval.ApproverUserId);
    }
}

public sealed class TaxPaymentConfiguration : IEntityTypeConfiguration<TaxPayment>
{
    public void Configure(EntityTypeBuilder<TaxPayment> builder)
    {
        builder.ToTable("TaxPayments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.PaymentReference).HasMaxLength(256).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxPayments_Amount_Positive", "[Amount] > 0"));
        builder.HasOne<TaxDeclaration>()
            .WithMany(declaration => declaration.Payments)
            .HasForeignKey(payment => payment.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(payment => payment.PaidByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => payment.TaxDeclarationId);
        builder.HasIndex(payment => payment.PaidByUserId);
        builder.HasIndex(payment => payment.PaymentReference);
        builder.HasIndex(payment => payment.PaidAt);
    }
}

public sealed class TaxDocumentConfiguration : IEntityTypeConfiguration<TaxDocument>
{
    public void Configure(EntityTypeBuilder<TaxDocument> builder)
    {
        builder.ToTable("TaxDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.DocumentType).HasConversion<string>().HasMaxLength(100);
        builder.Property(document => document.FileName).HasMaxLength(260).IsRequired();
        builder.Property(document => document.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxDocuments_FileSize_NonNegative", "[FileSizeBytes] >= 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxDocuments_Version_Positive", "[Version] > 0"));
        builder.HasOne<TaxDeclaration>()
            .WithMany(declaration => declaration.Documents)
            .HasForeignKey(document => document.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => document.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(document => document.TaxDeclarationId);
        builder.HasIndex(document => document.UploadedByUserId);
        builder.HasIndex(document => document.UploadedAt);
        builder.HasIndex(document => document.IsDeleted);
        builder.HasIndex(document => new { document.TaxDeclarationId, document.DocumentType });
        builder.HasIndex(document => new
        {
            document.TaxDeclarationId,
            document.DocumentType,
            document.IsDeleted,
            document.UploadedAt
        });
        builder.HasIndex(document => new { document.TaxDeclarationId, document.DocumentType, document.Version }).IsUnique();
    }
}

public sealed class DeclarationDocumentConfiguration : IEntityTypeConfiguration<DeclarationDocument>
{
    public void Configure(EntityTypeBuilder<DeclarationDocument> builder)
    {
        builder.ToTable("DeclarationDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.FileName).HasMaxLength(260).IsRequired();
        builder.Property(document => document.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.HasOne<TaxDeclaration>()
            .WithMany()
            .HasForeignKey(document => document.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => document.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(document => document.TaxDeclarationId);
        builder.HasIndex(document => document.UploadedByUserId);
        builder.HasIndex(document => document.UploadedAt);
        builder.HasIndex(document => new { document.TaxDeclarationId, document.DocumentType });
    }
}
