namespace EthanTcm.Application.Abstractions;

public interface IAuditService
{
    void Add(AuditEntry entry);
    Task<IReadOnlyCollection<AuditLogListItemDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}

public sealed record AuditEntry(
    string Action,
    string EntityName,
    string EntityId,
    object? OldValues = null,
    object? NewValues = null,
    string? Module = null,
    string? Source = null);

public sealed record AuditLogQuery(
    string? Search,
    string? Action,
    string? EntityName,
    DateOnly? From,
    DateOnly? To,
    int Take = 200);

public sealed record AuditLogListItemDto(
    Guid Id,
    Guid? UserId,
    string? UserDisplayName,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt,
    string? Module,
    string? Source);

public interface IAuditRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? RequestPath { get; }
}
