namespace EthanTcm.Application.Authentication;

public sealed class EthanTcmAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public AuthMode Mode { get; set; } = AuthMode.LocalAuth;
    public LocalAuthOptions LocalAuth { get; set; } = new();
    public ActiveDirectoryOptions ActiveDirectory { get; set; } = new();
}

public sealed class LocalAuthOptions
{
    public string Login { get; set; } = "dev.user";
    public string DisplayName { get; set; } = "Development User";
    public string Email { get; set; } = "dev.user@local";
    public string[] Roles { get; set; } = [ApplicationRoles.Administrator];
}

public sealed class ActiveDirectoryOptions
{
    public bool AutoProvisionUsers { get; set; } = true;
    public string DefaultRole { get; set; } = ApplicationRoles.ReadOnly;
    public Dictionary<string, string[]> GroupRoleMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
