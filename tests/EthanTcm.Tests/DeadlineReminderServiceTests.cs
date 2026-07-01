using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EthanTcm.Tests;

public sealed class DeadlineReminderServiceTests
{
    [Fact]
    public async Task Daily_run_sends_j15_reminder_to_required_recipients()
    {
        var options = CreateOptions();
        var setup = await CreateSetupAsync(options, dueDate: new DateOnly(2026, 7, 15));
        await using var dbContext = CreateDbContext(options);
        var sender = new FakeNotificationEmailSender();
        var service = CreateService(dbContext, sender);

        var result = await service.RunDailyAsync(new DateOnly(2026, 6, 30));

        Assert.Equal(1, result.CandidateDeclarations);
        Assert.Equal(5, result.NotificationsSent);
        Assert.Equal(5, sender.Messages.Count);
        Assert.True(await dbContext.NotificationLogs.CountAsync(log => log.TaxDeclarationId == setup.DeclarationId) == 5);
    }

    [Fact]
    public async Task Daily_run_does_not_duplicate_already_sent_reminders()
    {
        var options = CreateOptions();
        await CreateSetupAsync(options, dueDate: new DateOnly(2026, 7, 15));
        await using var dbContext = CreateDbContext(options);
        var sender = new FakeNotificationEmailSender();
        var service = CreateService(dbContext, sender);

        await service.RunDailyAsync(new DateOnly(2026, 6, 30));
        var secondRun = await service.RunDailyAsync(new DateOnly(2026, 6, 30));

        Assert.Equal(0, secondRun.NotificationsCreated);
        Assert.Equal(5, sender.Messages.Count);
    }

    [Fact]
    public async Task Overdue_declaration_escalates_to_tax_manager()
    {
        var options = CreateOptions();
        var setup = await CreateSetupAsync(options, dueDate: new DateOnly(2026, 6, 15));
        await using var dbContext = CreateDbContext(options);
        var sender = new FakeNotificationEmailSender();
        var service = CreateService(dbContext, sender);

        await service.RunDailyAsync(new DateOnly(2026, 6, 30));

        var taxManagerLogExists = await dbContext.NotificationLogs.AnyAsync(log =>
            log.TaxDeclarationId == setup.DeclarationId &&
            log.RecipientUserId == setup.TaxManagerId &&
            log.Subject.Contains("Late declaration"));
        Assert.True(taxManagerLogExists);
    }

    private static DeadlineReminderService CreateService(EthanTcmDbContext dbContext, FakeNotificationEmailSender sender)
    {
        return new DeadlineReminderService(dbContext, sender, new TestAuditService(dbContext), NullLogger<DeadlineReminderService>.Instance);
    }

    private static async Task<ReminderSetup> CreateSetupAsync(DbContextOptions<EthanTcmDbContext> options, DateOnly dueDate)
    {
        await using var dbContext = CreateDbContext(options);
        var legalEntity = new LegalEntity("ETHAN", "ETHAN TCM", "CD", null);
        var department = new Department("FINANCE", "Finance");
        var category = new TaxCategory("VAT", "VAT");
        var frequency = new TaxFrequency("MONTHLY", "Monthly", 12);

        var preparer = new User("preparer", "Preparer", "preparer@local");
        var approver = new User("approver", "Approver", "approver@local");
        var paymentOwner = new User("payment", "Payment Owner", "payment@local");
        var submissionOwner = new User("submission", "Submission Owner", "submission@local");
        var taxManager = new User("manager", "Tax Manager", "manager@local");
        var taxManagerRole = new Role(ApplicationRoles.TaxManager, "Tax Manager");
        taxManager.AssignRole(taxManagerRole.Id, DateTimeOffset.UtcNow);

        var obligation = new TaxObligation(
            legalEntity.Id,
            department.Id,
            category.Id,
            frequency.Id,
            preparer.Id,
            "VAT Return",
            RiskLevel.Medium,
            requiresPayment: true,
            DateTimeOffset.UtcNow);
        obligation.AddResponsible(approver.Id, ResponsibleType.Approver1, DateTimeOffset.UtcNow);
        obligation.AddResponsible(paymentOwner.Id, ResponsibleType.PaymentProcessOwner, DateTimeOffset.UtcNow);
        obligation.AddResponsible(submissionOwner.Id, ResponsibleType.SubmissionProcessOwner, DateTimeOffset.UtcNow);

        var period = new TaxPeriod(dueDate.Year, dueDate.Month, null, dueDate.AddDays(-30), dueDate, $"{dueDate:yyyy-MM}");
        var declaration = new TaxDeclaration(
            obligation.Id,
            period.Id,
            dueDate,
            period.Label,
            paymentRequired: true,
            preparer.Id);

        dbContext.AddRange(
            legalEntity,
            department,
            category,
            frequency,
            preparer,
            approver,
            paymentOwner,
            submissionOwner,
            taxManager,
            taxManagerRole,
            obligation,
            period,
            declaration);
        await dbContext.SaveChangesAsync();

        return new ReminderSetup(declaration.Id, taxManager.Id);
    }

    private static EthanTcmDbContext CreateDbContext(DbContextOptions<EthanTcmDbContext> options)
    {
        return new EthanTcmDbContext(options);
    }

    private static DbContextOptions<EthanTcmDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private sealed record ReminderSetup(Guid DeclarationId, Guid TaxManagerId);

    private sealed class FakeNotificationEmailSender : INotificationEmailSender
    {
        public List<(string RecipientEmail, string Subject, string Body)> Messages { get; } = [];

        public Task<NotificationEmailSendResult> SendAsync(
            string recipientEmail,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((recipientEmail, subject, body));
            return Task.FromResult(new NotificationEmailSendResult(true, true, null));
        }
    }
}
