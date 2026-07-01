using EthanTcm.Application.Abstractions;

namespace EthanTcm.Web.Services;

public sealed class WebAuditRequestContext(IHttpContextAccessor httpContextAccessor) : IAuditRequestContext
{
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
    public string? RequestPath => httpContextAccessor.HttpContext?.Request.Path.Value;
}
