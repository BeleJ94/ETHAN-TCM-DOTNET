using System.Security.Cryptography;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EthanTcm.Infrastructure.Services;

public sealed class CorrespondenceService(EthanTcmDbContext db, ICurrentUserService user, IAuditService audit,
    IOptions<TaxDocumentStorageOptions> storageOptions) : ICorrespondenceService
{
    private static readonly CorrespondenceStatus[] Closed = [CorrespondenceStatus.Closed, CorrespondenceStatus.Cancelled, CorrespondenceStatus.FiledWithoutAction];
    public async Task<CorrespondencePage> SearchAsync(CorrespondenceQuery q, CancellationToken ct = default)
    {
        var query = Visible(db.Correspondences.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(q.Search)) { var s = q.Search.Trim(); query = query.Where(x => x.Subject.Contains(s) || (x.ReferenceNumber != null && x.ReferenceNumber.Contains(s)) || (x.SenderOrganization != null && x.SenderOrganization.Contains(s)) || (x.RecipientOrganization != null && x.RecipientOrganization.Contains(s))); }
        if (q.Direction.HasValue) query = query.Where(x => x.Direction == q.Direction);
        if (q.Status.HasValue) query = query.Where(x => x.Status == q.Status);
        if (q.MyItems && user.UserId.HasValue) query = query.Where(x => x.AssignedToUserId == user.UserId);
        if (q.AssignedToUserId.HasValue) query = query.Where(x => x.AssignedToUserId == q.AssignedToUserId);
        if (q.DepartmentId.HasValue) query = query.Where(x => x.InternalDepartmentId == q.DepartmentId);
        var filterToday = DateOnly.FromDateTime(DateTime.UtcNow); var filterSoon = filterToday.AddDays(5);
        var finalActions = new[] { CorrespondenceActionStatus.Completed, CorrespondenceActionStatus.Cancelled };
        if (q.Risk.HasValue) query = q.Risk.Value switch
        {
            CorrespondenceRiskFilter.Unassigned => query.Where(x => x.AssignedToUserId == null && !Closed.Contains(x.Status)),
            CorrespondenceRiskFilter.Overdue => query.Where(x => x.DueDate < filterToday && !Closed.Contains(x.Status)),
            CorrespondenceRiskFilter.DueToday => query.Where(x => x.DueDate == filterToday && !Closed.Contains(x.Status)),
            CorrespondenceRiskFilter.DueSoon => query.Where(x => x.DueDate > filterToday && x.DueDate <= filterSoon && !Closed.Contains(x.Status)),
            CorrespondenceRiskFilter.Waiting => query.Where(x => db.CorrespondenceFollowUpActions.Any(a => a.CorrespondenceId == x.Id && a.Status == CorrespondenceActionStatus.WaitingForThirdParty)),
            CorrespondenceRiskFilter.Escalated => query.Where(x => db.CorrespondenceFollowUpActions.Any(a => a.CorrespondenceId == x.Id && a.IsEscalated && !finalActions.Contains(a.Status))),
            CorrespondenceRiskFilter.Critical => query.Where(x => x.Priority == CorrespondencePriority.Critical && !Closed.Contains(x.Status)),
            _ => query
        };
        var page = Math.Max(1, q.Page); var size = Math.Clamp(q.PageSize, 10, 50); var total = await query.CountAsync(ct); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await (from x in query join u in db.Users.AsNoTracking() on x.AssignedToUserId equals u.Id into users from u in users.DefaultIfEmpty()
            orderby x.Priority descending, x.DueDate, x.CreatedAt descending
            select new CorrespondenceListItem(x.Id, x.ReferenceNumber ?? "DRAFT", x.Direction, x.Subject,
                x.Direction == CorrespondenceDirection.Incoming ? (x.SenderOrganization ?? x.SenderName ?? "—") : (x.RecipientOrganization ?? x.RecipientName ?? "—"),
                x.Status, x.Priority, x.CorrespondenceDate, x.DueDate, u == null ? null : u.DisplayName, x.DueDate < today && !Closed.Contains(x.Status),
                db.CorrespondenceFollowUpActions.Count(a => a.CorrespondenceId == x.Id && !finalActions.Contains(a.Status)),
                db.CorrespondenceFollowUpActions.Any(a => a.CorrespondenceId == x.Id && a.IsEscalated && !finalActions.Contains(a.Status)),
                db.CorrespondenceFollowUpActions.Any(a => a.CorrespondenceId == x.Id && a.Status == CorrespondenceActionStatus.WaitingForThirdParty)))
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows, page, size, total, (int)Math.Ceiling(total / (double)size));
    }
    public async Task<CorrespondenceDetails?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var x = await Visible(db.Correspondences.AsNoTracking()).Include(x => x.History).Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id, ct); if (x is null) return null;
        var userIds = x.History.Select(h => h.ActorUserId).Append(x.AssignedToUserId ?? Guid.Empty).Distinct().ToArray();
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        var departmentName = x.InternalDepartmentId.HasValue ? await db.Departments.AsNoTracking().Where(d => d.Id == x.InternalDepartmentId).Select(d => d.Name).FirstOrDefaultAsync(ct) : null;
        return new(x.Id, x.ReferenceNumber, x.BusinessReference, x.Direction, x.Subject, x.Summary, x.Status, x.Priority, x.Confidentiality, x.Channel,
            x.CorrespondenceDate, x.ReceivedAt, x.SentAt, x.DueDate, x.SenderName, x.SenderOrganization, x.RecipientName, x.RecipientOrganization,
            x.AssignedToUserId, x.AssignedToUserId.HasValue && names.TryGetValue(x.AssignedToUserId.Value, out var assigned) ? assigned : null,
            x.CorrespondentOrganizationId, x.InternalDepartmentId, departmentName,
            x.ParentCorrespondenceId, x.TaxDeclarationId, x.TaxObligationId,
            x.History.OrderByDescending(h => h.OccurredAt).Select(h => new CorrespondenceHistoryItem(h.Action, h.FromStatus, h.ToStatus, h.Comment, h.OccurredAt, names.GetValueOrDefault(h.ActorUserId, "User"))).ToArray(),
            x.Documents.OrderByDescending(d => d.CreatedAt).Select(d => new CorrespondenceDocumentItem(d.Id, d.Type, d.FileName, d.FileSizeBytes, d.CreatedAt)).ToArray());
    }
    public async Task<CorrespondenceDashboard> GetDashboardAsync(CancellationToken ct = default)
    {
        var q = Visible(db.Correspondences.AsNoTracking()); var today = DateOnly.FromDateTime(DateTime.UtcNow); var soon = today.AddDays(5); var open = q.Where(x => !Closed.Contains(x.Status));
        var total = await open.CountAsync(ct); var received = await q.CountAsync(x => x.ReceivedAt != null && x.ReceivedAt.Value.Date == DateTime.UtcNow.Date, ct);
        var unassigned = await open.CountAsync(x => x.AssignedToUserId == null, ct); var overdue = await open.CountAsync(x => x.DueDate < today, ct);
        var dueToday = await open.CountAsync(x => x.DueDate == today, ct); var dueSoon = await open.CountAsync(x => x.DueDate > today && x.DueDate <= soon, ct); var validation = await open.CountAsync(x => x.Status == CorrespondenceStatus.SubmittedForValidation, ct);
        var ready = await open.CountAsync(x => x.Status == CorrespondenceStatus.ReadyToSend, ct);
        var priority = await SearchAsync(new(null, null, null, false, 1, 10), ct);
        var closedDurations = await q.Where(x => x.ClosedAt != null).Select(x => EF.Functions.DateDiffDay(x.CreatedAt, x.ClosedAt!.Value)).Take(500).ToListAsync(ct);
        var openIds = open.Select(x => x.Id); var finalActions = new[] { CorrespondenceActionStatus.Completed, CorrespondenceActionStatus.Cancelled };
        var waiting = await db.CorrespondenceFollowUpActions
            .Where(x => openIds.Contains(x.CorrespondenceId) && x.Status == CorrespondenceActionStatus.WaitingForThirdParty)
            .Select(x => x.CorrespondenceId).Distinct().CountAsync(ct);
        var escalated = await db.CorrespondenceFollowUpActions
            .Where(x => openIds.Contains(x.CorrespondenceId) && x.IsEscalated && !finalActions.Contains(x.Status))
            .Select(x => x.CorrespondenceId).Distinct().CountAsync(ct);
        var since = DateTimeOffset.UtcNow.AddDays(-30); var closed30 = await q.CountAsync(x => x.ClosedAt >= since, ct);
        var closureSamples = await q.Where(x => x.ClosedAt != null && x.DueDate != null).Select(x => new { x.DueDate, x.ClosedAt }).Take(500).ToListAsync(ct);
        var onTime = closureSamples.Count == 0 ? 0 : Math.Round(100m * closureSamples.Count(x => DateOnly.FromDateTime(x.ClosedAt!.Value.UtcDateTime) <= x.DueDate) / closureSamples.Count, 1);
        var incoming = await open.CountAsync(x => x.Direction == CorrespondenceDirection.Incoming, ct); var outgoing = total - incoming;
        return new(total, received, unassigned, overdue, dueToday, dueSoon, validation, ready, waiting, escalated, closed30,
            closedDurations.Count == 0 ? 0 : Math.Round((decimal)closedDurations.Average(), 1), onTime, incoming, outgoing, priority.Items);
    }
    public async Task<CorrespondenceMetricDetails> GetMetricDetailsAsync(CorrespondenceMetric metric, int page, int pageSize, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 10, 50);
        var query = MetricQuery(metric, today);
        var total = await query.CountAsync(ct);
        var rows = await ProjectMetricItems(query, today)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(ct);
        var (title, definition) = MetricMetadata(metric);
        return new(metric, title, definition, total, normalizedPage, normalizedPageSize, rows);
    }
    public async Task<DashboardMetricExportDto> ExportMetricDetailsAsync(CorrespondenceMetric metric, DashboardExportFormat format, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await ProjectMetricItems(MetricQuery(metric, today), today).ToListAsync(ct);
        var (title, definition) = MetricMetadata(metric);
        return CorrespondenceExecutiveReportBuilder.Build(title, definition, metric, DateTimeOffset.UtcNow, items, format);
    }
    public async Task<IReadOnlyCollection<CorrespondenceOption>> GetUsersAsync(CancellationToken ct = default) => await db.Users.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName).Select(x => new CorrespondenceOption(x.Id, x.DisplayName + (string.IsNullOrWhiteSpace(x.Email) ? string.Empty : " — " + x.Email))).ToListAsync(ct);
    public async Task<CorrespondenceReferenceData> GetReferenceDataAsync(CancellationToken ct = default) => new(
        await db.CorrespondenceOrganizations.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CorrespondenceOption(x.Id, x.Code + " — " + x.Name)).ToListAsync(ct),
        await db.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CorrespondenceOption(x.Id, x.Code + " — " + x.Name)).ToListAsync(ct));
    public async Task<CorrespondenceResult> CreateAsync(CorrespondenceCreateCommand c, CancellationToken ct = default)
    {
        if (!user.UserId.HasValue) return Fail("Current user is required.");
        var organization = await db.CorrespondenceOrganizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == c.CorrespondentOrganizationId && x.IsActive, ct);
        var departmentExists = await db.Departments.AsNoTracking().AnyAsync(x => x.Id == c.InternalDepartmentId && x.IsActive, ct);
        if (organization is null) return Fail("A registered correspondent organisation is required.");
        if (!departmentExists) return Fail("An active internal department is required.");
        var x = new Correspondence(c.Direction, c.Subject, c.BusinessReference, c.CorrespondenceDate, c.Priority, c.Confidentiality, c.Channel, c.SenderName, c.SenderOrganization, c.RecipientName, c.RecipientOrganization, c.Summary);
        x.MarkCreatedBy(user.UserId.Value); x.SetRouting(organization.Id, organization.Name, c.InternalDepartmentId); x.Link(c.ParentCorrespondenceId, c.TaxDeclarationId, c.TaxObligationId); db.Correspondences.Add(x); Audit("Create", x); await db.SaveChangesAsync(ct); return new(true, x.Id, null);
    }
    public async Task<CorrespondenceResult> RegisterAsync(Guid id, CancellationToken ct = default)
    {
        if (!user.UserId.HasValue) return Fail("Current user is required.");

        var actorId = user.UserId.Value;
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                // A retry must start from database state, not from entities mutated by a failed attempt.
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                var correspondence = await db.Correspondences.FirstOrDefaultAsync(item => item.Id == id, ct);
                if (correspondence is null) return Fail("Correspondence not found.");
                if (correspondence.Status != CorrespondenceStatus.Draft)
                {
                    return correspondence.ReferenceNumber is not null
                        ? new CorrespondenceResult(true, correspondence.Id, "Correspondence is already registered.")
                        : Fail("Only a draft correspondence can be registered.");
                }

                var year = DateTime.UtcNow.Year;
                var sequence = await db.CorrespondenceSequences
                    .FirstOrDefaultAsync(item => item.Year == year && item.Direction == correspondence.Direction, ct);
                if (sequence is null)
                {
                    sequence = new CorrespondenceSequence(year, correspondence.Direction);
                    db.CorrespondenceSequences.Add(sequence);
                }

                var number = sequence.Next();
                correspondence.Register(
                    $"{(correspondence.Direction == CorrespondenceDirection.Incoming ? "IN" : "OUT")}-{year}-{number:000000}",
                    actorId,
                    DateTimeOffset.UtcNow);
                // The aggregate creates history entries with domain-generated GUIDs. Explicitly mark
                // them as Added so EF Core does not interpret their non-empty keys as existing rows.
                db.CorrespondenceHistory.AddRange(correspondence.History);
                Audit("Register", correspondence);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new CorrespondenceResult(true, correspondence.Id, null);
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            var current = await db.Correspondences.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, ct);
            if (current?.ReferenceNumber is not null && current.Status != CorrespondenceStatus.Draft)
            {
                return new CorrespondenceResult(true, current.Id, "Correspondence was already registered by another request.");
            }

            return Fail("The correspondence changed while it was being registered. Refresh the page and try again.");
        }
    }
    public async Task<CorrespondenceResult> AssignAsync(Guid id, Guid userId, DateOnly? due, string? comment, CancellationToken ct = default) => await Mutate(id, x => x.Assign(userId, due, comment, Current(), DateTimeOffset.UtcNow), "Assign", ct);
    public async Task<CorrespondenceResult> AdvanceAsync(Guid id, CorrespondenceStatus target, string? comment, CancellationToken ct = default) => await Mutate(id, x => x.Advance(target, Current(), comment, DateTimeOffset.UtcNow), target.ToString(), ct);
    public async Task<CorrespondenceResult> UploadAsync(CorrespondenceUploadCommand c, CancellationToken ct = default)
    {
        var x = await Visible(db.Correspondences).FirstOrDefaultAsync(x => x.Id == c.CorrespondenceId, ct); if (x is null) return Fail("Correspondence not found.");
        if (c.Length <= 0 || c.Length > storageOptions.Value.MaxFileSizeBytes) return Fail("Invalid file size.");
        var ext = Path.GetExtension(c.FileName).ToLowerInvariant(); if (!storageOptions.Value.AllowedExtensions.Contains(ext.TrimStart('.'), StringComparer.OrdinalIgnoreCase)) return Fail("File extension is not allowed.");
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(storageOptions.Value.RootPath) ? "App_Data/Documents" : storageOptions.Value.RootPath); var folder = Path.Combine(root, "Correspondence", x.Id.ToString("N")); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{c.Type}_{RandomNumberGenerator.GetHexString(12)}{ext}"); await using (var file = File.Create(path)) await c.Content.CopyToAsync(file, ct);
        var d = new CorrespondenceDocument(x.Id, c.Type, Path.GetFileName(c.FileName), path, c.ContentType, c.Length, Current()); db.Add(d); Audit("UploadDocument", x); await db.SaveChangesAsync(ct); return new(true, x.Id, null);
    }
    public async Task<CorrespondenceDownload?> DownloadAsync(Guid documentId, CancellationToken ct = default)
    {
        var d = await db.CorrespondenceDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct); if (d is null || !await Visible(db.Correspondences.AsNoTracking()).AnyAsync(x => x.Id == d.CorrespondenceId, ct) || !File.Exists(d.FilePath)) return null;
        return new(d.FileName, d.ContentType, d.FilePath);
    }
    private IQueryable<Correspondence> Visible(IQueryable<Correspondence> q) => user.IsInRole(ApplicationRoles.Administrator) || user.IsInRole(ApplicationRoles.TaxManager) || user.IsInRole(ApplicationRoles.Auditor) ? q : user.UserId.HasValue ? q.Where(x => x.CreatedByUserId == user.UserId || x.AssignedToUserId == user.UserId) : q.Where(_ => false);
    private IQueryable<Correspondence> MetricQuery(CorrespondenceMetric metric, DateOnly today)
    {
        var query = Visible(db.Correspondences.AsNoTracking());
        var open = query.Where(x => !Closed.Contains(x.Status));
        var finalActions = new[] { CorrespondenceActionStatus.Completed, CorrespondenceActionStatus.Cancelled };
        return metric switch
        {
            CorrespondenceMetric.OpenWorkload => open,
            CorrespondenceMetric.Unassigned => open.Where(x => x.AssignedToUserId == null),
            CorrespondenceMetric.Overdue => open.Where(x => x.DueDate < today),
            CorrespondenceMetric.DueToday => open.Where(x => x.DueDate == today),
            CorrespondenceMetric.DueSoon => open.Where(x => x.DueDate > today && x.DueDate <= today.AddDays(5)),
            CorrespondenceMetric.Escalated => open.Where(x => db.CorrespondenceFollowUpActions.Any(a => a.CorrespondenceId == x.Id && a.IsEscalated && !finalActions.Contains(a.Status))),
            CorrespondenceMetric.WaitingForThirdParty => open.Where(x => db.CorrespondenceFollowUpActions.Any(a => a.CorrespondenceId == x.Id && a.Status == CorrespondenceActionStatus.WaitingForThirdParty)),
            CorrespondenceMetric.ClosedLast30Days => query.Where(x => x.ClosedAt >= DateTimeOffset.UtcNow.AddDays(-30)),
            CorrespondenceMetric.OnTimeClosure => query.Where(x => x.ClosedAt != null && x.DueDate != null && DateOnly.FromDateTime(x.ClosedAt.Value.UtcDateTime) <= x.DueDate),
            CorrespondenceMetric.IncomingOpen => open.Where(x => x.Direction == CorrespondenceDirection.Incoming),
            CorrespondenceMetric.OutgoingOpen => open.Where(x => x.Direction == CorrespondenceDirection.Outgoing),
            _ => open
        };
    }
    private IQueryable<CorrespondenceMetricItem> ProjectMetricItems(IQueryable<Correspondence> query, DateOnly today)
    {
        return from x in query
               join owner in db.Users.AsNoTracking() on x.AssignedToUserId equals owner.Id into owners
               from owner in owners.DefaultIfEmpty()
               join department in db.Departments.AsNoTracking() on x.InternalDepartmentId equals department.Id into departments
               from department in departments.DefaultIfEmpty()
               orderby x.Priority descending, x.DueDate, x.CreatedAt descending
               select new CorrespondenceMetricItem(
                   x.Id, x.ReferenceNumber ?? "DRAFT", x.Direction, x.Subject,
                   x.Direction == CorrespondenceDirection.Incoming ? (x.SenderOrganization ?? x.SenderName ?? "-") : (x.RecipientOrganization ?? x.RecipientName ?? "-"),
                   x.Status, x.Priority, x.CorrespondenceDate, x.DueDate,
                   owner == null ? null : owner.DisplayName, department == null ? null : department.Name,
                   x.AssignedToUserId == null ? "Owner assignment required" :
                       x.DueDate < today && !Closed.Contains(x.Status) ? "Immediate action - overdue" :
                       x.Status == CorrespondenceStatus.AwaitingResponse ? "Waiting for response" :
                       x.Status == CorrespondenceStatus.SubmittedForValidation ? "Validation required" :
                       Closed.Contains(x.Status) ? "Completed" : "Follow-up required",
                   x.DueDate < today && !Closed.Contains(x.Status) ? today.DayNumber - x.DueDate.GetValueOrDefault().DayNumber : 0);
    }
    private static (string Title, string Definition) MetricMetadata(CorrespondenceMetric metric) => metric switch
    {
        CorrespondenceMetric.OpenWorkload => ("Open correspondence workload", "All correspondence records that have not reached a final status."),
        CorrespondenceMetric.Unassigned => ("Unassigned correspondence", "Open correspondence records without an accountable owner."),
        CorrespondenceMetric.Overdue => ("Overdue correspondence", "Open correspondence records whose due date is before today."),
        CorrespondenceMetric.DueToday => ("Correspondence due today", "Open correspondence records requiring completion today."),
        CorrespondenceMetric.DueSoon => ("Correspondence due in five days", "Open correspondence records due after today and within the next five days."),
        CorrespondenceMetric.Escalated => ("Escalated correspondence", "Open correspondence records with at least one active escalated follow-up action."),
        CorrespondenceMetric.WaitingForThirdParty => ("Waiting for a third party", "Open correspondence records with a follow-up action blocked by an external response."),
        CorrespondenceMetric.ClosedLast30Days => ("Closed in the last 30 days", "Correspondence records closed during the rolling 30-day reporting window."),
        CorrespondenceMetric.OnTimeClosure => ("On-time closures", "Closed correspondence records completed on or before their assigned due date."),
        CorrespondenceMetric.IncomingOpen => ("Open incoming correspondence", "All incoming correspondence records that remain operationally open."),
        CorrespondenceMetric.OutgoingOpen => ("Open outgoing correspondence", "All outgoing correspondence records that remain operationally open."),
        _ => ("Correspondence details", "Detailed composition of the selected indicator.")
    };
    private Guid Current() => user.UserId ?? throw new InvalidOperationException("Current user is required.");
    private async Task<CorrespondenceResult> Mutate(Guid id, Action<Correspondence> action, string auditAction, CancellationToken ct)
    {
        var correspondence = await Visible(db.Correspondences).Include(item => item.FollowUpActions).FirstOrDefaultAsync(item => item.Id == id, ct);
        if (correspondence is null) return Fail("Correspondence not found.");
        try
        {
            action(correspondence);
            db.CorrespondenceHistory.AddRange(correspondence.History);
            Audit(auditAction, correspondence);
            await db.SaveChangesAsync(ct);
            return new(true, id, null);
        }
        catch (Exception ex) when (ex is EthanTcm.Domain.Common.DomainException or DbUpdateConcurrencyException)
        {
            return Fail(ex.Message);
        }
    }
    private void Audit(string action, Correspondence x) => audit.Add(new(action, nameof(Correspondence), x.Id.ToString(), null, new { x.ReferenceNumber, x.Status, x.AssignedToUserId }, "Correspondence", "Web"));
    private static CorrespondenceResult Fail(string message) => new(false, null, message);
}
