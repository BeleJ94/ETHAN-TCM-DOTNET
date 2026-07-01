using System.Text.Json;
using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class AuditService(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditRequestContext requestContext)
    : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(AuditEntry entry)
    {
        dbContext.AuditLogs.Add(new AuditLog(
            currentUserService.UserId,
            entry.EntityName,
            entry.EntityId,
            entry.Action,
            entry.OldValues is null ? null : JsonSerializer.Serialize(entry.OldValues, JsonOptions),
            entry.NewValues is null ? null : JsonSerializer.Serialize(entry.NewValues, JsonOptions),
            DateTimeOffset.UtcNow,
            currentUserService.DisplayName ?? currentUserService.Login,
            module: entry.Module,
            source: entry.Source,
            ipAddress: requestContext.IpAddress,
            userAgent: requestContext.UserAgent,
            requestPath: requestContext.RequestPath));
    }

    public async Task<IReadOnlyCollection<AuditLogListItemDto>> ListAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, 500);
        var logs = dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(log => log.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            logs = logs.Where(log => log.EntityName == query.EntityName);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            logs = logs.Where(log => log.OccurredAt >= from);
        }

        if (query.To.HasValue)
        {
            var toExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            logs = logs.Where(log => log.OccurredAt < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            logs = logs.Where(log =>
                log.EntityName.Contains(search) ||
                log.EntityId.Contains(search) ||
                log.Action.Contains(search) ||
                (log.UserDisplayName != null && log.UserDisplayName.Contains(search)));
        }

        return await logs
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .Select(log => new AuditLogListItemDto(
                log.Id,
                log.UserId,
                log.UserDisplayName,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.OldValue,
                log.NewValue,
                log.IpAddress,
                log.UserAgent,
                log.CreatedAt,
                log.Module,
                log.Source))
            .ToArrayAsync(cancellationToken);
    }
}

public sealed class EmptyAuditRequestContext : IAuditRequestContext
{
    public string? IpAddress => null;
    public string? UserAgent => null;
    public string? RequestPath => null;
}
