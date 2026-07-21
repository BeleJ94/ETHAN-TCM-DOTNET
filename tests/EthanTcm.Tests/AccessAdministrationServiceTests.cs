using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EthanTcm.Tests;

public sealed class AccessAdministrationServiceTests
{
    [Fact]
    public async Task EnsureCatalog_creates_permissions_and_default_role_grants()
    {
        await using var db = CreateDb(); var service = CreateService(db);
        await service.EnsureCatalogAsync();
        Assert.Equal(ApplicationPermissions.All.Length, await db.Permissions.CountAsync());
        var adminRole = await db.Roles.SingleAsync(x => x.Code == ApplicationRoles.Administrator);
        Assert.Equal(ApplicationPermissions.RolePermissions[ApplicationRoles.Administrator].Distinct().Count(), await db.RolePermissions.CountAsync(x => x.RoleId == adminRole.Id));
    }

    [Fact]
    public async Task UpdateUser_prevents_removing_the_last_administrator()
    {
        await using var db = CreateDb(); var service = CreateService(db); await service.EnsureCatalogAsync();
        var adminRole = await db.Roles.SingleAsync(x => x.Code == ApplicationRoles.Administrator);
        var user = new User("admin", "Administrator", "admin@local"); user.AssignRole(adminRole.Id, DateTimeOffset.UtcNow); db.Users.Add(user); await db.SaveChangesAsync();
        var result = await service.UpdateUserAsync(user.Id, false, [], "en", "Test demotion");
        Assert.False(result.Success); Assert.Contains("last active administrator", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUser_replaces_roles_and_writes_access_audit()
    {
        await using var db = CreateDb(); var service = CreateService(db); await service.EnsureCatalogAsync();
        var preparer = await db.Roles.SingleAsync(x => x.Code == ApplicationRoles.Preparer);
        var user = new User("employee", "Employee", "employee@local"); db.Users.Add(user); await db.SaveChangesAsync();
        var result = await service.UpdateUserAsync(user.Id, true, [preparer.Id], "fr", "New tax preparation responsibility");
        Assert.True(result.Success); Assert.True(await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == preparer.Id));
        Assert.Equal("fr", (await db.Users.FindAsync(user.Id))!.PreferredCulture);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityId == user.Id.ToString() && x.Action == "UpdateAccess"));
    }

    [Fact]
    public void Identity_synchronization_does_not_reactivate_a_suspended_user()
    {
        var user = new User("employee", "Employee", "employee@local"); user.Deactivate(DateTimeOffset.UtcNow);
        user.SynchronizeIdentity("Updated Employee", "updated@local", "S-1-5-test", DateTimeOffset.UtcNow);
        Assert.False(user.IsActive); Assert.Equal("Updated Employee", user.DisplayName);
    }

    [Fact]
    public async Task Authentication_sync_preserves_application_roles_and_emits_database_permissions()
    {
        await using var db = CreateDb(); var access = CreateService(db); await access.EnsureCatalogAsync();
        var preparer = await db.Roles.SingleAsync(x => x.Code == ApplicationRoles.Preparer);
        var user = new User("DOMAIN\\employee", "Employee", "employee@local"); db.Users.Add(user); db.UserRoles.Add(new UserRole(user.Id, preparer.Id, DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        var options = Options.Create(new EthanTcmAuthenticationOptions { ActiveDirectory = new ActiveDirectoryOptions { AutoProvisionUsers = true, DefaultRole = ApplicationRoles.ReadOnly } });
        var service = new ActiveDirectoryUserSyncService(db, access, options);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "DOMAIN\\employee"), new Claim(ClaimTypes.Email, "employee@local")], "Test"));
        var identity = await service.SynchronizeAsync(principal, [ApplicationRoles.Administrator]);
        Assert.NotNull(identity);
        Assert.Contains(identity.Claims, x => x.Type == ClaimTypes.Role && x.Value == ApplicationRoles.Preparer);
        Assert.DoesNotContain(identity.Claims, x => x.Type == ClaimTypes.Role && x.Value == ApplicationRoles.Administrator);
        Assert.Contains(identity.Claims, x => x.Type == EthanTcmClaimTypes.Permission && x.Value == ApplicationPermissions.PrepareTaxDeclarations);
    }

    private static EthanTcmDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EthanTcmDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new EthanTcmDbContext(options);
        db.Roles.AddRange(ApplicationRoles.All.Select(code => new Role(code, code)));
        db.SaveChanges();
        return db;
    }
    private static AccessAdministrationService CreateService(EthanTcmDbContext db) => new(db, new TestCurrentUser(), new TestAuditService(db));
    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null; public string? Login => "test.admin"; public string? DisplayName => "Test Administrator"; public string? Email => "test@local"; public Guid? DepartmentId => null;
        public bool IsAuthenticated => true; public bool IsActive => true; public IReadOnlyCollection<string> Roles => [ApplicationRoles.Administrator]; public bool IsInRole(string role) => role == ApplicationRoles.Administrator;
    }
}
