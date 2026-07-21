using EthanTcm.Application.Abstractions;

namespace EthanTcm.Web.Models;

public sealed class AuditLogIndexViewModel
{
    public string? Search { get; set; }
    public string? ActionFilter { get; set; }
    public string? EntityName { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public IReadOnlyCollection<AuditLogListItemDto> Items { get; init; } = [];
}
