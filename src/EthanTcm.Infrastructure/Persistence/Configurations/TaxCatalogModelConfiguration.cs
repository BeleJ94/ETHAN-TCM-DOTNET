using EthanTcm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EthanTcm.Infrastructure.Persistence.Configurations;

public abstract class TaxObligationChildConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    public abstract void Configure(EntityTypeBuilder<TEntity> builder);
    protected static void RestrictToObligation(EntityTypeBuilder<TEntity> builder, string foreignKey)
    {
        builder.HasOne<TaxObligation>().WithMany().HasForeignKey(foreignKey).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaxObligationAliasConfiguration : TaxObligationChildConfiguration<TaxObligationAlias>
{
    public override void Configure(EntityTypeBuilder<TaxObligationAlias> b)
    {
        b.ToTable("TaxObligationAliases"); b.HasKey(x => x.Id);
        b.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        b.Property(x => x.SourceReference).HasMaxLength(200);
        b.Property(x => x.Alias).HasMaxLength(500).IsRequired();
        b.Property(x => x.SourceRow).HasMaxLength(50).IsRequired();
        RestrictToObligation(b, nameof(TaxObligationAlias.TaxObligationId));
        b.HasIndex(x => new { x.SourceName, x.SourceRow, x.Alias }).IsUnique();
        b.HasIndex(x => x.TaxObligationId);
    }
}

public sealed class TaxSourceReferenceConfiguration : TaxObligationChildConfiguration<TaxSourceReference>
{
    public override void Configure(EntityTypeBuilder<TaxSourceReference> b)
    {
        b.ToTable("TaxSourceReferences"); b.HasKey(x => x.Id);
        b.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        b.Property(x => x.SourceRow).HasMaxLength(50).IsRequired();
        b.Property(x => x.ExternalNumber).HasMaxLength(50);
        b.Property(x => x.DataHash).HasMaxLength(64).IsRequired();
        RestrictToObligation(b, nameof(TaxSourceReference.TaxObligationId));
        b.HasIndex(x => new { x.SourceName, x.SourceRow, x.TaxObligationId }).IsUnique();
    }
}

public sealed class TaxObligationVersionConfiguration : TaxObligationChildConfiguration<TaxObligationVersion>
{
    public override void Configure(EntityTypeBuilder<TaxObligationVersion> b)
    {
        b.ToTable("TaxObligationVersions"); b.HasKey(x => x.Id);
        b.Property(x => x.Frequency).HasMaxLength(200);
        b.Property(x => x.FilingDeadlineRule).HasMaxLength(1000);
        b.Property(x => x.PaymentDeadlineRule).HasMaxLength(1000);
        b.Property(x => x.RawDeadlineText).HasMaxLength(2000);
        b.Property(x => x.BusinessCycle).HasMaxLength(500);
        b.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.ChangeReason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.DataHash).HasMaxLength(64).IsRequired();
        RestrictToObligation(b, nameof(TaxObligationVersion.TaxObligationId));
        b.HasOne<TaxAuthority>().WithMany().HasForeignKey(x => x.TaxAuthorityId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TaxObligationId, x.VersionNumber }).IsUnique();
        b.HasIndex(x => new { x.TaxObligationId, x.EffectiveTo });
    }
}

public sealed class TaxAuthorityConfiguration : IEntityTypeConfiguration<TaxAuthority>
{
    public void Configure(EntityTypeBuilder<TaxAuthority> b)
    {
        b.ToTable("TaxAuthorities"); b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public abstract class TaxVersionTextConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : class
{
    public abstract void Configure(EntityTypeBuilder<TEntity> builder);

    protected static void ConfigureBase(EntityTypeBuilder<TEntity> b, string table, string textProperty)
    {
        b.ToTable(table); b.HasKey("Id");
        b.Property<string>(textProperty).HasMaxLength(4000).IsRequired();
        b.Property<string>("SourceName").HasMaxLength(100).IsRequired();
        b.Property<string>("SourceRow").HasMaxLength(50).IsRequired();
        b.HasOne<TaxObligationVersion>().WithMany().HasForeignKey("TaxObligationVersionId").OnDelete(DeleteBehavior.Cascade);
        b.HasIndex("TaxObligationVersionId", "SourceName", "SourceRow").IsUnique();
    }
}

public sealed class TaxRateRuleConfiguration : TaxVersionTextConfiguration<TaxRateRule>
{ public override void Configure(EntityTypeBuilder<TaxRateRule> b) => ConfigureBase(b, "TaxRateRules", nameof(TaxRateRule.RuleText)); }
public sealed class TaxLegalReferenceConfiguration : TaxVersionTextConfiguration<TaxLegalReference>
{ public override void Configure(EntityTypeBuilder<TaxLegalReference> b) => ConfigureBase(b, "TaxLegalReferences", nameof(TaxLegalReference.ReferenceText)); }
public sealed class TaxPenaltyRuleConfiguration : TaxVersionTextConfiguration<TaxPenaltyRule>
{ public override void Configure(EntityTypeBuilder<TaxPenaltyRule> b) => ConfigureBase(b, "TaxPenaltyRules", nameof(TaxPenaltyRule.RuleText)); }
public sealed class TaxProcessTemplateConfiguration : TaxVersionTextConfiguration<TaxProcessTemplate>
{ public override void Configure(EntityTypeBuilder<TaxProcessTemplate> b) => ConfigureBase(b, "TaxProcessTemplates", nameof(TaxProcessTemplate.ProcessText)); }

public sealed class TaxRequiredDocumentConfiguration : TaxObligationChildConfiguration<TaxRequiredDocument>
{
    public override void Configure(EntityTypeBuilder<TaxRequiredDocument> b)
    {
        b.ToTable("TaxRequiredDocuments"); b.HasKey(x => x.Id);
        b.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(100);
        b.Property(x => x.Condition).HasMaxLength(500);
        RestrictToObligation(b, nameof(TaxRequiredDocument.TaxObligationId));
        b.HasIndex(x => new { x.TaxObligationId, x.DocumentType }).IsUnique();
    }
}

public sealed class TaxCatalogConflictConfiguration : IEntityTypeConfiguration<TaxCatalogConflict>
{
    public void Configure(EntityTypeBuilder<TaxCatalogConflict> b)
    {
        b.ToTable("TaxCatalogConflicts"); b.HasKey(x => x.Id);
        b.Property(x => x.CanonicalCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
        b.Property(x => x.ExistingValue).HasMaxLength(4000);
        b.Property(x => x.IncomingValue).HasMaxLength(4000);
        b.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        b.Property(x => x.SourceRow).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.Resolution).HasMaxLength(4000);
        b.HasIndex(x => new { x.CanonicalCode, x.FieldName, x.SourceName, x.SourceRow }).IsUnique();
        b.HasIndex(x => x.Status);
    }
}

public sealed class TaxAllocationRuleConfiguration : TaxObligationChildConfiguration<TaxAllocationRule>
{
    public override void Configure(EntityTypeBuilder<TaxAllocationRule> b)
    {
        b.ToTable("TaxAllocationRules"); b.HasKey(x => x.Id);
        b.Property(x => x.Percentage).HasPrecision(5, 2);
        b.Property(x => x.Beneficiary).HasMaxLength(300).IsRequired();
        b.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
        b.Property(x => x.SourceRow).HasMaxLength(50).IsRequired();
        RestrictToObligation(b, nameof(TaxAllocationRule.TaxObligationId));
        b.HasIndex(x => new { x.TaxObligationId, x.SourceName, x.SourceRow }).IsUnique();
    }
}
