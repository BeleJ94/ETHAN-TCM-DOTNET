using EthanTcm.Application.Abstractions;
using Quartz;

namespace EthanTcm.Jobs;

[DisallowConcurrentExecution]
public sealed class CorrespondenceActionReminderJob(
    ICorrespondenceActionReminderService reminderService,
    ILogger<CorrespondenceActionReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await reminderService.RunDailyAsync(today, context.CancellationToken);
        logger.LogInformation(
            "Correspondence reminders completed. Candidates={Candidates}, Created={Created}, Sent={Sent}, Failed={Failed}, Skipped={Skipped}, Escalated={Escalated}",
            result.CandidateActions, result.NotificationsCreated, result.NotificationsSent,
            result.NotificationsFailed, result.NotificationsSkipped, result.ActionsEscalated);
    }
}
