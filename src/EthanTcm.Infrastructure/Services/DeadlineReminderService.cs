using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EthanTcm.Infrastructure.Services;

public sealed class DeadlineReminderService(
    EthanTcmDbContext dbContext,
    INotificationEmailSender emailSender,
    IAuditService auditService,
    ILogger<DeadlineReminderService> logger)
    : IDeadlineReminderService
{
    private static readonly TaxDeclarationStatus[] CompletedStatuses =
    [
        TaxDeclarationStatus.Submitted,
        TaxDeclarationStatus.Paid,
        TaxDeclarationStatus.Closed,
        TaxDeclarationStatus.Cancelled,
        TaxDeclarationStatus.NotApplicable
    ];

    public async Task EnsureDefaultNotificationRulesAsync(CancellationToken cancellationToken = default)
    {
        var beforeTemplate = await EnsureTemplateAsync(
            "DUE_DATE_REMINDER",
            "Due date reminder",
            "[ETHAN TCM] {ObligationName} due in {DaysUntilDue} day(s)",
            "Declaration {ObligationName} for {PeriodLabel} is due on {DueDate}. Current status: {Status}.",
            cancellationToken);
        var todayTemplate = await EnsureTemplateAsync(
            "DUE_DATE_TODAY",
            "Due date today",
            "[ETHAN TCM] {ObligationName} is due today",
            "Declaration {ObligationName} for {PeriodLabel} is due today ({DueDate}). Current status: {Status}.",
            cancellationToken);
        var lateTemplate = await EnsureTemplateAsync(
            "DUE_DATE_LATE",
            "Late declaration escalation",
            "[ETHAN TCM] Late declaration: {ObligationName}",
            "Declaration {ObligationName} for {PeriodLabel} was due on {DueDate} and is now {DaysLate} day(s) late. Current status: {Status}.",
            cancellationToken);

        foreach (var offset in new[] { 30, 15, 10, 5, 1 })
        {
            await EnsureRuleAsync(
                $"DUE_MINUS_{offset}",
                beforeTemplate.Id,
                NotificationTrigger.BeforeDueDate,
                offset,
                notifyResponsible: true,
                notifyApprover: true,
                cancellationToken);
        }

        await EnsureRuleAsync(
            "DUE_TODAY",
            todayTemplate.Id,
            NotificationTrigger.OnDueDate,
            0,
            notifyResponsible: true,
            notifyApprover: true,
            cancellationToken);
        await EnsureRuleAsync(
            "DUE_OVERDUE_ESCALATION",
            lateTemplate.Id,
            NotificationTrigger.AfterDueDate,
            -1,
            notifyResponsible: true,
            notifyApprover: true,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeadlineReminderRunResult> RunDailyAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultNotificationRulesAsync(cancellationToken);

        var rules = await dbContext.NotificationRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .ToArrayAsync(cancellationToken);
        var templateIds = rules.Select(rule => rule.NotificationTemplateId).Distinct().ToArray();
        var templates = await dbContext.NotificationTemplates
            .AsNoTracking()
            .Where(template => templateIds.Contains(template.Id) && template.IsActive)
            .ToDictionaryAsync(template => template.Id, cancellationToken);

        var declarations = await dbContext.TaxDeclarations
            .AsNoTracking()
            .Include(declaration => declaration.TaxObligation)
            .ThenInclude(obligation => obligation!.Responsibles)
            .Where(declaration => !CompletedStatuses.Contains(declaration.Status))
            .Where(declaration => declaration.DueDate >= today.AddDays(-60) && declaration.DueDate <= today.AddDays(30))
            .OrderBy(declaration => declaration.DueDate)
            .ToArrayAsync(cancellationToken);

        var userIds = declarations
            .Select(declaration => declaration.AssignedToUserId)
            .Concat(declarations.SelectMany(declaration => declaration.TaxObligation?.Responsibles.Select(responsible => responsible.UserId) ?? []))
            .Distinct()
            .ToArray();
        var roleRecipients = await LoadRoleRecipientsAsync(cancellationToken);
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive && (userIds.Contains(user.Id) || roleRecipients.UserIds.Contains(user.Id)))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var created = 0;
        var sent = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var declaration in declarations)
        {
            var daysUntilDue = declaration.DueDate.DayNumber - today.DayNumber;
            var matchingRules = rules.Where(rule => IsRuleMatch(rule, daysUntilDue)).ToArray();

            foreach (var rule in matchingRules)
            {
                if (!templates.TryGetValue(rule.NotificationTemplateId, out var template))
                {
                    skipped++;
                    continue;
                }

                var recipients = ResolveRecipients(declaration, rule, roleRecipients)
                    .Where(users.ContainsKey)
                    .Distinct()
                    .ToArray();

                foreach (var recipientId in recipients)
                {
                    if (await AlreadyLoggedAsync(rule.Id, declaration.Id, recipientId, cancellationToken))
                    {
                        skipped++;
                        continue;
                    }

                    var recipient = users[recipientId];
                    var subject = Render(template.Subject, declaration, daysUntilDue);
                    var body = Render(template.Body, declaration, daysUntilDue);
                    var log = new NotificationLog(rule.Id, declaration.Id, recipient.Id, subject, body);
                    dbContext.NotificationLogs.Add(log);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    created++;

                    var result = await emailSender.SendAsync(recipient.Email, subject, body, cancellationToken);
                    if (result.Sent)
                    {
                        log.MarkSent(DateTimeOffset.UtcNow);
                        auditService.Add(new AuditEntry(
                            "SendNotification",
                            nameof(NotificationLog),
                            log.Id.ToString(),
                            null,
                            new
                            {
                                DeclarationId = declaration.Id,
                                RecipientUserId = recipient.Id,
                                recipient.Email,
                                rule.Code,
                                DryRun = result.DryRun
                            },
                            "Notifications",
                            "Jobs"));
                        sent++;
                    }
                    else
                    {
                        log.MarkFailed(result.ErrorMessage ?? "Notification email failed.", DateTimeOffset.UtcNow);
                        auditService.Add(new AuditEntry(
                            "SendNotificationFailed",
                            nameof(NotificationLog),
                            log.Id.ToString(),
                            null,
                            new
                            {
                                DeclarationId = declaration.Id,
                                RecipientUserId = recipient.Id,
                                recipient.Email,
                                rule.Code,
                                result.ErrorMessage
                            },
                            "Notifications",
                            "Jobs"));
                        failed++;
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        logger.LogInformation(
            "Deadline reminder run completed. Candidates={CandidateDeclarations}, Created={Created}, Sent={Sent}, Failed={Failed}, Skipped={Skipped}",
            declarations.Length,
            created,
            sent,
            failed,
            skipped);

        return new DeadlineReminderRunResult(declarations.Length, created, sent, failed, skipped);
    }

    private async Task<NotificationTemplate> EnsureTemplateAsync(
        string code,
        string name,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (template is not null)
        {
            return template;
        }

        template = new NotificationTemplate(code, name, NotificationChannel.Email, subject, body);
        dbContext.NotificationTemplates.Add(template);
        return template;
    }

    private async Task EnsureRuleAsync(
        string code,
        Guid templateId,
        NotificationTrigger trigger,
        int offsetDays,
        bool notifyResponsible,
        bool notifyApprover,
        CancellationToken cancellationToken)
    {
        if (await dbContext.NotificationRules.AnyAsync(rule => rule.Code == code, cancellationToken))
        {
            return;
        }

        dbContext.NotificationRules.Add(new NotificationRule(
            code,
            templateId,
            trigger,
            offsetDays,
            notifyResponsible,
            notifyApprover));
    }

    private static bool IsRuleMatch(NotificationRule rule, int daysUntilDue)
    {
        return rule.Trigger switch
        {
            NotificationTrigger.BeforeDueDate => daysUntilDue == rule.OffsetDays,
            NotificationTrigger.OnDueDate => daysUntilDue == 0,
            NotificationTrigger.AfterDueDate => daysUntilDue < 0,
            _ => false
        };
    }

    private async Task<bool> AlreadyLoggedAsync(
        Guid ruleId,
        Guid declarationId,
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        return await dbContext.NotificationLogs.AnyAsync(log =>
            log.NotificationRuleId == ruleId &&
            log.TaxDeclarationId == declarationId &&
            log.RecipientUserId == recipientId &&
            log.Status != NotificationStatus.Failed,
            cancellationToken);
    }

    private static IEnumerable<Guid> ResolveRecipients(
        TaxDeclaration declaration,
        NotificationRule rule,
        RoleRecipientLookup roleRecipients)
    {
        if (rule.NotifyResponsible)
        {
            yield return declaration.AssignedToUserId;

            foreach (var responsible in declaration.TaxObligation?.Responsibles ?? [])
            {
                if (responsible.Type is ResponsibleType.Primary
                    or ResponsibleType.Preparer
                    or ResponsibleType.PaymentProcessOwner
                    or ResponsibleType.SubmissionProcessOwner
                    or ResponsibleType.FollowUpOwner)
                {
                    yield return responsible.UserId;
                }
            }

            foreach (var financeManagerId in roleRecipients.FinanceManagerIds)
            {
                yield return financeManagerId;
            }
        }

        if (rule.NotifyApprover)
        {
            foreach (var responsible in declaration.TaxObligation?.Responsibles ?? [])
            {
                if (responsible.Type is ResponsibleType.Approver or ResponsibleType.Approver1 or ResponsibleType.Approver2 or ResponsibleType.Approver3)
                {
                    yield return responsible.UserId;
                }
            }

            foreach (var approverId in roleRecipients.ApproverIds)
            {
                yield return approverId;
            }
        }

        foreach (var taxManagerId in roleRecipients.TaxManagerIds)
        {
            yield return taxManagerId;
        }
    }

    private async Task<RoleRecipientLookup> LoadRoleRecipientsAsync(CancellationToken cancellationToken)
    {
        var roleUsers = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.IsActive && (role.Code == ApplicationRoles.Approver ||
                role.Code == ApplicationRoles.FinanceManager ||
                role.Code == ApplicationRoles.TaxManager)
            select new { userRole.UserId, role.Code })
            .ToArrayAsync(cancellationToken);

        return new RoleRecipientLookup(
            roleUsers.Where(item => item.Code == ApplicationRoles.Approver).Select(item => item.UserId).Distinct().ToArray(),
            roleUsers.Where(item => item.Code == ApplicationRoles.FinanceManager).Select(item => item.UserId).Distinct().ToArray(),
            roleUsers.Where(item => item.Code == ApplicationRoles.TaxManager).Select(item => item.UserId).Distinct().ToArray());
    }

    private static string Render(string template, TaxDeclaration declaration, int daysUntilDue)
    {
        var daysLate = Math.Max(0, -daysUntilDue);
        return template
            .Replace("{ObligationName}", declaration.TaxObligation?.Name ?? "-", StringComparison.Ordinal)
            .Replace("{PeriodLabel}", declaration.PeriodLabel, StringComparison.Ordinal)
            .Replace("{DueDate}", declaration.DueDate.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
            .Replace("{Status}", declaration.Status.ToString(), StringComparison.Ordinal)
            .Replace("{DaysUntilDue}", Math.Max(0, daysUntilDue).ToString(), StringComparison.Ordinal)
            .Replace("{DaysLate}", daysLate.ToString(), StringComparison.Ordinal);
    }

    private sealed record RoleRecipientLookup(
        IReadOnlyCollection<Guid> ApproverIds,
        IReadOnlyCollection<Guid> FinanceManagerIds,
        IReadOnlyCollection<Guid> TaxManagerIds)
    {
        public IReadOnlyCollection<Guid> UserIds =>
            ApproverIds.Concat(FinanceManagerIds).Concat(TaxManagerIds).Distinct().ToArray();
    }
}
