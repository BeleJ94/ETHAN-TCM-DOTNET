using EthanTcm.Domain.Entities;
using EthanTcm.Application.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EthanTcm.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Login).HasMaxLength(128).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Department).HasMaxLength(256);
        builder.HasIndex(user => user.Login).IsUnique();
        builder.HasIndex(user => user.Email);
        builder.HasIndex(user => user.IsActive);
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Login).HasMaxLength(128).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.ExternalId).HasMaxLength(256);
        builder.Property(user => user.PreferencesJson).HasColumnType("nvarchar(max)");
        builder.Property(user => user.PreferredCulture).HasMaxLength(5).HasDefaultValue(User.DefaultCulture).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_Users_PreferredCulture", "[PreferredCulture] IN ('en', 'fr')"));
        builder.ToTable(table => table.HasCheckConstraint("CK_Users_PreferencesJson_Json", "[PreferencesJson] IS NULL OR ISJSON([PreferencesJson]) = 1"));
        builder.HasIndex(user => user.Login).IsUnique();
        builder.HasIndex(user => user.Email);
        builder.HasIndex(user => user.ExternalId);
        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.DepartmentId);
        builder.HasIndex(user => user.DelegatedToUserId);
        builder.HasIndex(user => user.LastSyncedAt);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(user => user.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(user => user.DelegatedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(user => user.Roles)
            .WithOne()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(User.Roles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Code).HasMaxLength(50).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(128).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(1000);
        builder.HasIndex(role => role.Code).IsUnique();
        builder.HasIndex(role => role.IsActive);
        builder.HasData(
            CreateRole("6b46396d-4056-45fd-aebb-35a5b99256bd", ApplicationRoles.Administrator, "Administrator", "Full system administration."),
            CreateRole("b9d91a92-7076-4e50-ac7e-3198132ed64f", ApplicationRoles.TaxManager, "Tax Manager", "Tax obligations and declaration oversight."),
            CreateRole("bc7790b2-5d9c-4579-8b39-3a0dd2eac4c2", ApplicationRoles.Preparer, "Preparer", "Declaration preparation."),
            CreateRole("67bb7368-580c-41e0-a8a9-24c95f4d28f0", ApplicationRoles.Approver, "Approver", "Declaration approval."),
            CreateRole("e5853748-4fc9-4680-a2c0-ac643d5c9407", ApplicationRoles.FinanceManager, "Finance Manager", "Payment and finance validation."),
            CreateRole("c3a3aa9e-25ab-4023-80e8-6398aaed017d", ApplicationRoles.Auditor, "Auditor", "Audit and compliance review."),
            CreateRole("d9af8939-cd08-4e8d-a153-3269833a9c92", ApplicationRoles.ReadOnly, "Read Only", "Read-only access."));
    }

    private static object CreateRole(string id, string code, string name, string description)
    {
        return new
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = new DateTimeOffset(2026, 6, 25, 0, 0, 0, TimeSpan.Zero),
            CreatedByUserId = (Guid?)null,
            UpdatedAt = (DateTimeOffset?)null,
            UpdatedByUserId = (Guid?)null
        };
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(userRole => userRole.Id);
        builder.HasOne<User>()
            .WithMany(user => user.Roles)
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();
        builder.HasIndex(userRole => userRole.AssignedAt);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Code).HasMaxLength(150).IsRequired();
        builder.Property(permission => permission.Name).HasMaxLength(150).IsRequired();
        builder.Property(permission => permission.Domain).HasMaxLength(80).IsRequired();
        builder.Property(permission => permission.Description).HasMaxLength(1000);
        builder.HasIndex(permission => permission.Code).IsUnique();
        builder.HasIndex(permission => new { permission.Domain, permission.IsActive });
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(item => item.Id);
        builder.HasOne<Role>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Permission>().WithMany().HasForeignKey(item => item.PermissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.RoleId, item.PermissionId }).IsUnique();
    }
}

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Code).HasMaxLength(50).IsRequired();
        builder.Property(department => department.Name).HasMaxLength(256).IsRequired();
        builder.HasIndex(department => department.Code).IsUnique();
        builder.HasIndex(department => department.IsActive);
    }
}
