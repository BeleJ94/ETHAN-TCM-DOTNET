using EthanTcm.Application.Abstractions;
using EthanTcm.Application.TaxDeclarations;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class TaxDeclarationGenerationService(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditService auditService)
    : ITaxDeclarationGenerationService
{
    private const string AuditModule = "Tax Declarations";

    public async Task<TaxDeclarationGenerationResult> GenerateAnnualAsync(
        int fiscalYear,
        CancellationToken cancellationToken = default)
    {
        if (fiscalYear is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Fiscal year is invalid.");
        }

        var obligations = await dbContext.TaxObligations
            .Include(obligation => obligation.Responsibles)
            .Include(obligation => obligation.ScheduleRules)
            .OrderBy(obligation => obligation.Name)
            .ToArrayAsync(cancellationToken);

        var skippedInactive = obligations.Count(obligation => !obligation.IsActive);
        var activeObligations = obligations.Where(obligation => obligation.IsActive).ToArray();
        var frequencyIds = activeObligations.Select(obligation => obligation.TaxFrequencyId).Distinct().ToArray();
        var frequencies = await dbContext.TaxFrequencies
            .AsNoTracking()
            .Where(frequency => frequencyIds.Contains(frequency.Id))
            .ToDictionaryAsync(frequency => frequency.Id, cancellationToken);

        var periods = await dbContext.TaxPeriods
            .Where(period => period.Year == fiscalYear)
            .ToDictionaryAsync(period => PeriodKey(period.PeriodType, period.Sequence), cancellationToken);

        var obligationIds = activeObligations.Select(obligation => obligation.Id).ToArray();
        var periodIds = periods.Values.Select(period => period.Id).ToArray();
        var existingDeclarations = await dbContext.TaxDeclarations
            .AsNoTracking()
            .Where(declaration =>
                obligationIds.Contains(declaration.TaxObligationId) &&
                periodIds.Contains(declaration.TaxPeriodId))
            .Select(declaration => new DeclarationKey(declaration.TaxObligationId, declaration.TaxPeriodId))
            .ToArrayAsync(cancellationToken);

        var existing = existingDeclarations.ToHashSet();
        var items = new List<TaxDeclarationGenerationItem>();
        var created = 0;
        var skippedDuplicates = 0;

        foreach (var obligation in activeObligations)
        {
            if (!frequencies.TryGetValue(obligation.TaxFrequencyId, out var frequency))
            {
                items.Add(Skipped(obligation, "-", default, null, "Frequency is missing."));
                continue;
            }

            var assignedToUserId = FindAssignedUser(obligation);
            if (assignedToUserId == Guid.Empty)
            {
                items.Add(Skipped(obligation, "-", default, null, "Preparer is missing."));
                continue;
            }

            var plan = TaxDeclarationGenerationCalendar.PlanAnnual(new TaxDeclarationPlanInput(
                fiscalYear,
                frequency.Code,
                frequency.Name,
                frequency.OccurrencesPerYear,
                obligation.ScheduleRules
                    .Select(rule => new TaxDeclarationScheduleRuleInput(rule.DueDay, rule.DueMonth, rule.MoveToNextBusinessDay))
                    .ToArray()));

            foreach (var planItem in plan)
            {
                var period = GetOrCreatePeriod(periods, planItem);
                var declarationKey = new DeclarationKey(obligation.Id, period.Id);

                if (existing.Contains(declarationKey))
                {
                    skippedDuplicates++;
                    items.Add(Skipped(obligation, planItem.PeriodLabel, planItem.DueDate, planItem.ReminderDate, "Already generated."));
                    continue;
                }

                var declaration = new TaxDeclaration(
                    obligation.Id,
                    period.Id,
                    planItem.DueDate,
                    planItem.PeriodLabel,
                    obligation.RequiresPayment,
                    assignedToUserId,
                    planItem.ReminderDate);

                MarkCreatedByCurrentUser(declaration);
                dbContext.TaxDeclarations.Add(declaration);
                AddAudit("Generate", declaration.Id, null, ToAuditPayload(obligation, declaration, period));

                existing.Add(declarationKey);
                created++;
                items.Add(new TaxDeclarationGenerationItem(
                    obligation.Id,
                    obligation.Name,
                    planItem.PeriodLabel,
                    planItem.DueDate,
                    planItem.ReminderDate,
                    true,
                    null));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaxDeclarationGenerationResult(
            fiscalYear,
            created,
            skippedDuplicates,
            skippedInactive,
            items);
    }

    public async Task<TaxDeclarationManualCreationResult> CreateManualAsync(
        TaxDeclarationManualCreationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TaxObligationId == Guid.Empty)
        {
            return new TaxDeclarationManualCreationResult(null, false, "Tax obligation is required.");
        }

        if (command.AssignedToUserId == Guid.Empty)
        {
            return new TaxDeclarationManualCreationResult(null, false, "Assigned user is required.");
        }

        if (command.DueDate < command.PeriodDate)
        {
            return new TaxDeclarationManualCreationResult(null, false, "Due date cannot be before period date.");
        }

        var obligation = await dbContext.TaxObligations
            .FirstOrDefaultAsync(item => item.Id == command.TaxObligationId, cancellationToken);

        if (obligation is null || !obligation.IsActive)
        {
            return new TaxDeclarationManualCreationResult(null, false, "Tax obligation is invalid or inactive.");
        }

        var assignedUserExists = await dbContext.Users.AnyAsync(
            user => user.Id == command.AssignedToUserId && user.IsActive,
            cancellationToken);

        if (!assignedUserExists)
        {
            return new TaxDeclarationManualCreationResult(null, false, "Assigned user is invalid.");
        }

        var label = string.IsNullOrWhiteSpace(command.PeriodLabel)
            ? $"Manual {command.PeriodDate:yyyy-MM-dd}"
            : command.PeriodLabel.Trim();

        var duplicateExists = await dbContext.TaxDeclarations.AnyAsync(
            declaration => declaration.TaxObligationId == command.TaxObligationId && declaration.PeriodLabel == label,
            cancellationToken);

        if (duplicateExists)
        {
            return new TaxDeclarationManualCreationResult(null, false, "A manual declaration with the same label already exists for this obligation.");
        }

        var sequence = await GetNextManualSequenceAsync(command.PeriodDate.Year, cancellationToken);
        var period = TaxPeriod.Manual(command.PeriodDate.Year, sequence, command.PeriodDate, label);
        var declaration = new TaxDeclaration(
            obligation.Id,
            period.Id,
            command.DueDate,
            label,
            obligation.RequiresPayment,
            command.AssignedToUserId,
            command.ReminderDate,
            command.IsNotApplicable ? TaxDeclarationStatus.NotApplicable : TaxDeclarationStatus.ToPrepare);

        MarkCreatedByCurrentUser(period);
        MarkCreatedByCurrentUser(declaration);
        dbContext.TaxPeriods.Add(period);
        dbContext.TaxDeclarations.Add(declaration);
        AddAudit("Create", declaration.Id, null, ToAuditPayload(obligation, declaration, period));

        await dbContext.SaveChangesAsync(cancellationToken);
        return new TaxDeclarationManualCreationResult(declaration.Id, true, null);
    }

    public async Task<TaxDeclarationManualCreationOptions> GetManualCreationOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var obligations = await dbContext.TaxObligations
            .AsNoTracking()
            .Where(obligation => obligation.IsActive)
            .OrderBy(obligation => obligation.Name)
            .Select(obligation => new LookupItemDto(obligation.Id, obligation.Name))
            .ToArrayAsync(cancellationToken);

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .Select(user => new LookupItemDto(user.Id, user.DisplayName + " <" + user.Email + ">"))
            .ToArrayAsync(cancellationToken);

        return new TaxDeclarationManualCreationOptions(obligations, users);
    }

    private TaxPeriod GetOrCreatePeriod(
        IDictionary<string, TaxPeriod> periods,
        TaxDeclarationPlanItem planItem)
    {
        var key = PeriodKey(planItem.PeriodType, planItem.Sequence);
        if (periods.TryGetValue(key, out var period))
        {
            return period;
        }

        period = planItem.PeriodType switch
        {
            "Weekly" => TaxPeriod.Weekly(GetFiscalYear(planItem), planItem.Sequence, planItem.StartDate, planItem.EndDate),
            _ => new TaxPeriod(
                planItem.StartDate.Year,
                planItem.Month,
                planItem.Quarter,
                planItem.StartDate,
                planItem.EndDate,
                planItem.PeriodLabel)
        };

        MarkCreatedByCurrentUser(period);
        dbContext.TaxPeriods.Add(period);
        periods.Add(key, period);
        return period;
    }

    private static int GetFiscalYear(TaxDeclarationPlanItem planItem)
    {
        return planItem.PeriodLabel.Length >= 4 &&
            int.TryParse(planItem.PeriodLabel.AsSpan(0, 4), out var fiscalYear)
            ? fiscalYear
            : planItem.StartDate.Year;
    }

    private async Task<int> GetNextManualSequenceAsync(int year, CancellationToken cancellationToken)
    {
        var maxSequence = await dbContext.TaxPeriods
            .Where(period => period.Year == year && period.PeriodType == "Manual")
            .MaxAsync(period => (int?)period.Sequence, cancellationToken);

        return (maxSequence ?? 0) + 1;
    }

    private static Guid FindAssignedUser(TaxObligation obligation)
    {
        return obligation.Responsibles.FirstOrDefault(responsible => responsible.Type == ResponsibleType.Preparer)?.UserId
            ?? obligation.Responsibles.FirstOrDefault(responsible => responsible.IsPrimary)?.UserId
            ?? Guid.Empty;
    }

    private static string PeriodKey(string periodType, int? sequence)
    {
        return $"{periodType}:{sequence.GetValueOrDefault()}";
    }

    private void MarkCreatedByCurrentUser(TaxPeriod period)
    {
        if (currentUserService.UserId.HasValue)
        {
            period.MarkCreatedBy(currentUserService.UserId.Value);
        }
    }

    private void MarkCreatedByCurrentUser(TaxDeclaration declaration)
    {
        if (currentUserService.UserId.HasValue)
        {
            declaration.MarkCreatedBy(currentUserService.UserId.Value);
        }
    }

    private static TaxDeclarationGenerationItem Skipped(
        TaxObligation obligation,
        string periodLabel,
        DateOnly dueDate,
        DateOnly? reminderDate,
        string reason)
    {
        return new TaxDeclarationGenerationItem(
            obligation.Id,
            obligation.Name,
            periodLabel,
            dueDate,
            reminderDate,
            false,
            reason);
    }

    private void AddAudit(string action, Guid entityId, object? oldValue, object? newValue)
    {
        auditService.Add(new AuditEntry(
            action,
            nameof(TaxDeclaration),
            entityId.ToString(),
            oldValue,
            newValue,
            AuditModule,
            "Web"));
    }

    private static object ToAuditPayload(TaxObligation obligation, TaxDeclaration declaration, TaxPeriod period)
    {
        return new
        {
            declaration.Id,
            declaration.TaxObligationId,
            ObligationName = obligation.Name,
            declaration.TaxPeriodId,
            Period = period.Label,
            period.PeriodType,
            declaration.DueDate,
            declaration.ReminderDate,
            declaration.Status,
            declaration.AssignedToUserId
        };
    }

    private sealed record DeclarationKey(Guid TaxObligationId, Guid TaxPeriodId);
}
