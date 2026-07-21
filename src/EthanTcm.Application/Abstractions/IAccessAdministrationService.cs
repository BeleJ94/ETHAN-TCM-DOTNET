namespace EthanTcm.Application.Abstractions;

public interface IAccessAdministrationService
{
    Task EnsureCatalogAsync(CancellationToken cancellationToken = default);
    Task<AccessUserPage> SearchUsersAsync(string? search, bool? active, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AccessUserDetails?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccessOperationResult> UpdateUserAsync(Guid id, bool isActive, IReadOnlyCollection<Guid> roleIds, string preferredCulture, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AccessRoleMatrixItem>> GetRoleMatrixAsync(CancellationToken cancellationToken = default);
    Task<AccessOperationResult> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, string reason, CancellationToken cancellationToken = default);
    Task<AccessOperationResult> BootstrapAdministratorAsync(string login, CancellationToken cancellationToken = default);
}

public sealed record AccessUserListItem(Guid Id, string Login, string DisplayName, string Email, bool IsActive, DateTimeOffset? LastSyncedAt, IReadOnlyCollection<string> Roles);
public sealed record AccessUserPage(IReadOnlyCollection<AccessUserListItem> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
public sealed record AccessRoleOption(Guid Id, string Code, string Name, bool Selected);
public sealed record AccessUserDetails(Guid Id, string Login, string DisplayName, string Email, string? ExternalId, bool IsActive, string PreferredCulture, DateTimeOffset? LastSyncedAt, IReadOnlyCollection<AccessRoleOption> Roles, IReadOnlyCollection<string> EffectivePermissions);
public sealed record AccessPermissionOption(Guid Id, string Code, string Name, string Domain, string? Description, bool Selected);
public sealed record AccessRoleMatrixItem(Guid Id, string Code, string Name, string? Description, IReadOnlyCollection<AccessPermissionOption> Permissions);
public sealed record AccessOperationResult(bool Success, string Message);
