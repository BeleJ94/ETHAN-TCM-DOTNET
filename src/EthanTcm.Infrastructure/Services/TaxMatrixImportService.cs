using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class TaxMatrixImportService(
    EthanTcmDbContext dbContext,
    IAuditService auditService) : ITaxMatrixImportService, ITaxMatrixImporter
{
    private static readonly string[] MonthColumns =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];

    private static readonly string[] EmailColumns =
    [
        "Preparer",
        "Approver 1",
        "Approver 2",
        "Approver 3"
    ];

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public async Task<TaxMatrixImportResult> ImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        var systemUserId = await EnsureSystemUserAsync(cancellationToken);
        var preview = await PreviewAsync(fileStream, fileName, systemUserId, cancellationToken);
        var commit = await CommitAsync(preview.ImportBatchId, cancellationToken);

        return new TaxMatrixImportResult(
            preview.TotalRows,
            commit.Imported ? preview.ValidRows : 0,
            preview.InvalidRows);
    }

    public async Task<TaxMatrixPreviewResult> PreviewAsync(
        Stream fileStream,
        string fileName,
        Guid importedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only .xlsx tax matrix files are supported.");
        }

        var importedAt = DateTimeOffset.UtcNow;
        var batch = new ImportBatch(fileName, importedByUserId, importedAt);
        dbContext.ImportBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        Directory.CreateDirectory(GetStagingDirectory());
        var stagedFilePath = GetStagedFilePath(batch.Id);

        await using (var stagedFile = File.Create(stagedFilePath))
        {
            fileStream.Position = 0;
            await fileStream.CopyToAsync(stagedFile, cancellationToken);
        }

        var validation = ValidateWorkbook(stagedFilePath);

        var invalidRows = validation.Errors.Select(error => error.RowNumber).Distinct().Count();

        batch.RegisterValidationResult(
            validation.TotalRows,
            validation.TotalRows - invalidRows,
            invalidRows,
            DateTimeOffset.UtcNow);

        foreach (var error in validation.Errors)
        {
            batch.AddError(error.RowNumber, error.ColumnName, error.Message, DateTimeOffset.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaxMatrixPreviewResult(
            batch.Id,
            batch.FileName,
            validation.TotalRows,
            validation.TotalRows - invalidRows,
            validation.Errors.Count,
            validation.Errors.Count > 0,
            validation.Errors);
    }

    public async Task<TaxMatrixCommitResult> CommitAsync(
        Guid importBatchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.ImportBatches
            .Include(current => current.Errors)
            .FirstOrDefaultAsync(current => current.Id == importBatchId, cancellationToken);

        if (batch is null)
        {
            return new TaxMatrixCommitResult(importBatchId, 0, false, "Import batch not found.");
        }

        if (batch.Errors.Count > 0)
        {
            return new TaxMatrixCommitResult(importBatchId, 0, false, "The import contains critical validation errors.");
        }

        var stagedFilePath = GetStagedFilePath(importBatchId);
        if (!File.Exists(stagedFilePath))
        {
            return new TaxMatrixCommitResult(importBatchId, 0, false, "The staged Excel file is no longer available.");
        }

        var rows = ReadWorkbook(stagedFilePath);
        var imported = 0;
        var now = DateTimeOffset.UtcNow;
        var legalEntity = await EnsureDefaultLegalEntityAsync(cancellationToken);
        var roleIds = await GetRoleIdsAsync(cancellationToken);

        foreach (var row in rows)
        {
            var department = await EnsureDepartmentAsync(row.Department, cancellationToken);
            var taxCategory = await EnsureTaxCategoryAsync(row.TaxCategory, cancellationToken);
            var frequency = await EnsureFrequencyAsync(row.Frequency, cancellationToken);
            var preparer = await EnsureUserAsync(row.PreparerEmail, department.Id, roleIds.PreparerRoleId, cancellationToken);

            var obligationName = $"{row.ReportType} - {row.TaxCategory} - {row.Department}";
            var exists = await dbContext.TaxObligations.AnyAsync(
                obligation => obligation.LegalEntityId == legalEntity.Id && obligation.Name == obligationName,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            var obligation = new TaxObligation(
                legalEntity.Id,
                department.Id,
                taxCategory.Id,
                frequency.Id,
                preparer.Id,
                obligationName,
                RiskLevel.Medium,
                row.RequiresPayment,
                now);

            foreach (var approverEmail in row.ApproverEmails)
            {
                var approver = await EnsureUserAsync(approverEmail, department.Id, roleIds.ApproverRoleId, cancellationToken);
                obligation.AddResponsible(approver.Id, ResponsibleType.Approver, now);
            }

            foreach (var month in row.ReminderMonths)
            {
                obligation.AddScheduleRule(row.DeadlineDay, month, moveToNextBusinessDay: true, now);
            }

            if (row.ReminderMonths.Count == 0)
            {
                obligation.AddScheduleRule(row.DeadlineDay, dueMonth: null, moveToNextBusinessDay: true, now);
            }

            dbContext.TaxObligations.Add(obligation);
            imported++;
        }

        batch.MarkImported(DateTimeOffset.UtcNow);
        auditService.Add(new AuditEntry(
            "ImportExcel",
            nameof(ImportBatch),
            batch.Id.ToString(),
            null,
            new
            {
                batch.FileName,
                ImportedRows = imported,
                batch.TotalRows,
                batch.ValidRows,
                batch.InvalidRows,
                batch.Status
            },
            "Tax Matrix Import",
            "Web"));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaxMatrixCommitResult(importBatchId, imported, true, null);
    }

    private static TaxMatrixValidationResult ValidateWorkbook(string filePath)
    {
        var rows = ReadWorkbook(filePath);
        var errors = new List<TaxMatrixImportErrorDto>();
        var seenEmailsByRow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            AddRequiredError(errors, row.RowNumber, "Number", row.Number, "Number is missing.");
            AddRequiredError(errors, row.RowNumber, "Department", row.Department, "Department is missing.");
            AddRequiredError(errors, row.RowNumber, "Report Type", row.ReportType, "Report type is missing.");
            AddRequiredError(errors, row.RowNumber, "Tax Category", row.TaxCategory, "Tax category is missing.");
            AddRequiredError(errors, row.RowNumber, "Frequency", row.Frequency, "Frequency is missing.");
            AddRequiredError(errors, row.RowNumber, "Legal deadline", row.LegalDeadline, "Legal deadline is missing.");
            AddRequiredError(errors, row.RowNumber, "Preparer", row.PreparerEmail, "Responsible preparer is missing.");

            if (!string.IsNullOrWhiteSpace(row.LegalDeadline) && !TryParseDeadlineDay(row.LegalDeadline, out _))
            {
                errors.Add(new TaxMatrixImportErrorDto(row.RowNumber, "Legal deadline", "Legal deadline must contain a valid day number between 1 and 31."));
            }

            seenEmailsByRow.Clear();
            foreach (var emailValue in row.EmailValues)
            {
                if (string.IsNullOrWhiteSpace(emailValue.Value))
                {
                    continue;
                }

                if (!EmailRegex.IsMatch(emailValue.Value))
                {
                    errors.Add(new TaxMatrixImportErrorDto(row.RowNumber, emailValue.ColumnName, $"Invalid e-mail '{emailValue.Value}'."));
                }

                if (!seenEmailsByRow.Add(emailValue.Value))
                {
                    errors.Add(new TaxMatrixImportErrorDto(row.RowNumber, emailValue.ColumnName, $"Duplicated e-mail '{emailValue.Value}' on this row."));
                }
            }

            if (!string.IsNullOrWhiteSpace(row.Frequency) && GetOccurrencesPerYear(row.Frequency) <= 0)
            {
                errors.Add(new TaxMatrixImportErrorDto(row.RowNumber, "Frequency", $"Unsupported frequency '{row.Frequency}'."));
            }
        }

        return new TaxMatrixValidationResult(rows.Count, errors);
    }

    private static List<TaxMatrixRow> ReadWorkbook(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return [];
        }

        var headerRow = usedRange.FirstRowUsed();
        var headers = headerRow.CellsUsed()
            .ToDictionary(cell => NormalizeHeader(cell.GetString()), cell => cell.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

        var rows = new List<TaxMatrixRow>();
        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            if (row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString())))
            {
                continue;
            }

            var legalDeadline = GetCell(row, headers, "Legal deadline");
            var reminderMonths = MonthColumns
                .Select((month, index) => (Month: index + 1, Value: GetCell(row, headers, month)))
                .Where(month => IsMarked(month.Value))
                .Select(month => month.Month)
                .ToArray();

            rows.Add(new TaxMatrixRow(
                row.RowNumber(),
                GetCell(row, headers, "Number", "No", "No.", "#", "Numero", "Numéro"),
                GetCell(row, headers, "Department"),
                GetCell(row, headers, "Report Type"),
                GetCell(row, headers, "Tax Category"),
                GetCell(row, headers, "Frequency"),
                legalDeadline,
                TryParseDeadlineDay(legalDeadline, out var deadlineDay) ? deadlineDay : 0,
                reminderMonths,
                GetCell(row, headers, "Preparer"),
                GetCell(row, headers, "Approver 1"),
                GetCell(row, headers, "Approver 2"),
                GetCell(row, headers, "Approver 3"),
                IsPaymentRequired(GetCell(row, headers, "Payment Process")),
                GetCell(row, headers, "Submission Process"),
                GetCell(row, headers, "Follow-up Process")));
        }

        return rows;
    }

    private async Task<Department> EnsureDepartmentAsync(string name, CancellationToken cancellationToken)
    {
        var code = ToCode(name);
        var department = await dbContext.Departments.FirstOrDefaultAsync(current => current.Code == code, cancellationToken);
        if (department is not null)
        {
            return department;
        }

        department = new Department(code, name.Trim());
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);
        return department;
    }

    private async Task<TaxCategory> EnsureTaxCategoryAsync(string name, CancellationToken cancellationToken)
    {
        var code = ToCode(name);
        var category = await dbContext.TaxCategories.FirstOrDefaultAsync(current => current.Code == code, cancellationToken);
        if (category is not null)
        {
            return category;
        }

        category = new TaxCategory(code, name.Trim());
        dbContext.TaxCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    private async Task<TaxFrequency> EnsureFrequencyAsync(string name, CancellationToken cancellationToken)
    {
        var code = ToCode(name);
        var frequency = await dbContext.TaxFrequencies.FirstOrDefaultAsync(current => current.Code == code, cancellationToken);
        if (frequency is not null)
        {
            return frequency;
        }

        frequency = new TaxFrequency(code, name.Trim(), GetOccurrencesPerYear(name));
        dbContext.TaxFrequencies.Add(frequency);
        await dbContext.SaveChangesAsync(cancellationToken);
        return frequency;
    }

    private async Task<LegalEntity> EnsureDefaultLegalEntityAsync(CancellationToken cancellationToken)
    {
        const string code = "ETHAN";
        var legalEntity = await dbContext.LegalEntities.FirstOrDefaultAsync(current => current.Code == code, cancellationToken);
        if (legalEntity is not null)
        {
            return legalEntity;
        }

        legalEntity = new LegalEntity(code, "ETHAN TCM", "MA", null);
        dbContext.LegalEntities.Add(legalEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return legalEntity;
    }

    private async Task<User> EnsureUserAsync(string email, Guid? departmentId, Guid roleId, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(current => current.Roles)
            .FirstOrDefaultAsync(current => current.Email == normalizedEmail || current.Login == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User(normalizedEmail, ToDisplayNameFromEmail(normalizedEmail), normalizedEmail, departmentId);
            dbContext.Users.Add(user);
        }

        user.AssignRole(roleId, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<Guid> EnsureSystemUserAsync(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(current => current.Login == "system", cancellationToken);
        if (user is not null)
        {
            return user.Id;
        }

        user = new User("system", "ETHAN TCM System", "system@local");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    private async Task<(Guid PreparerRoleId, Guid ApproverRoleId)> GetRoleIdsAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.ToDictionaryAsync(role => role.Code, role => role.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        return (roles["Preparer"], roles["Approver"]);
    }

    private static void AddRequiredError(List<TaxMatrixImportErrorDto> errors, int rowNumber, string columnName, string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new TaxMatrixImportErrorDto(rowNumber, columnName, message));
        }
    }

    private static string GetCell(IXLRangeRow row, Dictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(NormalizeHeader(name), out var columnNumber))
            {
                return row.Cell(columnNumber).GetFormattedString().Trim();
            }
        }

        return string.Empty;
    }

    private static bool TryParseDeadlineDay(string value, out int day)
    {
        day = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = Regex.Match(value, @"\d{1,2}");
        return digits.Success && int.TryParse(digits.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out day) && day is >= 1 and <= 31;
    }

    private static int GetOccurrencesPerYear(string frequency)
    {
        var normalized = NormalizeHeader(frequency);
        return normalized switch
        {
            "monthly" or "month" or "mensuel" or "mensuelle" => 12,
            "quarterly" or "quarter" or "trimestriel" or "trimestrielle" => 4,
            "semiannual" or "semiannually" or "halfyearly" or "semestriel" or "semestrielle" => 2,
            "annual" or "annually" or "yearly" or "annuel" or "annuelle" => 1,
            "weekly" or "hebdomadaire" => 52,
            _ => 0
        };
    }

    private static bool IsMarked(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeHeader(value);
        return normalized is not ("no" or "n" or "false" or "0" or "na" or "n/a" or "non");
    }

    private static bool IsPaymentRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return IsMarked(value);
    }

    private static string NormalizeHeader(string value)
    {
        return value.Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static string ToCode(string value)
    {
        var cleaned = Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(cleaned) ? "UNKNOWN" : cleaned;
    }

    private static string ToDisplayNameFromEmail(string email)
    {
        var localPart = email.Split('@')[0];
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(localPart.Replace('.', ' ').Replace('_', ' '));
    }

    private static string GetStagingDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "EthanTcm", "TaxMatrixImports");
    }

    private static string GetStagedFilePath(Guid importBatchId)
    {
        return Path.Combine(GetStagingDirectory(), $"{importBatchId:N}.xlsx");
    }

    private sealed record TaxMatrixValidationResult(int TotalRows, IReadOnlyCollection<TaxMatrixImportErrorDto> Errors);

    private sealed record TaxMatrixRow(
        int RowNumber,
        string Number,
        string Department,
        string ReportType,
        string TaxCategory,
        string Frequency,
        string LegalDeadline,
        int DeadlineDay,
        IReadOnlyCollection<int> ReminderMonths,
        string PreparerEmail,
        string Approver1Email,
        string Approver2Email,
        string Approver3Email,
        bool RequiresPayment,
        string SubmissionProcess,
        string FollowUpProcess)
    {
        public IReadOnlyCollection<string> ApproverEmails =>
            new[] { Approver1Email, Approver2Email, Approver3Email }
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim().ToLowerInvariant())
                .ToArray();

        public IReadOnlyCollection<(string ColumnName, string Value)> EmailValues =>
            new[]
            {
                ("Preparer", PreparerEmail),
                ("Approver 1", Approver1Email),
                ("Approver 2", Approver2Email),
                ("Approver 3", Approver3Email)
            };
    }
}
