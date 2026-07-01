using System.Text.Json;
using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Tests;

internal sealed class TestAuditService(EthanTcmDbContext dbContext, Guid? userId = null) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(AuditEntry entry)
    {
        dbContext.AuditLogs.Add(new AuditLog(
            userId,
            entry.EntityName,
            entry.EntityId,
            entry.Action,
            entry.OldValues is null ? null : JsonSerializer.Serialize(entry.OldValues, JsonOptions),
            entry.NewValues is null ? null : JsonSerializer.Serialize(entry.NewValues, JsonOptions),
            DateTimeOffset.UtcNow,
            "Test User",
            module: entry.Module,
            source: entry.Source));
    }

    public async Task<IReadOnlyCollection<AuditLogListItemDto>> ListAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .Take(query.Take)
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
