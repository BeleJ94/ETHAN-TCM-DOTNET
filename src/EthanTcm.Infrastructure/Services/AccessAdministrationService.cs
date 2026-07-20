using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class AccessAdministrationService(EthanTcmDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IAccessAdministrationService
{
    public async Task EnsureCatalogAsync(CancellationToken ct = default)
    {
        var existing = await db.Permissions.ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, ct);
        foreach (var code in ApplicationPermissions.All)
        {
            if (existing.ContainsKey(code)) continue;
            var segments = code.Split('.');
            var domain = segments.Length > 1 ? segments[1] : "General";
            var action = segments.Last();
            var permission = new Permission(code, Humanize(action), domain, $"Allows {Humanize(action).ToLowerInvariant()} in {Humanize(domain)}.");
            db.Permissions.Add(permission);
            existing[code] = permission;
        }
        await db.SaveChangesAsync(ct);

        var roles = await db.Roles.Where(x => x.IsActive).ToListAsync(ct);
        var configuredRoleIds = await db.RolePermissions.Select(x => x.RoleId).Distinct().ToListAsync(ct);
        foreach (var role in roles.Where(role => !configuredRoleIds.Contains(role.Id)))
        {
            if (!ApplicationPermissions.RolePermissions.TryGetValue(role.Code, out var defaults)) continue;
            foreach (var code in defaults.Distinct(StringComparer.OrdinalIgnoreCase))
                db.RolePermissions.Add(new RolePermission(role.Id, existing[code].Id));
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<AccessUserPage> SearchUsersAsync(string? search, bool? active, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var value = search.Trim(); query = query.Where(x => x.Login.Contains(value) || x.DisplayName.Contains(value) || x.Email.Contains(value)); }
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        var normalizedPage = Math.Max(1, page); var size = Math.Clamp(pageSize, 10, 50); var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.DisplayName).Skip((normalizedPage - 1) * size).Take(size)
            .Select(x => new { x.Id, x.Login, x.DisplayName, x.Email, x.IsActive, x.LastSyncedAt }).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var roles = await (from ur in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on ur.RoleId equals role.Id where ids.Contains(ur.UserId) select new { ur.UserId, role.Name }).ToListAsync(ct);
        return new(rows.Select(x => new AccessUserListItem(x.Id, x.Login, x.DisplayName, x.Email, x.IsActive, x.LastSyncedAt, roles.Where(r => r.UserId == x.Id).Select(r => r.Name).OrderBy(v => v).ToArray())).ToArray(), normalizedPage, size, total);
    }

    public async Task<AccessUserDetails?> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureCatalogAsync(ct);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); if (user is null) return null;
        var selectedRoleIds = await db.UserRoles.AsNoTracking().Where(x => x.UserId == id).Select(x => x.RoleId).ToArrayAsync(ct);
        var roles = await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new AccessRoleOption(x.Id, x.Code, x.Name, selectedRoleIds.Contains(x.Id))).ToListAsync(ct);
        var permissions = await (from ur in db.UserRoles.AsNoTracking() join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id where ur.UserId == id && p.IsActive select p.Code).Distinct().OrderBy(x => x).ToListAsync(ct);
        return new(user.Id, user.Login, user.DisplayName, user.Email, user.ExternalId, user.IsActive, user.LastSyncedAt, roles, permissions);
    }

    public async Task<AccessOperationResult> UpdateUserAsync(Guid id, bool isActive, IReadOnlyCollection<Guid> roleIds, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "A reason is required for every access change.");
        var user = await db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id, ct); if (user is null) return new(false, "User not found.");
        var validRoleIds = await db.Roles.Where(x => x.IsActive && roleIds.Contains(x.Id)).Select(x => x.Id).ToArrayAsync(ct);
        var adminRoleId = await db.Roles.Where(x => x.Code == ApplicationRoles.Administrator).Select(x => x.Id).SingleAsync(ct);
        var removesAdministrator = user.Roles.Any(x => x.RoleId == adminRoleId) && (!isActive || !validRoleIds.Contains(adminRoleId));
        if (removesAdministrator && await ActiveAdministratorCount(ct) <= 1) return new(false, "The last active administrator cannot be disabled or demoted.");
        if (currentUser.UserId == id && removesAdministrator) return new(false, "You cannot remove your own active administrator access.");
        var oldRoles = user.Roles.Select(x => x.RoleId).ToArray(); var oldActive = user.IsActive;
        user.ReplaceRoles(validRoleIds, DateTimeOffset.UtcNow);
        foreach (var assignment in user.Roles.Where(x => !oldRoles.Contains(x.RoleId))) db.Entry(assignment).State = EntityState.Added;
        if (isActive) user.Activate(DateTimeOffset.UtcNow); else user.Deactivate(DateTimeOffset.UtcNow);
        audit.Add(new("UpdateAccess", nameof(User), id.ToString(), new { IsActive = oldActive, RoleIds = oldRoles }, new { IsActive = isActive, RoleIds = validRoleIds, Reason = reason.Trim() }, "Access Administration", "Web"));
        await db.SaveChangesAsync(ct); return new(true, "User access updated successfully.");
    }

    public async Task<IReadOnlyCollection<AccessRoleMatrixItem>> GetRoleMatrixAsync(CancellationToken ct = default)
    {
        await EnsureCatalogAsync(ct);
        var roles = await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
        var permissions = await db.Permissions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Domain).ThenBy(x => x.Name).ToListAsync(ct);
        var grants = await db.RolePermissions.AsNoTracking().ToListAsync(ct);
        return roles.Select(role => new AccessRoleMatrixItem(role.Id, role.Code, role.Name, role.Description,
            permissions.Select(p => new AccessPermissionOption(p.Id, p.Code, p.Name, p.Domain, p.Description, grants.Any(g => g.RoleId == role.Id && g.PermissionId == p.Id))).ToArray())).ToArray();
    }

    public async Task<AccessOperationResult> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "A reason is required for every permission change.");
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == roleId && x.IsActive, ct); if (role is null) return new(false, "Role not found.");
        var valid = await db.Permissions.Where(x => x.IsActive && permissionIds.Contains(x.Id)).Select(x => x.Id).ToArrayAsync(ct);
        if (role.Code == ApplicationRoles.Administrator)
        {
            var required = await db.Permissions.Where(x => x.Code == ApplicationPermissions.RunAdministrationTasks).Select(x => x.Id).SingleAsync(ct);
            if (!valid.Contains(required)) return new(false, "Administrator must retain administration access.");
        }
        var existing = await db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct); var old = existing.Select(x => x.PermissionId).ToArray();
        db.RolePermissions.RemoveRange(existing.Where(x => !valid.Contains(x.PermissionId)));
        db.RolePermissions.AddRange(valid.Where(id => existing.All(x => x.PermissionId != id)).Select(id => new RolePermission(roleId, id)));
        audit.Add(new("UpdateRolePermissions", nameof(Role), roleId.ToString(), new { PermissionIds = old }, new { PermissionIds = valid, Reason = reason.Trim() }, "Access Administration", "Web"));
        await db.SaveChangesAsync(ct); return new(true, "Role permissions updated successfully.");
    }

    public async Task<AccessOperationResult> BootstrapAdministratorAsync(string login, CancellationToken ct = default)
    {
        var normalized = login.Trim(); if (normalized.Length == 0) return new(false, "Login is required."); await EnsureCatalogAsync(ct);
        var user = await db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Login == normalized, ct);
        if (user is null) { user = new User(normalized, normalized, $"{normalized.Replace('\\', '.')}@local"); db.Users.Add(user); }
        var roleId = await db.Roles.Where(x => x.Code == ApplicationRoles.Administrator).Select(x => x.Id).SingleAsync(ct);
        var alreadyAdministrator = user.Roles.Any(x => x.RoleId == roleId);
        user.Activate(DateTimeOffset.UtcNow); user.AssignRole(roleId, DateTimeOffset.UtcNow);
        if (!alreadyAdministrator) db.Entry(user.Roles.Single(x => x.RoleId == roleId)).State = EntityState.Added;
        await db.SaveChangesAsync(ct);
        return new(true, $"Administrator access granted to {normalized}.");
    }

    private async Task<int> ActiveAdministratorCount(CancellationToken ct)
    {
        var roleId = await db.Roles.Where(x => x.Code == ApplicationRoles.Administrator).Select(x => x.Id).SingleAsync(ct);
        return await db.Users.CountAsync(u => u.IsActive && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId), ct);
    }
    private static string Humanize(string value) => string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
