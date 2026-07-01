using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class ApplicationUser : AuditableEntity
{
    private ApplicationUser()
    {
    }

    public ApplicationUser(string login, string displayName, string email)
    {
        Login = login;
        DisplayName = displayName;
        Email = email;
    }

    public string Login { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Department { get; private set; }
    public bool IsActive { get; private set; } = true;
}
