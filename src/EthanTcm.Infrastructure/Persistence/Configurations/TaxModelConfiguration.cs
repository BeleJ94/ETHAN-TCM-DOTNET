using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EthanTcm.Infrastructure.Persistence.Configurations;

public sealed class TaxTypeConfiguration : IEntityTypeConfiguration<TaxType>
{
    public void Configure(EntityTypeBuilder<TaxType> builder)
    {
        builder.ToTable("TaxTypes");
        builder.HasKey(type => type.Id);
        builder.Property(type => type.Code).HasMaxLength(50).IsRequired();
        builder.Property(type => type.Name).HasMaxLength(256).IsRequired();
        builder.Property(type => type.Description).HasMaxLength(1000);
        builder.HasIndex(type => type.Code).IsUnique();
        builder.HasIndex(type => type.IsActive);
    }
}

public sealed class LegalEntityConfiguration : IEntityTypeConfiguration<LegalEntity>
{
    public void Configure(EntityTypeBuilder<LegalEntity> builder)
    {
        builder.ToTable("LegalEntities");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Code).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Country).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.TaxIdentificationNumber).HasMaxLength(100);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.IsActive);
    }
}

public sealed class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
{
    public void Configure(EntityTypeBuilder<TaxCategory> builder)
    {
        builder.ToTable("TaxCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Code).HasMaxLength(50).IsRequired();
        builder.Property(category => category.Name).HasMaxLength(256).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.HasIndex(category => category.Code).IsUnique();
        builder.HasIndex(category => category.IsActive);
    }
}

public sealed class TaxFrequencyConfiguration : IEntityTypeConfiguration<TaxFrequency>
{
    public void Configure(EntityTypeBuilder<TaxFrequency> builder)
    {
        builder.ToTable("TaxFrequencies");
        builder.HasKey(frequency => frequency.Id);
        builder.Property(frequency => frequency.Code).HasMaxLength(50).IsRequired();
        builder.Property(frequency => frequency.Name).HasMaxLength(256).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxFrequencies_OccurrencesPerYear_Positive", "[OccurrencesPerYear] > 0"));
        builder.HasIndex(frequency => frequency.Code).IsUnique();
        builder.HasIndex(frequency => frequency.IsActive);
    }
}

public sealed class TaxObligationConfiguration : IEntityTypeConfiguration<TaxObligation>
{
    public void Configure(EntityTypeBuilder<TaxObligation> builder)
    {
        builder.ToTable("TaxObligations");
        builder.HasKey(obligation => obligation.Id);
        builder.Property(obligation => obligation.Name).HasMaxLength(256).IsRequired();
        builder.Property(obligation => obligation.CanonicalCode).HasMaxLength(100);
        builder.Property(obligation => obligation.Description).HasMaxLength(2000);
        builder.Property(obligation => obligation.ComplianceNotes).HasMaxLength(4000);
        builder.Property(obligation => obligation.LegalDeadline).HasMaxLength(500);
        builder.Property(obligation => obligation.ReviewReason).HasMaxLength(1000);
        builder.Property(obligation => obligation.RiskLevel).HasConversion<string>().HasMaxLength(50);
        builder.Property(obligation => obligation.CatalogValidationStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(TaxCatalogValidationStatus.Validated)
            .HasSentinel((TaxCatalogValidationStatus)(-1));
        builder.Property(obligation => obligation.RequiresPayment).HasDefaultValue(true);
        builder.Property(obligation => obligation.RequiresPaymentProof).HasDefaultValue(true);
        builder.HasOne(obligation => obligation.LegalEntity)
            .WithMany()
            .HasForeignKey(obligation => obligation.LegalEntityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(obligation => obligation.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxCategory>()
            .WithMany()
            .HasForeignKey(obligation => obligation.TaxCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxFrequency>()
            .WithMany()
            .HasForeignKey(obligation => obligation.TaxFrequencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(obligation => obligation.Responsibles)
            .WithOne()
            .HasForeignKey(responsible => responsible.TaxObligationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(obligation => obligation.ScheduleRules)
            .WithOne()
            .HasForeignKey(rule => rule.TaxObligationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(obligation => new { obligation.LegalEntityId, obligation.DepartmentId, obligation.TaxCategoryId, obligation.IsActive });
        builder.HasIndex(obligation => obligation.TaxFrequencyId);
        builder.HasIndex(obligation => obligation.IsActive);
        builder.HasIndex(obligation => obligation.RequiresReview);
        builder.HasIndex(obligation => obligation.SourceNumber);
        builder.HasIndex(obligation => obligation.CanonicalCode).IsUnique().HasFilter("[CanonicalCode] IS NOT NULL");
        builder.HasIndex(obligation => new { obligation.DepartmentId, obligation.Name, obligation.TaxCategoryId }).IsUnique();
        builder.HasIndex(obligation => new { obligation.LegalEntityId, obligation.Name }).IsUnique();
        builder.Metadata.FindNavigation(nameof(TaxObligation.Responsibles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(TaxObligation.ScheduleRules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class TaxPeriodConfiguration : IEntityTypeConfiguration<TaxPeriod>
{
    public void Configure(EntityTypeBuilder<TaxPeriod> builder)
    {
        builder.ToTable("TaxPeriods");
        builder.HasKey(period => period.Id);
        builder.Property(period => period.PeriodType).HasMaxLength(50).IsRequired();
        builder.Property(period => period.Label).HasMaxLength(100).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxPeriods_DateRange", "[EndDate] >= [StartDate]"));
        builder.HasIndex(period => period.StartDate);
        builder.HasIndex(period => period.EndDate);
        builder.HasIndex(period => new { period.Year, period.PeriodType, period.Sequence }).IsUnique();
    }
}

public sealed class TaxDeclarationConfiguration : IEntityTypeConfiguration<TaxDeclaration>
{
    public void Configure(EntityTypeBuilder<TaxDeclaration> builder)
    {
        builder.ToTable("TaxDeclarations");
        builder.HasKey(declaration => declaration.Id);
        builder.Property(declaration => declaration.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(declaration => declaration.PenaltyRiskLevel).HasConversion<string>().HasMaxLength(50);
        builder.Property(declaration => declaration.PeriodLabel).HasMaxLength(100).IsRequired();
        builder.Property(declaration => declaration.SubmissionReference).HasMaxLength(256);
        builder.Property(declaration => declaration.PaymentRequired).HasDefaultValue(true);
        builder.Property(declaration => declaration.ApprovalCycleNumber).HasDefaultValue(0);
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxDeclarations_DelayDays_NonNegative", "[DelayDays] >= 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_TaxDeclarations_ApprovalCycleNumber_NonNegative", "[ApprovalCycleNumber] >= 0"));
        builder.HasOne(declaration => declaration.TaxObligation)
            .WithMany()
            .HasForeignKey(declaration => declaration.TaxObligationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(declaration => declaration.TaxObligationVersion)
            .WithMany()
            .HasForeignKey(declaration => declaration.TaxObligationVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(declaration => declaration.TaxPeriod)
            .WithMany()
            .HasForeignKey(declaration => declaration.TaxPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(declaration => declaration.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(declaration => declaration.ValidatorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(declaration => declaration.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(declaration => declaration.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(declaration => declaration.Approvals)
            .WithOne()
            .HasForeignKey(approval => approval.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(declaration => declaration.Payments)
            .WithOne()
            .HasForeignKey(payment => payment.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(declaration => declaration.Documents)
            .WithOne()
            .HasForeignKey(document => document.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(declaration => declaration.DueDate);
        builder.HasIndex(declaration => declaration.ReminderDate);
        builder.HasIndex(declaration => declaration.Status);
        builder.HasIndex(declaration => declaration.ApprovalCycleNumber);
        builder.HasIndex(declaration => declaration.AssignedToUserId);
        builder.HasIndex(declaration => declaration.TaxObligationVersionId);
        builder.HasIndex(declaration => declaration.ValidatorUserId);
        builder.HasIndex(declaration => declaration.SubmittedByUserId);
        builder.HasIndex(declaration => declaration.ClosedByUserId);
        builder.HasIndex(declaration => declaration.SubmittedAt);
        builder.HasIndex(declaration => declaration.ClosedAt);
        builder.HasIndex(declaration => new { declaration.Status, declaration.DueDate });
        builder.HasIndex(declaration => new { declaration.AssignedToUserId, declaration.Status, declaration.DueDate });
        builder.HasIndex(declaration => new { declaration.TaxObligationId, declaration.TaxPeriodId }).IsUnique();
        builder.Metadata.FindNavigation(nameof(TaxDeclaration.Approvals))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(TaxDeclaration.Payments))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(TaxDeclaration.Documents))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
