using EthanTcm.Application.Abstractions;

namespace EthanTcm.Infrastructure.Services;

public sealed class DevelopmentCurrentUser : ICurrentUser
{
    public string Login => "dev.user";
    public string DisplayName => "Development User";
    public bool IsAuthenticated => true;
}
