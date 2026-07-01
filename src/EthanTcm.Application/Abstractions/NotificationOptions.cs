namespace EthanTcm.Application.Abstractions;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public bool DryRun { get; set; } = true;
    public string DailyCron { get; set; } = "0 0 7 ? * *";
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@ethantcm.local";
}
