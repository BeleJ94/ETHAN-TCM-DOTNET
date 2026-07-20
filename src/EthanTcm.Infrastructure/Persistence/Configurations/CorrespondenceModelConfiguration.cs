using EthanTcm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EthanTcm.Infrastructure.Persistence.Configurations;

public sealed class CorrespondenceModelConfiguration : IEntityTypeConfiguration<Correspondence>
{
    public void Configure(EntityTypeBuilder<Correspondence> b)
    {
        b.ToTable("Correspondences"); b.HasKey(x => x.Id); b.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        b.Property(x => x.ReferenceNumber).HasMaxLength(40); b.HasIndex(x => x.ReferenceNumber).IsUnique().HasFilter("[ReferenceNumber] IS NOT NULL");
        b.Property(x => x.BusinessReference).HasMaxLength(100); b.HasIndex(x => x.BusinessReference);
        b.Property(x => x.Summary).HasMaxLength(2000); b.Property(x => x.SenderName).HasMaxLength(200); b.Property(x => x.SenderOrganization).HasMaxLength(200);
        b.Property(x => x.RecipientName).HasMaxLength(200); b.Property(x => x.RecipientOrganization).HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => new { x.Status, x.DueDate }); b.HasIndex(x => new { x.AssignedToUserId, x.Status });
        b.HasMany(x => x.History).WithOne().HasForeignKey(x => x.CorrespondenceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(x => x.CorrespondenceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.FollowUpActions).WithOne().HasForeignKey(x => x.CorrespondenceId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CorrespondenceOrganization>().WithMany().HasForeignKey(x => x.CorrespondentOrganizationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Department>().WithMany().HasForeignKey(x => x.InternalDepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class CorrespondenceFollowUpActionConfiguration : IEntityTypeConfiguration<CorrespondenceFollowUpAction>
{
    public void Configure(EntityTypeBuilder<CorrespondenceFollowUpAction> b)
    {
        b.ToTable("CorrespondenceFollowUpActions"); b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(250).IsRequired(); b.Property(x => x.Description).HasMaxLength(1500);
        b.Property(x => x.WaitingFor).HasMaxLength(500); b.Property(x => x.CompletionResult).HasMaxLength(1500);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AssignedToUserId, x.Status, x.DueDate }); b.HasIndex(x => new { x.CorrespondenceId, x.Status });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.EscalationUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class CorrespondenceActionNotificationConfiguration : IEntityTypeConfiguration<CorrespondenceActionNotification>
{
    public void Configure(EntityTypeBuilder<CorrespondenceActionNotification> b)
    {
        b.ToTable("CorrespondenceActionNotifications"); b.HasKey(x => x.Id);
        b.Property(x => x.Trigger).HasMaxLength(30).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(300).IsRequired(); b.Property(x => x.Message).HasMaxLength(3000).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(2000); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.CorrespondenceActionId, x.RecipientUserId, x.Trigger }).IsUnique();
        b.HasIndex(x => new { x.Status, x.ProcessingDate });
        b.HasOne<CorrespondenceFollowUpAction>().WithMany().HasForeignKey(x => x.CorrespondenceActionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class CorrespondenceOrganizationConfiguration : IEntityTypeConfiguration<CorrespondenceOrganization>
{
    public void Configure(EntityTypeBuilder<CorrespondenceOrganization> b) { b.ToTable("CorrespondenceOrganizations"); b.HasKey(x => x.Id); b.Property(x => x.Code).HasMaxLength(40).IsRequired(); b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.ContactEmail).HasMaxLength(256); b.Property(x => x.Address).HasMaxLength(500); b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => new { x.IsActive, x.Name }); }
}
public sealed class CorrespondenceHistoryConfiguration : IEntityTypeConfiguration<CorrespondenceHistory>
{
    public void Configure(EntityTypeBuilder<CorrespondenceHistory> b) { b.ToTable("CorrespondenceHistory"); b.HasKey(x => x.Id); b.Property(x => x.Action).HasMaxLength(80); b.Property(x => x.Comment).HasMaxLength(1000); b.Property(x => x.RowVersion).IsRowVersion(); }
}
public sealed class CorrespondenceDocumentConfiguration : IEntityTypeConfiguration<CorrespondenceDocument>
{
    public void Configure(EntityTypeBuilder<CorrespondenceDocument> b) { b.ToTable("CorrespondenceDocuments"); b.HasKey(x => x.Id); b.Property(x => x.FileName).HasMaxLength(260); b.Property(x => x.FilePath).HasMaxLength(1000); b.Property(x => x.ContentType).HasMaxLength(150); b.Property(x => x.RowVersion).IsRowVersion(); }
}
public sealed class CorrespondenceSequenceConfiguration : IEntityTypeConfiguration<CorrespondenceSequence>
{
    public void Configure(EntityTypeBuilder<CorrespondenceSequence> b) { b.ToTable("CorrespondenceSequences"); b.HasKey(x => new { x.Year, x.Direction }); }
}
