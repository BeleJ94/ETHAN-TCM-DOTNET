using System.Security.Claims;

namespace EthanTcm.Application.Abstractions;

public interface IActiveDirectoryUserSyncService
{
    Task<ClaimsIdentity?> SynchronizeAsync(
        ClaimsPrincipal principal,
        IReadOnlyCollection<string> applicationRoles,
        CancellationToken cancellationToken = default);
}
