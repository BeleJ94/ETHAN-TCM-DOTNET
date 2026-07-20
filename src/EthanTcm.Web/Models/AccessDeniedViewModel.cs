namespace EthanTcm.Web.Models;

public sealed class AccessDeniedViewModel
{
    public int StatusCode { get; init; } = StatusCodes.Status403Forbidden;
    public string RequestedPath { get; init; } = string.Empty;
    public string TraceIdentifier { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = [];
}
