namespace EthanTcm.Application.Abstractions;

public interface INotificationEmailSender
{
    Task<NotificationEmailSendResult> SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationEmailSendResult(
    bool Sent,
    bool DryRun,
    string? ErrorMessage);
