using EthanTcm.Application.Abstractions;
using Quartz;

namespace EthanTcm.Jobs;

public sealed class DeadlineReminderJob(
    IDeadlineReminderService reminderService,
    ILogger<DeadlineReminderJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        logger.LogInformation("Starting deadline reminder job for {Today}", today);

        var result = await reminderService.RunDailyAsync(today, context.CancellationToken);

        logger.LogInformation(
            "Deadline reminder job completed. Candidates={CandidateDeclarations}, Created={Created}, Sent={Sent}, Failed={Failed}, Skipped={Skipped}",
            result.CandidateDeclarations,
            result.NotificationsCreated,
            result.NotificationsSent,
            result.NotificationsFailed,
            result.NotificationsSkipped);
    }
}
