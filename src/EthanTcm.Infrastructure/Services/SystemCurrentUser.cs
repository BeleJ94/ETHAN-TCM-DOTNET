using EthanTcm.Application.Abstractions;

namespace EthanTcm.Infrastructure.Services;

public sealed class SystemCurrentUser : ICurrentUser
{
    public string Login => "system";
    public string DisplayName => "ETHAN TCM System";
    public bool IsAuthenticated => true;
}
