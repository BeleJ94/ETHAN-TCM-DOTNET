namespace EthanTcm.Web.Models;

public sealed class AccountProfileViewModel
{
    public string Login { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = [];
}
