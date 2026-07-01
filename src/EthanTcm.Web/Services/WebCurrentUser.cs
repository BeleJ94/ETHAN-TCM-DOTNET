using System.Security.Claims;
using EthanTcm.Application.Abstractions;

namespace EthanTcm.Web.Services;

public sealed class WebCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? Login => User?.Identity?.Name;
    public string? DisplayName => User?.FindFirst(ClaimTypes.Name)?.Value ?? Login;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}
