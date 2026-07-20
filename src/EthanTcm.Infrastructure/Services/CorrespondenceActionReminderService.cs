using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EthanTcm.Infrastructure.Services;

public sealed class CorrespondenceActionReminderService(
    EthanTcmDbContext dbContext,
    INotificationEmailSender emailSender,
    IAuditService auditService,
    ILogger<CorrespondenceActionReminderService> logger)
    : ICorrespondenceActionReminderService
{
    public async Task<CorrespondenceActionReminderRunResult> RunDailyAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var finalStatuses = new[] { CorrespondenceActionStatus.Completed, CorrespondenceActionStatus.Cancelled };
        var actions = await dbContext.CorrespondenceFollowUpActions
            .Where(action => !finalStatuses.Contains(action.Status) && action.DueDate <= today.AddDays(5))
            .ToArrayAsync(cancellationToken);

        if (actions.Length == 0)
            return new(0, 0, 0, 0, 0, 0);

        var correspondenceIds = actions.Select(x => x.CorrespondenceId).Distinct().ToArray();
        var correspondences = await dbContext.Correspondences.AsNoTracking()
            .Where(x => correspondenceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var users = await dbContext.Users.AsNoTracking().Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var taxManagerRoleIds = await dbContext.Roles.AsNoTracking()
            .Where(x => x.IsActive && x.Code == ApplicationRoles.TaxManager)
            .Select(x => x.Id).ToArrayAsync(cancellationToken);
        var managerIds = await dbContext.UserRoles.AsNoTracking()
            .Where(x => taxManagerRoleIds.Contains(x.RoleId))
            .Select(x => x.UserId).Distinct().ToArrayAsync(cancellationToken);

        var actionIds = actions.Select(x => x.Id).ToArray();
        var existing = await dbContext.CorrespondenceActionNotifications.AsNoTracking()
            .Where(x => actionIds.Contains(x.CorrespondenceActionId))
            .Select(x => new { x.CorrespondenceActionId, x.RecipientUserId, x.Trigger })
            .ToArrayAsync(cancellationToken);
        var keys = existing.Select(x => Key(x.CorrespondenceActionId, x.RecipientUserId, x.Trigger)).ToHashSet();

        var created = 0; var sent = 0; var failed = 0; var skipped = 0; var escalated = 0;
        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!correspondences.TryGetValue(action.CorrespondenceId, out var correspondence)) { skipped++; continue; }
            var days = action.DueDate.DayNumber - today.DayNumber;
            var trigger = days switch { 5 => "J-5", 2 => "J-2", 0 => "DUE-TODAY", < 0 => "OVERDUE", _ => null };
            if (trigger is not null && users.TryGetValue(action.AssignedToUserId, out var owner))
                await NotifyAsync(action, correspondence, owner, trigger, today, days, keys, cancellationToken,
                    () => created++, () => sent++, () => failed++, () => skipped++);

            if (days > -3 || action.IsEscalated) continue;
            var recipients = action.EscalationUserId.HasValue
                ? new[] { action.EscalationUserId.Value }
                : managerIds;
            var validRecipients = recipients.Distinct().Where(users.ContainsKey).ToArray();
            if (validRecipients.Length == 0) { skipped++; continue; }

            foreach (var recipientId in validRecipients)
                await NotifyAsync(action, correspondence, users[recipientId], "ESCALATION-J+3", today, days, keys, cancellationToken,
                    () => created++, () => sent++, () => failed++, () => skipped++);
            action.Escalate(DateTimeOffset.UtcNow);
            auditService.Add(new AuditEntry("ActionEscalated", nameof(CorrespondenceFollowUpAction), action.Id.ToString(),
                NewValues: new { action.DueDate, DaysOverdue = -days, Recipients = validRecipients }, Module: "Correspondences", Source: "Jobs"));
            escalated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new(actions.Length, created, sent, failed, skipped, escalated);
    }

    private async Task NotifyAsync(
        CorrespondenceFollowUpAction action, Correspondence correspondence, User recipient,
        string trigger, DateOnly today, int days, HashSet<string> keys, CancellationToken cancellationToken,
        Action created, Action sent, Action failed, Action skipped)
    {
        var key = Key(action.Id, recipient.Id, trigger);
        if (!keys.Add(key)) { skipped(); return; }
        var reference = correspondence.ReferenceNumber ?? correspondence.BusinessReference ?? correspondence.Id.ToString("N")[..8];
        var urgency = days >= 0 ? $"due in {days} day(s)" : $"overdue by {-days} day(s)";
        var subject = trigger == "ESCALATION-J+3"
            ? $"[Escalation] Correspondence action overdue — {reference}"
            : $"[Action reminder {trigger}] {action.Title} — {reference}";
        var body = $"Hello {recipient.DisplayName},\n\nAction: {action.Title}\nCorrespondence: {reference} — {correspondence.Subject}\nDue date: {action.DueDate:yyyy-MM-dd} ({urgency})\nPriority: {action.Priority}\n\nOpen the correspondence action cockpit to take action.";
        var log = new CorrespondenceActionNotification(action.Id, recipient.Id, trigger, today, subject, body);
        dbContext.CorrespondenceActionNotifications.Add(log); created();
        var result = await emailSender.SendAsync(recipient.Email, subject, body, cancellationToken);
        if (result.Sent) { log.MarkSent(DateTimeOffset.UtcNow); sent(); }
        else { log.MarkFailed(result.ErrorMessage ?? "Notification could not be sent.", DateTimeOffset.UtcNow); failed(); }
        auditService.Add(new AuditEntry(result.Sent ? "ReminderSent" : "ReminderFailed", nameof(CorrespondenceFollowUpAction), action.Id.ToString(),
            NewValues: new { Trigger = trigger, Recipient = recipient.Email, result.DryRun, result.ErrorMessage }, Module: "Correspondences", Source: "Jobs"));
        logger.LogInformation("Correspondence action reminder {Trigger} processed for {ActionId} and {Recipient}", trigger, action.Id, recipient.Email);
    }

    private static string Key(Guid actionId, Guid recipientId, string trigger) => $"{actionId:N}:{recipientId:N}:{trigger}";
}
