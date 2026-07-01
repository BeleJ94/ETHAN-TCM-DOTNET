using System.Security.Claims;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;

namespace EthanTcm.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(EthanTcmClaimTypes.UserId);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Login => User?.FindFirstValue(EthanTcmClaimTypes.Login)
        ?? User?.Identity?.Name;

    public string? DisplayName => User?.FindFirstValue(ClaimTypes.Name) ?? Login;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public Guid? DepartmentId => null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsActive => IsAuthenticated && UserId.HasValue;

    public IReadOnlyCollection<string> Roles => User?.Claims
        .Where(claim => claim.Type == ClaimTypes.Role)
        .Select(claim => claim.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) == true;
    }
}
