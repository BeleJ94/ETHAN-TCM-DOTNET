namespace EthanTcm.Application.Abstractions;

public interface IDeadlineReminderService
{
    Task<DeadlineReminderRunResult> RunDailyAsync(DateOnly today, CancellationToken cancellationToken = default);
    Task EnsureDefaultNotificationRulesAsync(CancellationToken cancellationToken = default);
}

public sealed record DeadlineReminderRunResult(
    int CandidateDeclarations,
    int NotificationsCreated,
    int NotificationsSent,
    int NotificationsFailed,
    int NotificationsSkipped);
