using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EthanTcm.Tests;

public sealed class CorrespondenceActionReminderServiceTests
{
    [Fact]
    public async Task J5_reminder_is_sent_once_to_action_owner()
    {
        var options = Options();
        await SeedAsync(options, new DateOnly(2026, 7, 21));
        await using var db = new EthanTcmDbContext(options);
        var sender = new FakeSender(); var service = Service(db, sender);

        var first = await service.RunDailyAsync(new DateOnly(2026, 7, 16));
        var second = await service.RunDailyAsync(new DateOnly(2026, 7, 16));

        Assert.Equal(1, first.NotificationsSent);
        Assert.Equal(0, second.NotificationsCreated);
        Assert.Single(sender.Messages);
        Assert.Equal("J-5", (await db.CorrespondenceActionNotifications.SingleAsync()).Trigger);
    }

    [Fact]
    public async Task J3_overdue_action_is_escalated_to_tax_manager()
    {
        var options = Options();
        var ids = await SeedAsync(options, new DateOnly(2026, 7, 13));
        await using var db = new EthanTcmDbContext(options);
        var sender = new FakeSender(); var service = Service(db, sender);

        var result = await service.RunDailyAsync(new DateOnly(2026, 7, 16));

        Assert.Equal(1, result.ActionsEscalated);
        Assert.Contains(sender.Messages, x => x.Email == "manager@local" && x.Subject.Contains("Escalation"));
        Assert.True((await db.CorrespondenceFollowUpActions.FindAsync(ids.ActionId))!.IsEscalated);
    }

    private static CorrespondenceActionReminderService Service(EthanTcmDbContext db, FakeSender sender) =>
        new(db, sender, new FakeAudit(), NullLogger<CorrespondenceActionReminderService>.Instance);

    private static async Task<Ids> SeedAsync(DbContextOptions<EthanTcmDbContext> options, DateOnly dueDate)
    {
        await using var db = new EthanTcmDbContext(options);
        var owner = new User("owner", "Action Owner", "owner@local");
        var manager = new User("manager", "Tax Manager", "manager@local");
        var role = new Role(ApplicationRoles.TaxManager, "Tax Manager");
        manager.AssignRole(role.Id, DateTimeOffset.UtcNow);
        var correspondence = new Correspondence(CorrespondenceDirection.Incoming, "Authority request", "DGI/26/10",
            dueDate.AddDays(-10), CorrespondencePriority.High, CorrespondenceConfidentiality.Internal,
            CorrespondenceChannel.Email, "DGI", "DGI", null, null, "Prepare a response");
        var action = new CorrespondenceFollowUpAction(correspondence.Id, "Prepare response", null, owner.Id,
            dueDate, CorrespondencePriority.High, owner.Id);
        db.AddRange(owner, manager, role, correspondence, action);
        await db.SaveChangesAsync();
        return new(action.Id);
    }

    private static DbContextOptions<EthanTcmDbContext> Options() => new DbContextOptionsBuilder<EthanTcmDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private sealed record Ids(Guid ActionId);
    private sealed class FakeSender : INotificationEmailSender
    {
        public List<(string Email, string Subject)> Messages { get; } = [];
        public Task<NotificationEmailSendResult> SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default)
        { Messages.Add((recipientEmail, subject)); return Task.FromResult(new NotificationEmailSendResult(true, true, null)); }
    }
    private sealed class FakeAudit : IAuditService
    {
        public void Add(AuditEntry entry) { }
        public Task<IReadOnlyCollection<AuditLogListItemDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<AuditLogListItemDto>>([]);
    }
}
