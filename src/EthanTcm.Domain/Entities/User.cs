using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class User : AuditableEntity
{
    private readonly List<UserRole> _roles = [];

    private User()
    {
    }

    public User(string login, string displayName, string email, Guid? departmentId = null)
    {
        Login = EntityGuards.Required(login, nameof(Login));
        DisplayName = EntityGuards.Required(displayName, nameof(DisplayName));
        Email = EntityGuards.Required(email, nameof(Email));
        DepartmentId = departmentId;
    }

    public string Login { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? ExternalId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? DelegatedToUserId { get; private set; }
    public DateTimeOffset? DelegationStartsAt { get; private set; }
    public DateTimeOffset? DelegationEndsAt { get; private set; }
    public string? PreferencesJson { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public void UpdateProfile(
        string displayName,
        string email,
        string? externalId,
        Guid? departmentId,
        DateTimeOffset syncedAt)
    {
        DisplayName = EntityGuards.Required(displayName, nameof(displayName));
        Email = EntityGuards.Required(email, nameof(email));
        ExternalId = externalId;
        DepartmentId = departmentId;
        LastSyncedAt = syncedAt;
        IsActive = true;
        MarkUpdated(syncedAt);
    }

    public void SynchronizeIdentity(string displayName, string email, string? externalId, DateTimeOffset syncedAt)
    {
        DisplayName = EntityGuards.Required(displayName, nameof(displayName));
        Email = EntityGuards.Required(email, nameof(email));
        ExternalId = externalId;
        LastSyncedAt = syncedAt;
        MarkUpdated(syncedAt);
    }

    public void Activate(DateTimeOffset timestamp)
    {
        IsActive = true;
        MarkUpdated(timestamp);
    }

    public void ReplaceRoles(IEnumerable<Guid> roleIds, DateTimeOffset timestamp)
        => SynchronizeRoles(roleIds, timestamp);

    public void AssignRole(Guid roleId, DateTimeOffset assignedAt)
    {
        EntityGuards.Required(roleId, nameof(roleId));

        if (_roles.Any(role => role.RoleId == roleId))
        {
            return;
        }

        _roles.Add(new UserRole(Id, roleId, assignedAt));
        MarkUpdated(assignedAt);
    }

    public void SynchronizeRoles(IEnumerable<Guid> roleIds, DateTimeOffset timestamp)
    {
        var requiredRoleIds = roleIds.Distinct().ToArray();

        _roles.RemoveAll(role => !requiredRoleIds.Contains(role.RoleId));

        foreach (var roleId in requiredRoleIds)
        {
            if (_roles.All(role => role.RoleId != roleId))
            {
                _roles.Add(new UserRole(Id, roleId, timestamp));
            }
        }

        MarkUpdated(timestamp);
    }

    public void SetDelegation(Guid? delegatedToUserId, DateTimeOffset? startsAt, DateTimeOffset? endsAt, DateTimeOffset timestamp)
    {
        DelegatedToUserId = delegatedToUserId;
        DelegationStartsAt = startsAt;
        DelegationEndsAt = endsAt;
        MarkUpdated(timestamp);
    }

    public void SetPreferences(string? preferencesJson, DateTimeOffset timestamp)
    {
        PreferencesJson = preferencesJson;
        MarkUpdated(timestamp);
    }

    public void Deactivate(DateTimeOffset timestamp)
    {
        IsActive = false;
        MarkUpdated(timestamp);
    }
}
