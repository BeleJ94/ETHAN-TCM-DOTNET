using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EthanTcm.Infrastructure.Services;

public sealed class TaxObligationReferentialService(
    EthanTcmDbContext dbContext,
    IAuditService auditService)
    : ITaxObligationReferentialService
{
    private const string AuditModule = "Tax Referential";

    public async Task<TaxObligationListResult> SearchAsync(
        TaxObligationSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var options = await GetEditOptionsAsync(cancellationToken);
        var query = dbContext.TaxObligations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(obligation => obligation.Name.Contains(search));
        }

        if (criteria.DepartmentId.HasValue)
        {
            query = query.Where(obligation => obligation.DepartmentId == criteria.DepartmentId.Value);
        }

        if (criteria.TaxCategoryId.HasValue)
        {
            query = query.Where(obligation => obligation.TaxCategoryId == criteria.TaxCategoryId.Value);
        }

        if (criteria.TaxFrequencyId.HasValue)
        {
            query = query.Where(obligation => obligation.TaxFrequencyId == criteria.TaxFrequencyId.Value);
        }

        if (criteria.IsActive.HasValue)
        {
            query = query.Where(obligation => obligation.IsActive == criteria.IsActive.Value);
        }

        var obligations = await query
            .OrderBy(obligation => obligation.Name)
            .Take(500)
            .ToListAsync(cancellationToken);

        var departments = options.Departments.ToDictionary(item => item.Id, item => item.Label);
        var categories = options.TaxCategories.ToDictionary(item => item.Id, item => item.Label);
        var frequencies = options.TaxFrequencies.ToDictionary(item => item.Id, item => item.Label);

        var items = obligations.Select(obligation => new TaxObligationListItemDto(
                obligation.Id,
                obligation.Name,
                departments.GetValueOrDefault(obligation.DepartmentId, "-"),
                categories.GetValueOrDefault(obligation.TaxCategoryId, "-"),
                frequencies.GetValueOrDefault(obligation.TaxFrequencyId, "-"),
                obligation.RequiresPayment,
                obligation.IsActive))
            .ToArray();

        return new TaxObligationListResult(items, options);
    }

    public async Task<TaxObligationDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var obligation = await dbContext.TaxObligations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(current => current.Responsibles)
            .Include(current => current.ScheduleRules)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (obligation is null)
        {
            return null;
        }

        return await ToDetailsAsync(obligation, cancellationToken);
    }

    public async Task<TaxObligationEditOptions> GetEditOptionsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await dbContext.Departments
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Name))
            .ToArrayAsync(cancellationToken);

        var categories = await dbContext.TaxCategories
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Name))
            .ToArrayAsync(cancellationToken);

        var frequencies = await dbContext.TaxFrequencies
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Name))
            .ToArrayAsync(cancellationToken);

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayName)
            .Select(item => new LookupItemDto(item.Id, item.DisplayName + " <" + item.Email + ">"))
            .ToArrayAsync(cancellationToken);

        return new TaxObligationEditOptions(departments, categories, frequencies, users);
    }

    public async Task<Guid> CreateAsync(TaxObligationUpsertCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateCommandAsync(command, existingId: null, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var legalEntity = await EnsureDefaultLegalEntityAsync(cancellationToken);
        var obligation = new TaxObligation(
            legalEntity.Id,
            command.DepartmentId,
            command.TaxCategoryId,
            command.TaxFrequencyId,
            command.PreparerUserId,
            command.Name.Trim(),
            command.RiskLevel,
            command.RequiresPayment,
            now);

        obligation.UpdateDetails(
            command.DepartmentId,
            command.TaxCategoryId,
            command.TaxFrequencyId,
            command.Name.Trim(),
            NormalizeOptional(command.Description),
            command.RiskLevel,
            command.RequiresPayment,
            now);
        var newResponsibles = BuildResponsibles(command);
        obligation.ReplaceResponsibles(newResponsibles, now);
        obligation.EnsureReadyForUse();

        dbContext.TaxObligations.Add(obligation);
        AddAudit("Create", obligation.Id, oldValue: null, newValue: await ToAuditPayloadAsync(obligation, cancellationToken));
        AddAudit("ChangeResponsible", obligation.Id, oldValue: null, newValue: newResponsibles);
        await dbContext.SaveChangesAsync(cancellationToken);

        return obligation.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, TaxObligationUpsertCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            return await UpdateOnceAsync(id, command, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            try
            {
                return await UpdateOnceAsync(id, command, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("The tax obligation was modified by another process. Reload the page and try again.", ex);
            }
        }
    }

    private async Task<bool> UpdateOnceAsync(Guid id, TaxObligationUpsertCommand command, CancellationToken cancellationToken)
    {
        var obligation = await dbContext.TaxObligations
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (obligation is null)
        {
            return false;
        }

        await ValidateCommandAsync(command, id, cancellationToken);

        var oldValue = await ToAuditPayloadByIdAsync(id, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var oldResponsibles = await dbContext.TaxObligationResponsibles
            .AsNoTracking()
            .Where(item => item.TaxObligationId == id)
            .Select(item => new { item.UserId, item.Type })
            .OrderBy(item => item.Type)
            .ThenBy(item => item.UserId)
            .ToArrayAsync(cancellationToken);

        obligation.UpdateDetails(
            command.DepartmentId,
            command.TaxCategoryId,
            command.TaxFrequencyId,
            command.Name.Trim(),
            NormalizeOptional(command.Description),
            command.RiskLevel,
            command.RequiresPayment,
            now);
        var newResponsibles = BuildResponsibles(command);
        await ReplaceResponsiblesAsync(id, newResponsibles, now, cancellationToken);

        AddAudit("Update", obligation.Id, oldValue, new
        {
            obligation.Id,
            command.Name,
            command.Description,
            command.DepartmentId,
            command.TaxCategoryId,
            command.TaxFrequencyId,
            command.RiskLevel,
            command.RequiresPayment,
            Responsibles = newResponsibles
        });
        var updatedResponsibles = newResponsibles
            .Select(item => new { item.UserId, item.Type })
            .OrderBy(item => item.Type)
            .ThenBy(item => item.UserId)
            .ToArray();
        if (!oldResponsibles.SequenceEqual(updatedResponsibles))
        {
            AddAudit("ChangeResponsible", obligation.Id, oldResponsibles, updatedResponsibles);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ReplaceResponsiblesAsync(
        Guid taxObligationId,
        IReadOnlyCollection<(Guid UserId, ResponsibleType Type)> responsibles,
        DateTimeOffset assignedAt,
        CancellationToken cancellationToken)
    {
        await dbContext.TaxObligationResponsibles
            .Where(responsible => responsible.TaxObligationId == taxObligationId)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.TaxObligationResponsibles.AddRange(responsibles.Select(responsible =>
            new TaxObligationResponsible(taxObligationId, responsible.UserId, responsible.Type, assignedAt)));
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var obligation = await dbContext.TaxObligations
            .Include(current => current.Responsibles)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (obligation is null)
        {
            return false;
        }

        var oldValue = await ToAuditPayloadAsync(obligation, cancellationToken);
        obligation.Deactivate(DateTimeOffset.UtcNow);

        AddAudit("Deactivate", obligation.Id, oldValue, await ToAuditPayloadAsync(obligation, cancellationToken));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The tax obligation was modified by another process. Reload the page and try again.", ex);
        }
        return true;
    }

    public async Task<TaxObligationVersionCreationContext?> GetVersionCreationContextAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var obligation = await dbContext.TaxObligations
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.Id, item.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (obligation is null)
        {
            return null;
        }

        var latest = await dbContext.TaxObligationVersions
            .AsNoTracking()
            .Where(item => item.TaxObligationId == id)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var authorities = await dbContext.TaxAuthorities
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Name))
            .ToArrayAsync(cancellationToken);

        var minimumEffectiveFrom = latest is null
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : latest.EffectiveFrom.AddDays(1);

        string[] rateRules = [];
        string[] legalReferences = [];
        string[] penaltyRules = [];
        string[] processTemplates = [];
        if (latest is not null)
        {
            rateRules = await dbContext.TaxRateRules.AsNoTracking()
                .Where(item => item.TaxObligationVersionId == latest.Id)
                .OrderBy(item => item.SourceRow)
                .Select(item => item.RuleText)
                .ToArrayAsync(cancellationToken);
            legalReferences = await dbContext.TaxLegalReferences.AsNoTracking()
                .Where(item => item.TaxObligationVersionId == latest.Id)
                .OrderBy(item => item.SourceRow)
                .Select(item => item.ReferenceText)
                .ToArrayAsync(cancellationToken);
            penaltyRules = await dbContext.TaxPenaltyRules.AsNoTracking()
                .Where(item => item.TaxObligationVersionId == latest.Id)
                .OrderBy(item => item.SourceRow)
                .Select(item => item.RuleText)
                .ToArrayAsync(cancellationToken);
            processTemplates = await dbContext.TaxProcessTemplates.AsNoTracking()
                .Where(item => item.TaxObligationVersionId == latest.Id)
                .OrderBy(item => item.SourceRow)
                .Select(item => item.ProcessText)
                .ToArrayAsync(cancellationToken);
        }

        return new TaxObligationVersionCreationContext(
            obligation.Id,
            obligation.Name,
            (latest?.VersionNumber ?? 0) + 1,
            minimumEffectiveFrom,
            latest?.TaxAuthorityId,
            latest?.Frequency,
            latest?.FilingDeadlineRule,
            latest?.PaymentDeadlineRule,
            latest?.RawDeadlineText,
            latest?.BusinessCycle,
            latest?.ValidationStatus ?? TaxCatalogValidationStatus.Draft,
            latest?.RequiresReview ?? true,
            rateRules,
            legalReferences,
            penaltyRules,
            processTemplates,
            authorities);
    }

    public async Task<Guid?> CreateVersionAsync(
        Guid id,
        TaxObligationVersionCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        var obligation = await dbContext.TaxObligations
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (obligation is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(command.ChangeReason))
        {
            throw new InvalidOperationException("Le motif de la réforme est obligatoire.");
        }

        if (command.TaxAuthorityId.HasValue &&
            !await dbContext.TaxAuthorities.AnyAsync(
                item => item.Id == command.TaxAuthorityId.Value,
                cancellationToken))
        {
            throw new InvalidOperationException("L’autorité fiscale sélectionnée est invalide.");
        }

        var latest = await dbContext.TaxObligationVersions
            .Where(item => item.TaxObligationId == id)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null && command.EffectiveFrom <= latest.EffectiveFrom)
        {
            throw new InvalidOperationException(
                $"La date d’effet doit être postérieure au {latest.EffectiveFrom:dd/MM/yyyy}.");
        }

        var rateRules = NormalizeRules(command.RateRules, "taux ou base taxable");
        var legalReferences = NormalizeRules(command.LegalReferences, "référence légale");
        var penaltyRules = NormalizeRules(command.PenaltyRules, "pénalité");
        var processTemplates = NormalizeRules(command.ProcessTemplates, "étape de processus");
        var nextVersionNumber = (latest?.VersionNumber ?? 0) + 1;

        if (latest is not null &&
            (!latest.EffectiveTo.HasValue || latest.EffectiveTo.Value >= command.EffectiveFrom))
        {
            latest.Close(command.EffectiveFrom.AddDays(-1));
        }

        var normalizedPayload = new
        {
            command.EffectiveFrom,
            command.TaxAuthorityId,
            Frequency = NormalizeOptional(command.Frequency),
            FilingDeadlineRule = NormalizeOptional(command.FilingDeadlineRule),
            PaymentDeadlineRule = NormalizeOptional(command.PaymentDeadlineRule),
            RawDeadlineText = NormalizeOptional(command.RawDeadlineText),
            BusinessCycle = NormalizeOptional(command.BusinessCycle),
            command.ValidationStatus,
            command.RequiresReview,
            RateRules = rateRules,
            LegalReferences = legalReferences,
            PenaltyRules = penaltyRules,
            ProcessTemplates = processTemplates
        };
        var dataHash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalizedPayload))));

        var version = new TaxObligationVersion(
            id,
            nextVersionNumber,
            command.EffectiveFrom,
            normalizedPayload.Frequency,
            normalizedPayload.FilingDeadlineRule,
            normalizedPayload.PaymentDeadlineRule,
            normalizedPayload.RawDeadlineText,
            normalizedPayload.BusinessCycle,
            command.ValidationStatus,
            command.RequiresReview,
            command.ChangeReason.Trim(),
            dataHash,
            command.TaxAuthorityId);

        obligation.UpdateCatalogMetadata(
            obligation.ComplianceNotes,
            command.ValidationStatus,
            command.RequiresReview,
            command.RequiresReview ? command.ChangeReason : null,
            DateTimeOffset.UtcNow);

        const string sourceName = "Manual fiscal reform";
        dbContext.TaxObligationVersions.Add(version);
        AddVersionRules(version.Id, nextVersionNumber, sourceName, rateRules, legalReferences, penaltyRules, processTemplates);

        AddAudit(
            "CreateVersion",
            id,
            latest is null
                ? null
                : new { latest.Id, latest.VersionNumber, latest.EffectiveFrom, latest.EffectiveTo },
            new
            {
                version.Id,
                version.VersionNumber,
                version.EffectiveFrom,
                version.TaxAuthorityId,
                version.Frequency,
                version.FilingDeadlineRule,
                version.PaymentDeadlineRule,
                version.RawDeadlineText,
                version.BusinessCycle,
                version.ValidationStatus,
                version.RequiresReview,
                version.ChangeReason,
                RateRules = rateRules,
                LegalReferences = legalReferences,
                PenaltyRules = penaltyRules,
                ProcessTemplates = processTemplates
            });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Le référentiel a été modifié par un autre utilisateur. Rechargez la page puis réessayez.",
                ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                "La nouvelle version fiscale n’a pas pu être enregistrée. Rechargez la page puis réessayez.",
                ex);
        }

        return version.Id;
    }

    private void AddVersionRules(
        Guid versionId,
        int versionNumber,
        string sourceName,
        IReadOnlyCollection<string> rateRules,
        IReadOnlyCollection<string> legalReferences,
        IReadOnlyCollection<string> penaltyRules,
        IReadOnlyCollection<string> processTemplates)
    {
        dbContext.TaxRateRules.AddRange(rateRules.Select((text, index) =>
            new TaxRateRule(versionId, text, sourceName, $"Manual:v{versionNumber}:R:{index + 1}")));
        dbContext.TaxLegalReferences.AddRange(legalReferences.Select((text, index) =>
            new TaxLegalReference(versionId, text, sourceName, $"Manual:v{versionNumber}:L:{index + 1}")));
        dbContext.TaxPenaltyRules.AddRange(penaltyRules.Select((text, index) =>
            new TaxPenaltyRule(versionId, text, sourceName, $"Manual:v{versionNumber}:P:{index + 1}")));
        dbContext.TaxProcessTemplates.AddRange(processTemplates.Select((text, index) =>
            new TaxProcessTemplate(versionId, text, sourceName, $"Manual:v{versionNumber}:T:{index + 1}")));
    }

    private static string[] NormalizeRules(IEnumerable<string>? values, string label)
    {
        var rules = (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rules.Any(value => value.Length > 4000))
        {
            throw new InvalidOperationException($"Chaque {label} doit contenir au maximum 4 000 caractères.");
        }

        return rules;
    }

    private async Task ValidateCommandAsync(TaxObligationUpsertCommand command, Guid? existingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        await EnsureExistsAsync(dbContext.Departments, command.DepartmentId, "Department", cancellationToken);
        await EnsureExistsAsync(dbContext.TaxCategories, command.TaxCategoryId, "Tax category", cancellationToken);
        await EnsureExistsAsync(dbContext.TaxFrequencies, command.TaxFrequencyId, "Frequency", cancellationToken);

        if (command.PreparerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Preparer is required.");
        }

        await EnsureExistsAsync(dbContext.Users, command.PreparerUserId, "Preparer", cancellationToken);
        await EnsureOptionalUserExistsAsync(command.Approver1UserId, "Approver 1", cancellationToken);
        await EnsureOptionalUserExistsAsync(command.Approver2UserId, "Approver 2", cancellationToken);
        await EnsureOptionalUserExistsAsync(command.Approver3UserId, "Approver 3", cancellationToken);
        await EnsureOptionalUserExistsAsync(command.PaymentProcessOwnerUserId, "Payment process owner", cancellationToken);
        await EnsureOptionalUserExistsAsync(command.SubmissionProcessOwnerUserId, "Submission process owner", cancellationToken);
        await EnsureOptionalUserExistsAsync(command.FollowUpOwnerUserId, "Follow-up owner", cancellationToken);

        var legalEntity = await EnsureDefaultLegalEntityAsync(cancellationToken);
        var duplicateExists = await dbContext.TaxObligations.AnyAsync(
            obligation =>
                obligation.LegalEntityId == legalEntity.Id &&
                obligation.Name == command.Name.Trim() &&
                (!existingId.HasValue || obligation.Id != existingId.Value),
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("A tax obligation with the same name already exists.");
        }
    }

    private static async Task EnsureExistsAsync<TEntity>(
        DbSet<TEntity> dbSet,
        Guid id,
        string label,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (id == Guid.Empty || await dbSet.FindAsync([id], cancellationToken) is null)
        {
            throw new InvalidOperationException($"{label} is invalid.");
        }
    }

    private async Task EnsureOptionalUserExistsAsync(Guid? userId, string label, CancellationToken cancellationToken)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return;
        }

        await EnsureExistsAsync(dbContext.Users, userId.Value, label, cancellationToken);
    }

    private static IReadOnlyCollection<(Guid UserId, ResponsibleType Type)> BuildResponsibles(TaxObligationUpsertCommand command)
    {
        var responsibles = new List<(Guid UserId, ResponsibleType Type)>
        {
            (command.PreparerUserId, ResponsibleType.Preparer)
        };

        if (IsSelected(command.Approver1UserId))
        {
            responsibles.Add((command.Approver1UserId!.Value, ResponsibleType.Approver1));
        }

        if (IsSelected(command.Approver2UserId))
        {
            responsibles.Add((command.Approver2UserId!.Value, ResponsibleType.Approver2));
        }

        if (IsSelected(command.Approver3UserId))
        {
            responsibles.Add((command.Approver3UserId!.Value, ResponsibleType.Approver3));
        }

        if (IsSelected(command.PaymentProcessOwnerUserId))
        {
            responsibles.Add((command.PaymentProcessOwnerUserId!.Value, ResponsibleType.PaymentProcessOwner));
        }

        if (IsSelected(command.SubmissionProcessOwnerUserId))
        {
            responsibles.Add((command.SubmissionProcessOwnerUserId!.Value, ResponsibleType.SubmissionProcessOwner));
        }

        if (IsSelected(command.FollowUpOwnerUserId))
        {
            responsibles.Add((command.FollowUpOwnerUserId!.Value, ResponsibleType.FollowUpOwner));
        }

        return responsibles;
    }

    private static bool IsSelected(Guid? userId)
    {
        return userId.HasValue && userId.Value != Guid.Empty;
    }

    private async Task<TaxObligationDetailsDto> ToDetailsAsync(TaxObligation obligation, CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments.AsNoTracking().FirstAsync(item => item.Id == obligation.DepartmentId, cancellationToken);
        var category = await dbContext.TaxCategories.AsNoTracking().FirstAsync(item => item.Id == obligation.TaxCategoryId, cancellationToken);
        var frequency = await dbContext.TaxFrequencies.AsNoTracking().FirstAsync(item => item.Id == obligation.TaxFrequencyId, cancellationToken);
        var userIds = obligation.Responsibles.Select(item => item.UserId).Distinct().ToArray();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var versions = await dbContext.TaxObligationVersions
            .AsNoTracking()
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderByDescending(item => item.VersionNumber)
            .ToArrayAsync(cancellationToken);
        var versionIds = versions.Select(item => item.Id).ToArray();
        var authorityIds = versions.Where(item => item.TaxAuthorityId.HasValue)
            .Select(item => item.TaxAuthorityId!.Value).Distinct().ToArray();
        var authorities = await dbContext.TaxAuthorities.AsNoTracking()
            .Where(item => authorityIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var rateRules = await dbContext.TaxRateRules.AsNoTracking()
            .Where(item => versionIds.Contains(item.TaxObligationVersionId))
            .ToArrayAsync(cancellationToken);
        var legalReferences = await dbContext.TaxLegalReferences.AsNoTracking()
            .Where(item => versionIds.Contains(item.TaxObligationVersionId))
            .ToArrayAsync(cancellationToken);
        var penaltyRules = await dbContext.TaxPenaltyRules.AsNoTracking()
            .Where(item => versionIds.Contains(item.TaxObligationVersionId))
            .ToArrayAsync(cancellationToken);
        var processTemplates = await dbContext.TaxProcessTemplates.AsNoTracking()
            .Where(item => versionIds.Contains(item.TaxObligationVersionId))
            .ToArrayAsync(cancellationToken);
        var requiredDocuments = await dbContext.TaxRequiredDocuments.AsNoTracking()
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderByDescending(item => item.IsRequired)
            .ThenBy(item => item.DocumentType)
            .ToArrayAsync(cancellationToken);
        var aliases = await dbContext.TaxObligationAliases.AsNoTracking()
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderBy(item => item.SourceName).ThenBy(item => item.SourceRow)
            .ToArrayAsync(cancellationToken);
        var sourceReferences = await dbContext.TaxSourceReferences.AsNoTracking()
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderBy(item => item.SourceName).ThenBy(item => item.SourceRow)
            .ToArrayAsync(cancellationToken);
        var conflicts = string.IsNullOrWhiteSpace(obligation.CanonicalCode)
            ? []
            : await dbContext.TaxCatalogConflicts.AsNoTracking()
                .Where(item => item.CanonicalCode == obligation.CanonicalCode)
                .OrderBy(item => item.Status).ThenBy(item => item.FieldName)
                .ToArrayAsync(cancellationToken);
        var allocations = await dbContext.TaxAllocationRules.AsNoTracking()
            .Where(item => item.TaxObligationId == obligation.Id)
            .OrderByDescending(item => item.Percentage)
            .ToArrayAsync(cancellationToken);
        var declarations = dbContext.TaxDeclarations.AsNoTracking()
            .Where(item => item.TaxObligationId == obligation.Id);
        var declarationCount = await declarations.CountAsync(cancellationToken);
        var openDeclarationCount = await declarations.CountAsync(
            item => item.Status != TaxDeclarationStatus.Closed &&
                    item.Status != TaxDeclarationStatus.Cancelled &&
                    item.Status != TaxDeclarationStatus.NotApplicable,
            cancellationToken);

        return new TaxObligationDetailsDto
        {
            Id = obligation.Id,
            Name = obligation.Name,
            CanonicalCode = obligation.CanonicalCode,
            Description = obligation.Description,
            ComplianceNotes = obligation.ComplianceNotes,
            SourceNumber = obligation.SourceNumber,
            LegalDeadline = obligation.LegalDeadline,
            RequiresReview = obligation.RequiresReview,
            ReviewReason = obligation.ReviewReason,
            CatalogValidationStatus = obligation.CatalogValidationStatus,
            DepartmentId = obligation.DepartmentId,
            Department = department.Name,
            TaxCategoryId = obligation.TaxCategoryId,
            TaxCategory = category.Name,
            TaxFrequencyId = obligation.TaxFrequencyId,
            Frequency = frequency.Name,
            RiskLevel = obligation.RiskLevel,
            RequiresPayment = obligation.RequiresPayment,
            RequiresSubmissionProof = obligation.RequiresSubmissionProof,
            RequiresPaymentProof = obligation.RequiresPaymentProof,
            IsActive = obligation.IsActive,
            CreatedAt = obligation.CreatedAt,
            UpdatedAt = obligation.UpdatedAt,
            DeclarationCount = declarationCount,
            OpenDeclarationCount = openDeclarationCount,
            Responsibles = obligation.Responsibles
                .OrderBy(item => item.Type)
                .Select(item =>
                {
                    var user = users[item.UserId];
                    return new TaxObligationResponsibleDto(item.UserId, user.DisplayName, user.Email, item.Type);
                })
                .ToArray(),
            ScheduleRules = obligation.ScheduleRules.OrderBy(item => item.DueMonth).ThenBy(item => item.DueDay)
                .Select(item => new TaxScheduleRuleDetailsDto(
                    item.DueDay, item.DueMonth, item.MoveToNextBusinessDay,
                    item.RawReminderText, item.ReminderDays, item.IsActive))
                .ToArray(),
            Versions = versions.Select(version => new TaxObligationVersionDetailsDto(
                    version.Id, version.VersionNumber, version.EffectiveFrom, version.EffectiveTo,
                    version.Frequency, version.FilingDeadlineRule, version.PaymentDeadlineRule,
                    version.RawDeadlineText, version.BusinessCycle,
                    version.TaxAuthorityId.HasValue ? authorities.GetValueOrDefault(version.TaxAuthorityId.Value) : null,
                    version.ValidationStatus, version.RequiresReview, version.ChangeReason,
                    rateRules.Where(item => item.TaxObligationVersionId == version.Id).Select(item => item.RuleText).ToArray(),
                    legalReferences.Where(item => item.TaxObligationVersionId == version.Id).Select(item => item.ReferenceText).ToArray(),
                    penaltyRules.Where(item => item.TaxObligationVersionId == version.Id).Select(item => item.RuleText).ToArray(),
                    processTemplates.Where(item => item.TaxObligationVersionId == version.Id).Select(item => item.ProcessText).ToArray()))
                .ToArray(),
            RequiredDocuments = requiredDocuments.Select(item =>
                new TaxRequiredDocumentDetailsDto(item.DocumentType, item.IsRequired, item.Condition)).ToArray(),
            Aliases = aliases.Select(item =>
                new TaxAliasDetailsDto(item.Alias, item.SourceName, item.SourceReference, item.SourceRow)).ToArray(),
            SourceReferences = sourceReferences.Select(item =>
                new TaxSourceReferenceDetailsDto(item.SourceName, item.SourceRow, item.ExternalNumber, item.ImportedAt, item.DataHash)).ToArray(),
            Conflicts = conflicts.Select(item =>
                new TaxCatalogConflictDetailsDto(item.FieldName, item.ExistingValue, item.IncomingValue,
                    item.SourceName, item.SourceRow, item.Status, item.Resolution)).ToArray(),
            Allocations = allocations.Select(item =>
                new TaxAllocationDetailsDto(item.Percentage, item.Beneficiary, item.SourceName, item.SourceRow)).ToArray()
        };
    }

    private async Task<TaxObligationDetailsDto> ToAuditPayloadAsync(TaxObligation obligation, CancellationToken cancellationToken)
    {
        return await ToDetailsAsync(obligation, cancellationToken);
    }

    private async Task<TaxObligationDetailsDto?> ToAuditPayloadByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var obligation = await dbContext.TaxObligations
            .AsNoTracking()
            .Include(current => current.Responsibles)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

        return obligation is null ? null : await ToDetailsAsync(obligation, cancellationToken);
    }

    private void AddAudit(string action, Guid entityId, object? oldValue, object? newValue)
    {
        auditService.Add(new AuditEntry(
            action,
            nameof(TaxObligation),
            entityId.ToString(),
            oldValue,
            newValue,
            AuditModule,
            "Web"));
    }

    private async Task<LegalEntity> EnsureDefaultLegalEntityAsync(CancellationToken cancellationToken)
    {
        const string code = "ETHAN";
        var legalEntity = await dbContext.LegalEntities.FirstOrDefaultAsync(item => item.Code == code, cancellationToken);
        if (legalEntity is not null)
        {
            return legalEntity;
        }

        legalEntity = new LegalEntity(code, "ETHAN TCM", "MA", null);
        dbContext.LegalEntities.Add(legalEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return legalEntity;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
