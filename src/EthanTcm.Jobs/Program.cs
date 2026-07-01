using EthanTcm.Jobs;
using EthanTcm.Application;
using EthanTcm.Application.Abstractions;
using EthanTcm.Infrastructure;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ETHAN TCM Jobs";
});

builder.Services.AddQuartz(options =>
{
    var jobKey = new JobKey(nameof(DeadlineReminderJob));
    var cron = builder.Configuration.GetSection(NotificationOptions.SectionName)
        .GetValue(nameof(NotificationOptions.DailyCron), "0 0 7 ? * *");

    options.AddJob<DeadlineReminderJob>(jobKey);
    options.AddTrigger(trigger => trigger
        .ForJob(jobKey)
        .WithIdentity($"{nameof(DeadlineReminderJob)}Trigger")
        .WithCronSchedule(cron));
});
builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var host = builder.Build();
host.Run();
