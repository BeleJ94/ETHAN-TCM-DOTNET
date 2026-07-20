namespace EthanTcm.Application.Abstractions;

public interface ICorrespondenceActionReminderService
{
    Task<CorrespondenceActionReminderRunResult> RunDailyAsync(
        DateOnly today,
        CancellationToken cancellationToken = default);
}

public sealed record CorrespondenceActionReminderRunResult(
    int CandidateActions,
    int NotificationsCreated,
    int NotificationsSent,
    int NotificationsFailed,
    int NotificationsSkipped,
    int ActionsEscalated);
