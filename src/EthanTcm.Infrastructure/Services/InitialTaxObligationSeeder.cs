using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Seed;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed partial class InitialTaxObligationSeeder(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService)
    : IInitialTaxObligationSeeder
{
    private const string AuditModule = "Initial Tax Obligation Seed";
    private const string LegalEntityCode = "ETHAN";
    private const string SeedUserLogin = "seed.system";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<InitialTaxObligationSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var errors = new List<string>();
        var now = DateTimeOffset.UtcNow;

        var legalEntity = await EnsureLegalEntityAsync(cancellationToken);
        var responsibleUser = await EnsureResponsibleUserAsync(cancellationToken);

        foreach (var item in InitialTaxObligationSeedData.Items)
        {
            try
            {
                var department = await EnsureDepartmentAsync(item.Department, cancellationToken);
                var category = await EnsureTaxCategoryAsync(item.TaxCategory, cancellationToken);
                var frequency = await EnsureTaxFrequencyAsync(item.Frequency, cancellationToken);
                var requiresReview = item.RequiresReview || item.Number is null || string.IsNullOrWhiteSpace(item.Frequency) || string.IsNullOrWhiteSpace(item.LegalDeadline);
                var reviewReason = BuildReviewReason(item, requiresReview);

                var obligation = await dbContext.TaxObligations
                    .AsSplitQuery()
                    .Include(current => current.Responsibles)
                    .Include(current => current.ScheduleRules)
                    .FirstOrDefaultAsync(
                        current =>
                            current.DepartmentId == department.Id &&
                            current.Name == item.ReportType &&
                            current.TaxCategoryId == category.Id,
                        cancellationToken);

                if (obligation is null)
                {
                    obligation = new TaxObligation(
                        legalEntity.Id,
                        department.Id,
                        category.Id,
                        frequency.Id,
                        responsibleUser.Id,
                        item.ReportType,
                        requiresReview ? RiskLevel.High : RiskLevel.Medium,
                        requiresPayment: false,
                        now);

                    obligation.UpdateSeedMetadata(item.Number, item.LegalDeadline, requiresReview, reviewReason, now);
                    obligation.ReplaceScheduleRules(BuildScheduleRules(item), now);
                    MarkCreatedByCurrentUser(obligation);
                    dbContext.TaxObligations.Add(obligation);
                    AddAudit("Create", obligation.Id, null, ToAuditPayload(item, obligation));
                    created++;
                }
                else
                {
                    var oldValue = ToAuditPayload(item, obligation);
                    obligation.UpdateDetails(
                        department.Id,
                        category.Id,
                        frequency.Id,
                        item.ReportType,
                        "Seeded from Tax - Matrix_202603.02.xlsx.",
                        requiresReview ? RiskLevel.High : obligation.RiskLevel,
                        obligation.RequiresPayment,
                        now);
                    obligation.UpdateSeedMetadata(item.Number, item.LegalDeadline, requiresReview, reviewReason, now);
                    dbContext.TaxScheduleRules.RemoveRange(obligation.ScheduleRules);
                    obligation.ReplaceScheduleRules(BuildScheduleRules(item), now);
                    MarkScheduleRulesAsAdded(obligation);
                    AddAudit("Update", obligation.Id, oldValue, ToAuditPayload(item, obligation));
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{item.ReportType}: {ex.Message}");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var requiresReviewCount = InitialTaxObligationSeedData.Items.Count(item =>
            item.RequiresReview ||
            item.Number is null ||
            string.IsNullOrWhiteSpace(item.Frequency) ||
            string.IsNullOrWhiteSpace(item.LegalDeadline));

        return new InitialTaxObligationSeedResult(created, updated, requiresReviewCount, errors);
    }

    private async Task<LegalEntity> EnsureLegalEntityAsync(CancellationToken cancellationToken)
    {
        var legalEntity = await dbContext.LegalEntities.FirstOrDefaultAsync(item => item.Code == LegalEntityCode, cancellationToken);
        if (legalEntity is not null)
        {
            return legalEntity;
        }

        legalEntity = new LegalEntity(LegalEntityCode, "ETHAN TCM", "CD", null);
        dbContext.LegalEntities.Add(legalEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return legalEntity;
    }

    private async Task<User> EnsureResponsibleUserAsync(CancellationToken cancellationToken)
    {
        if (currentUserService.UserId.HasValue)
        {
            var currentUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Id == currentUserService.UserId.Value, cancellationToken);
            if (currentUser is not null)
            {
                return currentUser;
            }
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Login == SeedUserLogin, cancellationToken);
        if (user is not null)
        {
            return user;
        }

        user = new User(SeedUserLogin, "Seed System User", "seed.system@local");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<Department> EnsureDepartmentAsync(string name, CancellationToken cancellationToken)
    {
        var code = BuildCode(name);
        var trackedDepartment = dbContext.Departments.Local.FirstOrDefault(item => item.Code == code || item.Name == name);
        if (trackedDepartment is not null)
        {
            return trackedDepartment;
        }

        var department = await dbContext.Departments.FirstOrDefaultAsync(item => item.Code == code || item.Name == name, cancellationToken);
        if (department is not null)
        {
            return department;
        }

        department = new Department(code, name);
        dbContext.Departments.Add(department);
        return department;
    }

    private async Task<TaxCategory> EnsureTaxCategoryAsync(string name, CancellationToken cancellationToken)
    {
        var code = BuildCode(name);
        var trackedCategory = dbContext.TaxCategories.Local.FirstOrDefault(item => item.Code == code || item.Name == name);
        if (trackedCategory is not null)
        {
            return trackedCategory;
        }

        var category = await dbContext.TaxCategories.FirstOrDefaultAsync(item => item.Code == code || item.Name == name, cancellationToken);
        if (category is not null)
        {
            return category;
        }

        category = new TaxCategory(code, name, "Seeded from Tax - Matrix_202603.02.xlsx.");
        dbContext.TaxCategories.Add(category);
        return category;
    }

    private async Task<TaxFrequency> EnsureTaxFrequencyAsync(string? name, CancellationToken cancellationToken)
    {
        var frequencyName = string.IsNullOrWhiteSpace(name) ? "Requires Review" : name.Trim();
        var code = BuildCode(frequencyName);
        var trackedFrequency = dbContext.TaxFrequencies.Local.FirstOrDefault(item => item.Code == code || item.Name == frequencyName);
        if (trackedFrequency is not null)
        {
            return trackedFrequency;
        }

        var frequency = await dbContext.TaxFrequencies.FirstOrDefaultAsync(item => item.Code == code || item.Name == frequencyName, cancellationToken);
        if (frequency is not null)
        {
            return frequency;
        }

        frequency = new TaxFrequency(code, frequencyName, OccurrencesPerYear(frequencyName));
        dbContext.TaxFrequencies.Add(frequency);
        return frequency;
    }

    private static IReadOnlyCollection<(int DueDay, int? DueMonth, bool MoveToNextBusinessDay, string? RawReminderText, string? ReminderDays, bool IsActive)> BuildScheduleRules(
        InitialTaxObligationSeedItem item)
    {
        return item.MonthlySchedule
            .Where(schedule => schedule.IsActive)
            .Select(schedule =>
            {
                var rawReminderText = schedule.RawReminderText;
                var reminderDays = ExtractReminderDays(rawReminderText ?? item.LegalDeadline);
                var dueDay = reminderDays.FirstOrDefault();
                if (dueDay == 0)
                {
                    dueDay = ExtractReminderDays(item.LegalDeadline).FirstOrDefault();
                }

                return (
                    DueDay: dueDay == 0 ? 1 : dueDay,
                    DueMonth: (int?)schedule.MonthNumber,
                    MoveToNextBusinessDay: true,
                    RawReminderText: rawReminderText,
                    ReminderDays: reminderDays.Count == 0 ? null : string.Join(",", reminderDays),
                    IsActive: schedule.IsActive);
            })
            .ToArray();
    }

    private static IReadOnlyCollection<int> ExtractReminderDays(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return NumberRegex()
            .Matches(text)
            .Select(match => int.Parse(match.Value))
            .Where(value => value is >= 1 and <= 31)
            .Distinct()
            .ToArray();
    }

    private static int OccurrencesPerYear(string frequency)
    {
        var normalized = Normalize(frequency);
        if (normalized.Contains("MONTHLY", StringComparison.Ordinal))
        {
            return 12;
        }

        if (normalized.Contains("FOUR", StringComparison.Ordinal))
        {
            return 4;
        }

        if (normalized.Contains("WEEKLY", StringComparison.Ordinal))
        {
            return 52;
        }

        return 1;
    }

    private static string? BuildReviewReason(InitialTaxObligationSeedItem item, bool requiresReview)
    {
        if (!requiresReview)
        {
            return null;
        }

        var reasons = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.ReviewReason))
        {
            reasons.Add(item.ReviewReason);
        }

        if (item.Number is null && reasons.All(reason => !reason.Contains("Number", StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add("Number missing in source matrix");
        }

        if (string.IsNullOrWhiteSpace(item.Frequency))
        {
            reasons.Add("Frequency missing in source matrix");
        }

        if (string.IsNullOrWhiteSpace(item.LegalDeadline))
        {
            reasons.Add("Legal deadline missing in source matrix");
        }

        return string.Join("; ", reasons.Distinct());
    }

    private void AddAudit(string action, Guid entityId, object? oldValue, object? newValue)
    {
        dbContext.AuditLogs.Add(new AuditLog(
            currentUserService.UserId,
            nameof(TaxObligation),
            entityId.ToString(),
            action,
            oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
            newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
            DateTimeOffset.UtcNow,
            module: AuditModule,
            source: "Seed"));
    }

    private void MarkCreatedByCurrentUser(TaxObligation obligation)
    {
        if (currentUserService.UserId.HasValue)
        {
            obligation.MarkCreatedBy(currentUserService.UserId.Value);
        }
    }

    private void MarkScheduleRulesAsAdded(TaxObligation obligation)
    {
        foreach (var scheduleRule in obligation.ScheduleRules)
        {
            dbContext.Entry(scheduleRule).State = EntityState.Added;
        }
    }

    private static object ToAuditPayload(InitialTaxObligationSeedItem item, TaxObligation obligation)
    {
        return new
        {
            obligation.Id,
            item.Number,
            item.Department,
            item.ReportType,
            item.TaxCategory,
            item.Frequency,
            item.LegalDeadline,
            item.RequiresReview,
            item.ReviewReason,
            ScheduleRules = item.MonthlySchedule.Count(schedule => schedule.IsActive)
        };
    }

    private static string BuildCode(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Length <= 42)
        {
            return normalized;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..7];
        return $"{normalized[..42]}_{hash}";
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.ToUpperInvariant())
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        var normalized = ConsecutiveUnderscoreRegex().Replace(builder.ToString(), "_").Trim('_');
        return string.IsNullOrWhiteSpace(normalized) ? "UNKNOWN" : normalized;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex("_+")]
    private static partial Regex ConsecutiveUnderscoreRegex();
}
