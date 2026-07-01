using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.TaxCatalog;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EthanTcm.Infrastructure.Services;

public sealed partial class TaxCatalogSynchronizationService(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService) : ITaxCatalogSynchronizationService
{
    private const string Module = "Tax Catalog Synchronization";

    public async Task<TaxCatalogSynchronizationReport> SynchronizeAsync(
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        if (dryRun || !dbContext.Database.IsRelational())
            return await SynchronizeCoreAsync(dryRun, cancellationToken);

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            return await SynchronizeCoreAsync(dryRun: false, cancellationToken);
        });
    }

    private async Task<TaxCatalogSynchronizationReport> SynchronizeCoreAsync(
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var state = new SynchronizationState(dryRun);
        var existing = await dbContext.TaxObligations
            .Include(x => x.Responsibles)
            .ToListAsync(cancellationToken);
        var preservedIds = existing.ToDictionary(x => x.Id, x => x.CanonicalCode ?? x.Name);
        var aliases = await dbContext.TaxObligationAliases.ToListAsync(cancellationToken);
        var references = await dbContext.TaxSourceReferences.ToListAsync(cancellationToken);
        var versions = await dbContext.TaxObligationVersions.ToListAsync(cancellationToken);
        var conflicts = await dbContext.TaxCatalogConflicts.ToListAsync(cancellationToken);
        var requiredDocuments = await dbContext.TaxRequiredDocuments.ToListAsync(cancellationToken);
        var allocations = await dbContext.TaxAllocationRules.ToListAsync(cancellationToken);

        var legalEntity = await dbContext.LegalEntities.FirstAsync(cancellationToken);
        var fallbackResponsible = existing.SelectMany(x => x.Responsibles).FirstOrDefault();
        var fallbackUser = fallbackResponsible is null
            ? await dbContext.Users.FirstAsync(cancellationToken)
            : await dbContext.Users.FirstAsync(x => x.Id == fallbackResponsible.UserId, cancellationToken);

        IDbContextTransaction? transaction = null;
        if (!dryRun && dbContext.Database.IsRelational())
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var departments = await dbContext.Departments.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var categories = await dbContext.TaxCategories.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var frequencies = await dbContext.TaxFrequencies.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var authorities = await dbContext.TaxAuthorities.ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var users = (await dbContext.Users.Where(x => x.Email != null).ToListAsync(cancellationToken))
                .GroupBy(x => x.Email!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in ConsolidatedTaxCatalog.Items)
            {
                var obligation = FindExisting(item, existing, aliases);
                if (obligation is null)
                {
                    state.Created++;
                    if (dryRun)
                    {
                        state.InactiveCodes.Add(item.CanonicalCode);
                        state.Versioned++;
                        state.AliasesCreated++;
                        state.SourceReferencesCreated += 1 + ConsolidatedTaxCatalog.Reconciliation.Count(x => x.AppliesTo(item.CanonicalCode));
                        continue;
                    }

                    var department = EnsureDepartment(item.Department, departments);
                    var category = EnsureCategory(item.Category, categories);
                    var frequency = EnsureFrequency(item.Frequency, frequencies);
                    obligation = new TaxObligation(
                        legalEntity.Id, department.Id, category.Id, frequency.Id,
                        fallbackUser.Id, item.Name, RiskLevel.High, requiresPayment: true,
                        DateTimeOffset.UtcNow);
                    obligation.AssignCanonicalCode(item.CanonicalCode, DateTimeOffset.UtcNow);
                    obligation.UpdateCatalogMetadata(
                        item.PenaltyOrComments,
                        TaxCatalogValidationStatus.PendingValidation,
                        true,
                        item.RequiresReview ? "Incomplete or conflicting consolidated source data." : "New obligation pending fiscal validation.",
                        DateTimeOffset.UtcNow);
                    obligation.Deactivate(DateTimeOffset.UtcNow);
                    CopyParentResponsibilities(item.CanonicalCode, obligation, existing);
                    dbContext.TaxObligations.Add(obligation);
                    existing.Add(obligation);
                }
                else
                {
                    state.Linked++;
                    preservedIds[obligation.Id] = item.CanonicalCode;
                    if (!dryRun && obligation.CanonicalCode is null)
                    {
                        obligation.AssignCanonicalCode(item.CanonicalCode, DateTimeOffset.UtcNow);
                        state.Enriched++;
                    }
                }

                if (dryRun)
                {
                    if (obligation.CanonicalCode is null)
                        state.Enriched++;
                    if (obligation.IsActive) state.ActiveCodes.Add(item.CanonicalCode);
                    else state.InactiveCodes.Add(item.CanonicalCode);
                    if (NeedsVersion(item, obligation.Id, versions)) state.Versioned++;
                    state.AliasesCreated += MissingAliasCount(item, obligation.Id, aliases);
                    state.SourceReferencesCreated += MissingReferenceCount(item, obligation.Id, references);
                    continue;
                }

                var authority = EnsureAuthority(item.Authority, authorities);
                EnsureOperationalAssignments(item.CanonicalCode, obligation, users);
                var version = EnsureVersion(item, obligation, versions, authority?.Id, state);
                EnsureTraceability(item, obligation, aliases, references, state);
                EnsureLegalDetails(item, version);
                EnsureRequiredDocuments(obligation, item, requiredDocuments);
                AddAudit(obligation, item);
                if (obligation.IsActive) state.ActiveCodes.Add(item.CanonicalCode);
                else state.InactiveCodes.Add(item.CanonicalCode);
            }

            state.DuplicatesMerged = 13;
            state.Skipped = 2;
            state.Messages.Add("Customs regime and Other taxes/payments kept as review-only source rows; no active obligation created.");
            EnsureKnownConflicts(conflicts, state, dryRun);
            EnsureAllocations(existing, allocations, state, dryRun);

            if (!dryRun)
            {
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    var entries = string.Join(", ", exception.Entries.Select(entry => $"{entry.Metadata.ClrType.Name}:{entry.State}"));
                    throw new InvalidOperationException($"Tax catalog synchronization concurrency failure: {entries}", exception);
                }
                LinkExistingDeclarations(versions, state);
                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
            }

            return BuildReport(state, preservedIds);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static TaxObligation? FindExisting(
        TaxCatalogItem item,
        IEnumerable<TaxObligation> obligations,
        IEnumerable<TaxObligationAlias> aliases)
    {
        var byCode = obligations.FirstOrDefault(x => x.CanonicalCode == item.CanonicalCode);
        if (byCode is not null) return byCode;
        var alias = aliases.FirstOrDefault(x => Normalize(x.Alias) == Normalize(item.Name));
        if (alias is not null) return obligations.FirstOrDefault(x => x.Id == alias.TaxObligationId);
        if (int.TryParse(item.ExternalNumber, out var sourceNumber))
        {
            var byNumber = obligations.FirstOrDefault(x => x.SourceNumber == sourceNumber);
            if (byNumber is not null) return byNumber;
        }
        return obligations.FirstOrDefault(x => Normalize(x.Name) == Normalize(item.Name));
    }

    private Department EnsureDepartment(string name, IDictionary<string, Department> cache)
    {
        if (cache.TryGetValue(name, out var found)) return found;
        found = new Department(Code(name), name);
        cache[name] = found; dbContext.Departments.Add(found); return found;
    }

    private TaxCategory EnsureCategory(string name, IDictionary<string, TaxCategory> cache)
    {
        if (cache.TryGetValue(name, out var found)) return found;
        found = new TaxCategory(Code(name), name, $"Consolidated catalog {ConsolidatedTaxCatalog.Version}");
        cache[name] = found; dbContext.TaxCategories.Add(found); return found;
    }

    private TaxFrequency EnsureFrequency(string? name, IDictionary<string, TaxFrequency> cache)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Requires Review" : name.Trim();
        if (cache.TryGetValue(name, out var found)) return found;
        found = new TaxFrequency(Code(name), name, Occurrences(name));
        cache[name] = found; dbContext.TaxFrequencies.Add(found); return found;
    }

    private TaxAuthority? EnsureAuthority(string? name, IDictionary<string, TaxAuthority> cache)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var code = Code(name);
        if (cache.TryGetValue(code, out var found)) return found;
        found = new TaxAuthority(code, name);
        cache[code] = found; dbContext.TaxAuthorities.Add(found); return found;
    }

    private static bool NeedsVersion(TaxCatalogItem item, Guid obligationId, IEnumerable<TaxObligationVersion> versions)
    {
        var hash = HashLegal(item);
        return !versions.Any(x => x.TaxObligationId == obligationId && x.DataHash == hash);
    }

    private TaxObligationVersion EnsureVersion(
        TaxCatalogItem item,
        TaxObligation obligation,
        List<TaxObligationVersion> versions,
        Guid? authorityId,
        SynchronizationState state)
    {
        var previous = versions.Where(x => x.TaxObligationId == obligation.Id)
            .OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        var hash = HashLegal(item);
        var existingCatalogVersion = versions
            .Where(x => x.TaxObligationId == obligation.Id && x.DataHash == hash)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefault();
        if (existingCatalogVersion is not null) return existingCatalogVersion;
        previous?.Close(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        var version = new TaxObligationVersion(
            obligation.Id, (previous?.VersionNumber ?? 0) + 1, DateOnly.FromDateTime(DateTime.UtcNow),
            item.Frequency, item.RawDeadlineText, item.RawDeadlineText, item.RawDeadlineText,
            item.BusinessCycle,
            item.RequiresReview ? TaxCatalogValidationStatus.PendingValidation : TaxCatalogValidationStatus.Validated,
            item.RequiresReview, previous is null ? "Initial consolidated catalog version." : "Consolidated legal data changed.",
            hash, authorityId);
        dbContext.TaxObligationVersions.Add(version);
        versions.Add(version);
        state.Versioned++;
        return version;
    }

    private void EnsureTraceability(
        TaxCatalogItem item, TaxObligation obligation,
        List<TaxObligationAlias> aliases, List<TaxSourceReference> references,
        SynchronizationState state)
    {
        AddAlias(obligation, item.Name, ConsolidatedTaxCatalog.OperationalSource, item.SourceRow, aliases, state);
        AddReference(obligation, ConsolidatedTaxCatalog.OperationalSource, item.SourceRow, item.ExternalNumber, item, references, state);
        foreach (var reconciliation in ConsolidatedTaxCatalog.Reconciliation.Where(x => x.AppliesTo(item.CanonicalCode)))
            AddReference(obligation, ConsolidatedTaxCatalog.LegalSource, reconciliation.SourceRow, null, item, references, state);
    }

    private void AddAlias(TaxObligation obligation, string aliasValue, string source, string row, List<TaxObligationAlias> aliases, SynchronizationState state)
    {
        if (aliases.Any(x => x.TaxObligationId == obligation.Id && x.SourceName == source && x.SourceRow == row && x.Alias == aliasValue)) return;
        var alias = new TaxObligationAlias(obligation.Id, source, null, aliasValue, row);
        aliases.Add(alias); dbContext.TaxObligationAliases.Add(alias); state.AliasesCreated++;
    }

    private void AddReference(TaxObligation obligation, string source, string row, string? externalNumber, TaxCatalogItem item, List<TaxSourceReference> references, SynchronizationState state)
    {
        if (references.Any(x => x.TaxObligationId == obligation.Id && x.SourceName == source && x.SourceRow == row)) return;
        var reference = new TaxSourceReference(obligation.Id, source, row, externalNumber, DateTimeOffset.UtcNow, Hash(JsonSerializer.Serialize(item)));
        references.Add(reference); dbContext.TaxSourceReferences.Add(reference); state.SourceReferencesCreated++;
    }

    private void EnsureLegalDetails(TaxCatalogItem item, TaxObligationVersion version)
    {
        var row = item.LegalSourceRow ?? item.SourceRow;
        if (!string.IsNullOrWhiteSpace(item.RateOrTaxableBasis) &&
            !dbContext.TaxRateRules.Any(x => x.TaxObligationVersionId == version.Id && x.SourceRow == row))
            dbContext.TaxRateRules.Add(new TaxRateRule(version.Id, item.RateOrTaxableBasis, ConsolidatedTaxCatalog.LegalSource, row));
        if (!string.IsNullOrWhiteSpace(item.LegalReference) &&
            !dbContext.TaxLegalReferences.Any(x => x.TaxObligationVersionId == version.Id && x.SourceRow == row))
            dbContext.TaxLegalReferences.Add(new TaxLegalReference(version.Id, item.LegalReference, ConsolidatedTaxCatalog.LegalSource, row));
        if (!string.IsNullOrWhiteSpace(item.PenaltyOrComments) &&
            !dbContext.TaxPenaltyRules.Any(x => x.TaxObligationVersionId == version.Id && x.SourceRow == row))
            dbContext.TaxPenaltyRules.Add(new TaxPenaltyRule(version.Id, item.PenaltyOrComments, ConsolidatedTaxCatalog.LegalSource, row));
        if (!string.IsNullOrWhiteSpace(item.Process) &&
            !dbContext.TaxProcessTemplates.Any(x => x.TaxObligationVersionId == version.Id && x.SourceRow == row))
            dbContext.TaxProcessTemplates.Add(new TaxProcessTemplate(version.Id, item.Process, ConsolidatedTaxCatalog.LegalSource, row));
    }

    private void EnsureRequiredDocuments(TaxObligation obligation, TaxCatalogItem item, List<TaxRequiredDocument> documents)
    {
        DocumentType[] supported =
        [
            DocumentType.CalculationWorkpaper, DocumentType.DeclarationForm,
            DocumentType.ApprovalEvidence, DocumentType.FiledDeclaration,
            DocumentType.FilingReceipt, DocumentType.PerceptionNote,
            DocumentType.PaymentProof, DocumentType.FinancialStatements,
            DocumentType.PermitOrLicence
        ];
        foreach (var type in supported)
        {
            var required = type == DocumentType.FilingReceipt ||
                           (type == DocumentType.PaymentProof && obligation.RequiresPayment) ||
                           (type == DocumentType.FinancialStatements && item.CanonicalCode == "CIT-RETURN");
            var condition = type switch
            {
                DocumentType.FilingReceipt => "Required before closing",
                DocumentType.PaymentProof => "Required before Paid when payment applies",
                DocumentType.FinancialStatements => "Required with annual CIT return",
                DocumentType.PerceptionNote => "Configurable when triggered by a perception note",
                _ => "Configurable by obligation"
            };
            AddRequired(obligation, type, required, condition, documents);
        }
    }

    private void AddRequired(TaxObligation obligation, DocumentType type, bool required, string condition, List<TaxRequiredDocument> documents)
    {
        if (documents.Any(x => x.TaxObligationId == obligation.Id && x.DocumentType == type)) return;
        var document = new TaxRequiredDocument(obligation.Id, type, required, condition);
        documents.Add(document); dbContext.TaxRequiredDocuments.Add(document);
    }

    private void EnsureKnownConflicts(List<TaxCatalogConflict> existing, SynchronizationState state, bool dryRun)
    {
        foreach (var definition in ConsolidatedTaxCatalog.KnownConflicts)
        {
            if (existing.Any(x => x.CanonicalCode == definition.CanonicalCode && x.FieldName == definition.FieldName &&
                                  x.SourceName == definition.SourceName && x.SourceRow == definition.SourceRow)) continue;
            state.Conflicts++;
            if (dryRun) continue;
            var conflict = new TaxCatalogConflict(definition.CanonicalCode, definition.FieldName, definition.ExistingValue,
                definition.IncomingValue, definition.SourceName, definition.SourceRow);
            existing.Add(conflict); dbContext.TaxCatalogConflicts.Add(conflict);
        }
    }

    private void EnsureAllocations(List<TaxObligation> obligations, List<TaxAllocationRule> existing, SynchronizationState state, bool dryRun)
    {
        var royalty = obligations.FirstOrDefault(x => x.CanonicalCode == "MINING-ROYALTY")
            ?? obligations.First(x => x.SourceNumber == 11);
        foreach (var allocation in ConsolidatedTaxCatalog.MiningRoyaltyAllocations)
        {
            if (existing.Any(x => x.TaxObligationId == royalty.Id && x.SourceRow == allocation.SourceRow.ToString())) continue;
            if (dryRun) continue;
            var rule = new TaxAllocationRule(royalty.Id, allocation.Percentage, allocation.Beneficiary,
                ConsolidatedTaxCatalog.LegalSource, allocation.SourceRow.ToString());
            existing.Add(rule); dbContext.TaxAllocationRules.Add(rule);
        }
        state.Messages.Add("Royalty allocation rows 31-35 stored as allocation rules, never as tax obligations.");
    }

    private void LinkExistingDeclarations(List<TaxObligationVersion> versions, SynchronizationState state)
    {
        var latest = versions.GroupBy(x => x.TaxObligationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.VersionNumber).First().Id);
        foreach (var declaration in dbContext.TaxDeclarations.Local.Concat(
                     dbContext.TaxDeclarations.Where(x => x.TaxObligationVersionId == null)).Distinct())
        {
            if (declaration.TaxObligationVersionId is null && latest.TryGetValue(declaration.TaxObligationId, out var versionId))
                declaration.LinkVersion(versionId, DateTimeOffset.UtcNow);
        }
    }

    private void CopyParentResponsibilities(string code, TaxObligation target, IEnumerable<TaxObligation> obligations)
    {
        var parentCode = code.StartsWith("PAYROLL-", StringComparison.Ordinal) ? "PAYROLL-TAXES"
            : code is "VEHICLE-TAX" or "SPECIAL-ROAD-TRAFFIC-TAX" ? "VEHICLE-TAXES" : null;
        if (parentCode is null) return;
        var parent = obligations.FirstOrDefault(x => x.CanonicalCode == parentCode) ??
                     obligations.FirstOrDefault(x => parentCode == "PAYROLL-TAXES" ? x.SourceNumber == 3 : x.SourceNumber == 25);
        if (parent is null) return;
        foreach (var responsible in parent.Responsibles.Where(x => !x.IsPrimary))
            target.AddResponsible(responsible.UserId, responsible.Type, DateTimeOffset.UtcNow);
    }

    private void EnsureOperationalAssignments(
        string canonicalCode,
        TaxObligation obligation,
        IDictionary<string, User> users)
    {
        if (!ConsolidatedTaxCatalog.OperationalAssignments.TryGetValue(canonicalCode, out var assignments))
            return;

        var preparers = assignments.Preparers.Where(IsEmail).ToArray();
        if (preparers.Length > 0)
        {
            var existingPreparer = obligation.Responsibles.FirstOrDefault(x => x.Type == ResponsibleType.Preparer);
            var backupCandidates = preparers.AsEnumerable();
            if (existingPreparer is null)
            {
                AddAssignment(obligation, EnsureUser(preparers[0], users), ResponsibleType.Preparer);
                backupCandidates = preparers.Skip(1);
            }

            foreach (var backup in backupCandidates)
            {
                var backupUser = EnsureUser(backup, users);
                if (existingPreparer?.UserId == backupUser.Id)
                    continue;
                AddAssignment(obligation, backupUser, ResponsibleType.Backup);
            }
        }
        AddAssignments(obligation, assignments.Approver1, ResponsibleType.Approver1, users);
        AddAssignments(obligation, assignments.Approver2, ResponsibleType.Approver2, users);
        AddAssignments(obligation, assignments.Approver3, ResponsibleType.Approver3, users);
        AddAssignments(obligation, assignments.PaymentProcessOwners, ResponsibleType.PaymentProcessOwner, users);
        AddAssignments(obligation, assignments.SubmissionProcessOwners, ResponsibleType.SubmissionProcessOwner, users);
        AddAssignments(obligation, assignments.FollowUpOwners, ResponsibleType.FollowUpOwner, users);
    }

    private void AddAssignments(
        TaxObligation obligation,
        IEnumerable<string> values,
        ResponsibleType type,
        IDictionary<string, User> users)
    {
        foreach (var value in values.Where(IsEmail))
            AddAssignment(obligation, EnsureUser(value, users), type);
    }

    private void AddAssignment(TaxObligation obligation, User user, ResponsibleType type)
    {
        if (!obligation.Responsibles.Any(x => x.UserId == user.Id && x.Type == type))
        {
            obligation.AddResponsible(user.Id, type, DateTimeOffset.UtcNow);
            var added = obligation.Responsibles.Single(x => x.UserId == user.Id && x.Type == type);
            dbContext.Entry(added).State = EntityState.Added;
        }
    }

    private User EnsureUser(string email, IDictionary<string, User> users)
    {
        if (users.TryGetValue(email, out var user)) return user;
        var login = email.Split('@')[0];
        user = users.Values.FirstOrDefault(existing =>
            existing.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        if (user is not null)
        {
            users[email] = user;
            return user;
        }

        user = new User(login, login.Replace('.', ' '), email);
        users[email] = user;
        dbContext.Users.Add(user);
        return user;
    }

    private static bool IsEmail(string value) => value.Contains('@', StringComparison.Ordinal);

    private void AddAudit(TaxObligation obligation, TaxCatalogItem item) =>
        dbContext.AuditLogs.Add(new AuditLog(
            currentUserService.UserId, nameof(TaxObligation), obligation.Id.ToString(),
            "SynchronizeCatalog", null, JsonSerializer.Serialize(new { item.CanonicalCode, ConsolidatedTaxCatalog.Version }),
            DateTimeOffset.UtcNow, module: Module, source: "CLI"));

    private static int MissingAliasCount(TaxCatalogItem item, Guid id, IEnumerable<TaxObligationAlias> aliases) =>
        aliases.Any(x => x.TaxObligationId == id && x.SourceName == ConsolidatedTaxCatalog.OperationalSource &&
                         x.SourceRow == item.SourceRow && x.Alias == item.Name) ? 0 : 1;
    private static int MissingReferenceCount(TaxCatalogItem item, Guid id, IEnumerable<TaxSourceReference> references) =>
        (references.Any(x => x.TaxObligationId == id && x.SourceName == ConsolidatedTaxCatalog.OperationalSource && x.SourceRow == item.SourceRow) ? 0 : 1) +
        ConsolidatedTaxCatalog.Reconciliation.Count(row =>
            row.AppliesTo(item.CanonicalCode) &&
            !references.Any(x => x.TaxObligationId == id && x.SourceName == ConsolidatedTaxCatalog.LegalSource && x.SourceRow == row.SourceRow));

    private static TaxCatalogSynchronizationReport BuildReport(SynchronizationState s, IReadOnlyDictionary<Guid, string> ids) =>
        new(s.DryRun, s.Linked, s.Enriched, s.Created, s.Versioned, s.AliasesCreated,
            s.SourceReferencesCreated, s.DuplicatesMerged, s.Conflicts, s.Skipped,
            s.ActiveCodes.Order().ToArray(), s.InactiveCodes.Order().ToArray(), ids, s.Messages);

    private static string HashLegal(TaxCatalogItem item) => Hash(JsonSerializer.Serialize(new
    {
        item.Frequency, item.RawDeadlineText, item.RateOrTaxableBasis, item.LegalReference,
        item.PenaltyOrComments, item.BusinessCycle, item.Authority
    }));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Normalize(string value) => NonAlphaNumeric().Replace(value.ToUpperInvariant(), "");
    private static string Code(string value)
    {
        var normalized = Underscores()
            .Replace(NonCode().Replace(value.ToUpperInvariant(), "_"), "_")
            .Trim('_');
        if (normalized.Length <= 42)
            return normalized;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..7];
        return $"{normalized[..42]}_{hash}";
    }
    private static int Occurrences(string value) => value.Contains("month", StringComparison.OrdinalIgnoreCase) ? 12 : value.Contains("week", StringComparison.OrdinalIgnoreCase) ? 52 : 1;
    [GeneratedRegex("[^A-Z0-9]")] private static partial Regex NonAlphaNumeric();
    [GeneratedRegex("[^A-Z0-9_]")] private static partial Regex NonCode();
    [GeneratedRegex("_+")] private static partial Regex Underscores();

    private sealed class SynchronizationState(bool dryRun)
    {
        public bool DryRun { get; } = dryRun;
        public int Linked, Enriched, Created, Versioned, AliasesCreated, SourceReferencesCreated, DuplicatesMerged, Conflicts, Skipped;
        public HashSet<string> ActiveCodes { get; } = [];
        public HashSet<string> InactiveCodes { get; } = [];
        public List<string> Messages { get; } = [];
    }
}
