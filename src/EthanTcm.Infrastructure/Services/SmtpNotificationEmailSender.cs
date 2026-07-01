using System.Net;
using System.Net.Mail;
using EthanTcm.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EthanTcm.Infrastructure.Services;

public sealed class SmtpNotificationEmailSender(
    IOptions<NotificationOptions> options,
    ILogger<SmtpNotificationEmailSender> logger)
    : INotificationEmailSender
{
    public async Task<NotificationEmailSendResult> SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var notificationOptions = options.Value;
        if (notificationOptions.DryRun)
        {
            logger.LogInformation(
                "DryRun notification to {Recipient}: {Subject}. Body: {Body}",
                recipientEmail,
                subject,
                body);
            return new NotificationEmailSendResult(true, true, null);
        }

        try
        {
            using var message = new MailMessage(notificationOptions.Smtp.From, recipientEmail, subject, body);
            using var client = new SmtpClient(notificationOptions.Smtp.Host, notificationOptions.Smtp.Port)
            {
                EnableSsl = notificationOptions.Smtp.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(notificationOptions.Smtp.UserName))
            {
                client.Credentials = new NetworkCredential(
                    notificationOptions.Smtp.UserName,
                    notificationOptions.Smtp.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            return new NotificationEmailSendResult(true, false, null);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogError(ex, "Notification email failed for {Recipient}", recipientEmail);
            return new NotificationEmailSendResult(false, false, ex.Message);
        }
    }
}
