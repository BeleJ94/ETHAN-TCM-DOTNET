using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EthanTcm.Infrastructure.Services;

public sealed class DashboardService(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService,
    IMemoryCache memoryCache)
    : IDashboardService
{
    private static readonly TaxDeclarationStatus[] ClosedStatuses =
    [
        TaxDeclarationStatus.Closed,
        TaxDeclarationStatus.Cancelled,
        TaxDeclarationStatus.NotApplicable
    ];

    private static readonly TaxDeclarationStatus[] ApprovalStatuses =
    [
        TaxDeclarationStatus.SubmittedForReview,
        TaxDeclarationStatus.ApprovedLevel1,
        TaxDeclarationStatus.ApprovedLevel2
    ];

    public Task<DashboardDto> GetAsync(DashboardView view, CancellationToken cancellationToken = default)
    {
        var identity = currentUserService.UserId?.ToString()
            ?? currentUserService.Login
            ?? "anonymous";
        var roles = string.Join(",", currentUserService.Roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase));
        var cacheKey = $"dashboard:{identity}:{roles}:{view}";

        return memoryCache.GetOrCreateAsync(
            cacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                entry.Size = 1;
                return BuildAsync(view, cancellationToken);
            })!;
    }

    private async Task<DashboardDto> BuildAsync(DashboardView view, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var canViewGlobalDashboard = CanViewGlobalDashboard();
        var visibleDeclarations = await GetVisibleDeclarationRowsAsync(cancellationToken);

        var obligationsWithoutResponsible = canViewGlobalDashboard
            ? await dbContext.TaxObligations
                .AsNoTracking()
                .Include(obligation => obligation.Responsibles)
                .Where(obligation => obligation.IsActive)
                .Where(obligation => !obligation.Responsibles.Any(responsible =>
                    responsible.Type == ResponsibleType.Primary || responsible.Type == ResponsibleType.Preparer))
                .OrderBy(obligation => obligation.Name)
                .Select(obligation => new DashboardObligationIssueDto(
                    obligation.Id,
                    obligation.Name,
                    obligation.RiskLevel,
                    "Aucun responsable principal"))
                .Take(50)
                .ToArrayAsync(cancellationToken)
            : [];

        var itemSource = view switch
        {
            DashboardView.MyTasks => visibleDeclarations.Where(IsActionableForCurrentUser),
            DashboardView.TeamTasks => visibleDeclarations,
            DashboardView.ManagementOverview => visibleDeclarations,
            DashboardView.LateItems => visibleDeclarations.Where(declaration => IsLate(declaration, today)),
            DashboardView.ComplianceOverview => visibleDeclarations.Where(declaration =>
                MissingSubmissionProof(declaration) ||
                MissingPaymentProof(declaration) ||
                PenaltyRisk(declaration, today)),
            DashboardView.PaymentPending => visibleDeclarations.Where(declaration => declaration.Status == TaxDeclarationStatus.PaymentPending),
            DashboardView.LatePayments => visibleDeclarations.Where(declaration =>
                declaration.PaymentRequired &&
                declaration.Status == TaxDeclarationStatus.PaymentPending &&
                declaration.DueDate < today),
            DashboardView.MissingPaymentProof => visibleDeclarations.Where(MissingPaymentProof),
            _ => visibleDeclarations.Where(IsActionableForCurrentUser)
        };

        var items = itemSource
            .OrderByDescending(declaration => PenaltyRisk(declaration, today))
            .ThenBy(declaration => declaration.DueDate)
            .Take(75)
            .Select(declaration => ToItem(declaration, today))
            .ToArray();

        var evidenceCompliant = visibleDeclarations.Count(declaration =>
            !MissingSubmissionProof(declaration) && !MissingPaymentProof(declaration));
        var evidenceComplianceRate = visibleDeclarations.Length == 0
            ? 100m
            : decimal.Round(evidenceCompliant * 100m / visibleDeclarations.Length, 1);

        var metrics = new DashboardMetricsDto(
            visibleDeclarations.Length,
            evidenceComplianceRate,
            visibleDeclarations.Count(declaration => declaration.DueDate == today),
            visibleDeclarations.Count(declaration => IsDueInLessThanFiveDays(declaration, today)),
            visibleDeclarations.Count(declaration => declaration.DueDate >= startOfWeek && declaration.DueDate <= endOfWeek),
            visibleDeclarations.Count(declaration => declaration.DueDate >= startOfMonth && declaration.DueDate <= endOfMonth),
            visibleDeclarations.Count(declaration => IsLate(declaration, today)),
            visibleDeclarations.Count(declaration => ApprovalStatuses.Contains(declaration.Status)),
            visibleDeclarations.Count(declaration => declaration.Status == TaxDeclarationStatus.PaymentPending),
            visibleDeclarations.Count(MissingSubmissionProof),
            visibleDeclarations.Count(MissingPaymentProof),
            obligationsWithoutResponsible.Length,
            visibleDeclarations.Count(declaration => PenaltyRisk(declaration, today)));

        return new DashboardDto(
            view,
            today,
            DateTimeOffset.UtcNow,
            metrics,
            items,
            view == DashboardView.ComplianceOverview || view == DashboardView.ManagementOverview ? obligationsWithoutResponsible : []);
    }

    public Task<DashboardMetricDetailsDto> GetMetricDetailsAsync(
        DashboardMetric metric,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var identity = currentUserService.UserId?.ToString()
            ?? currentUserService.Login
            ?? "anonymous";
        var roles = string.Join(",", currentUserService.Roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase));
        var normalizedPageSize = Math.Clamp(pageSize, 10, 50);
        var normalizedPage = Math.Max(1, page);
        var cacheKey = $"dashboard-kpi:{identity}:{roles}:{metric}:{normalizedPage}:{normalizedPageSize}";

        return memoryCache.GetOrCreateAsync(
            cacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                entry.Size = 1;
                return BuildMetricDetailsAsync(
                    metric,
                    normalizedPage,
                    normalizedPageSize,
                    cancellationToken);
            })!;
    }

    private async Task<DashboardMetricDetailsDto> BuildMetricDetailsAsync(
        DashboardMetric metric,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageSize = pageSize;
        var requestedPage = page;
        var (title, definition) = MetricMetadata(metric);

        if (metric == DashboardMetric.ObligationsWithoutResponsible)
        {
            return await GetOwnerlessObligationsAsync(
                metric,
                title,
                definition,
                requestedPage,
                normalizedPageSize,
                cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (metric == DashboardMetric.PenaltyRisk)
        {
            return await GetPenaltyRiskDetailsAsync(
                metric,
                title,
                definition,
                requestedPage,
                normalizedPageSize,
                today,
                cancellationToken);
        }

        var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var query = BuildVisibleDeclarationQuery();

        query = metric switch
        {
            DashboardMetric.DueToday => query.Where(declaration => declaration.DueDate == today),
            DashboardMetric.DueSoon => query.Where(declaration =>
                declaration.DueDate > today && declaration.DueDate < today.AddDays(5)),
            DashboardMetric.DueThisWeek => query.Where(declaration =>
                declaration.DueDate >= startOfWeek && declaration.DueDate <= endOfWeek),
            DashboardMetric.DueThisMonth => query.Where(declaration =>
                declaration.DueDate >= startOfMonth && declaration.DueDate <= endOfMonth),
            DashboardMetric.Late => query.Where(declaration =>
                declaration.DueDate < today &&
                declaration.Status != TaxDeclarationStatus.Submitted &&
                declaration.Status != TaxDeclarationStatus.Paid),
            DashboardMetric.AwaitingApproval => query.Where(declaration =>
                ApprovalStatuses.Contains(declaration.Status)),
            DashboardMetric.PendingPayments => query.Where(declaration =>
                declaration.Status == TaxDeclarationStatus.PaymentPending),
            DashboardMetric.MissingSubmissionProof => query.Where(declaration =>
                (declaration.Status == TaxDeclarationStatus.Submitted ||
                 declaration.Status == TaxDeclarationStatus.PaymentPending ||
                 declaration.Status == TaxDeclarationStatus.Paid) &&
                !dbContext.TaxDocuments.Any(document =>
                    document.TaxDeclarationId == declaration.Id &&
                    document.DocumentType == DocumentType.SubmissionProof &&
                    !document.IsDeleted)),
            DashboardMetric.MissingPaymentProof => query.Where(declaration =>
                declaration.PaymentRequired &&
                (declaration.Status == TaxDeclarationStatus.PaymentPending ||
                 declaration.Status == TaxDeclarationStatus.Paid) &&
                !dbContext.TaxDocuments.Any(document =>
                    document.TaxDeclarationId == declaration.Id &&
                    document.DocumentType == DocumentType.PaymentProof &&
                    !document.IsDeleted)),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
        var pageNumber = Math.Min(requestedPage, totalPages);
        var rows = await (
            from declaration in query
            join obligation in dbContext.TaxObligations.AsNoTracking()
                on declaration.TaxObligationId equals obligation.Id
            join assignedUser in dbContext.Users.AsNoTracking()
                on declaration.AssignedToUserId equals assignedUser.Id
            orderby declaration.DueDate, obligation.Name, declaration.PeriodLabel
            select new
            {
                declaration.Id,
                Name = obligation.Name,
                declaration.PeriodLabel,
                declaration.DueDate,
                declaration.Status,
                ObligationRisk = obligation.RiskLevel,
                declaration.PenaltyRiskLevel,
                AssignedTo = assignedUser.DisplayName
            })
            .Skip((pageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArrayAsync(cancellationToken);

        var issue = MetricIssue(metric);
        var items = rows.Select(row => new DashboardMetricDetailItemDto(
                row.Id,
                DashboardDetailTargetType.TaxDeclaration,
                row.Name,
                row.PeriodLabel,
                row.DueDate,
                row.Status.ToString(),
                row.ObligationRisk > row.PenaltyRiskLevel ? row.ObligationRisk : row.PenaltyRiskLevel,
                row.AssignedTo,
                issue,
                Math.Max(0, today.DayNumber - row.DueDate.DayNumber)))
            .ToArray();

        return new DashboardMetricDetailsDto(
            metric,
            title,
            definition,
            totalCount,
            pageNumber,
            normalizedPageSize,
            items);
    }

    private async Task<DashboardMetricDetailsDto> GetPenaltyRiskDetailsAsync(
        DashboardMetric metric,
        string title,
        string definition,
        int requestedPage,
        int pageSize,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var rows = (await GetVisibleDeclarationRowsAsync(cancellationToken))
            .Where(declaration => PenaltyRisk(declaration, today))
            .OrderByDescending(declaration => IsLate(declaration, today))
            .ThenByDescending(declaration => declaration.ObligationRiskLevel)
            .ThenBy(declaration => declaration.DueDate)
            .ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling(rows.Length / (double)pageSize));
        var pageNumber = Math.Min(requestedPage, totalPages);
        var items = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new DashboardMetricDetailItemDto(
                row.Id,
                DashboardDetailTargetType.TaxDeclaration,
                row.ObligationName,
                row.PeriodLabel,
                row.DueDate,
                row.Status.ToString(),
                row.ObligationRiskLevel > row.PenaltyRiskLevel
                    ? row.ObligationRiskLevel
                    : row.PenaltyRiskLevel,
                row.AssignedTo,
                MetricIssue(metric),
                Math.Max(0, today.DayNumber - row.DueDate.DayNumber)))
            .ToArray();

        return new DashboardMetricDetailsDto(
            metric,
            title,
            definition,
            rows.Length,
            pageNumber,
            pageSize,
            items);
    }

    private async Task<DashboardMetricDetailsDto> GetOwnerlessObligationsAsync(
        DashboardMetric metric,
        string title,
        string definition,
        int requestedPage,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanViewGlobalDashboard())
        {
            return new DashboardMetricDetailsDto(metric, title, definition, 0, 1, pageSize, []);
        }

        var query = dbContext.TaxObligations
            .AsNoTracking()
            .Where(obligation => obligation.IsActive)
            .Where(obligation => !dbContext.TaxObligationResponsibles.Any(responsible =>
                responsible.TaxObligationId == obligation.Id &&
                (responsible.Type == ResponsibleType.Primary || responsible.Type == ResponsibleType.Preparer)));
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var pageNumber = Math.Min(requestedPage, totalPages);
        var rows = await query
            .OrderByDescending(obligation => obligation.RiskLevel)
            .ThenBy(obligation => obligation.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(obligation => new { obligation.Id, obligation.Name, obligation.RiskLevel })
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(row => new DashboardMetricDetailItemDto(
                row.Id,
                DashboardDetailTargetType.TaxObligation,
                row.Name,
                null,
                null,
                "Active",
                row.RiskLevel,
                null,
                "Responsable manquant",
                0))
            .ToArray();

        return new DashboardMetricDetailsDto(
            metric,
            title,
            definition,
            totalCount,
            pageNumber,
            pageSize,
            items);
    }

    private Task<DashboardDeclarationRow[]> GetVisibleDeclarationRowsAsync(CancellationToken cancellationToken)
    {
        var identity = currentUserService.UserId?.ToString()
            ?? currentUserService.Login
            ?? "anonymous";
        var roles = string.Join(",", currentUserService.Roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase));
        var cacheKey = $"dashboard-rows:{identity}:{roles}";

        return memoryCache.GetOrCreateAsync(
            cacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                entry.Size = 1;
                return LoadVisibleDeclarationRowsAsync(cancellationToken);
            })!;
    }

    private async Task<DashboardDeclarationRow[]> LoadVisibleDeclarationRowsAsync(CancellationToken cancellationToken)
    {
        var declarationQuery = BuildVisibleDeclarationQuery();
        var currentUserId = currentUserService.UserId;
        return await (
            from declaration in declarationQuery
            join obligation in dbContext.TaxObligations.AsNoTracking()
                on declaration.TaxObligationId equals obligation.Id
            join assignedUser in dbContext.Users.AsNoTracking()
                on declaration.AssignedToUserId equals assignedUser.Id
            orderby declaration.DueDate, declaration.PeriodLabel
            select new DashboardDeclarationRow(
                declaration.Id,
                obligation.Name,
                declaration.PeriodLabel,
                declaration.DueDate,
                declaration.Status,
                obligation.RiskLevel,
                declaration.PenaltyRiskLevel,
                declaration.PaymentRequired,
                declaration.AssignedToUserId,
                assignedUser.DisplayName,
                currentUserId.HasValue && dbContext.TaxObligationResponsibles.Any(responsible =>
                    responsible.TaxObligationId == declaration.TaxObligationId &&
                    responsible.UserId == currentUserId.Value),
                dbContext.TaxDocuments.Any(document =>
                    document.TaxDeclarationId == declaration.Id &&
                    document.DocumentType == DocumentType.SubmissionProof &&
                    !document.IsDeleted),
                dbContext.TaxDocuments.Any(document =>
                    document.TaxDeclarationId == declaration.Id &&
                    document.DocumentType == DocumentType.PaymentProof &&
                    !document.IsDeleted)))
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<TaxDeclaration> BuildVisibleDeclarationQuery()
    {
        var query = dbContext.TaxDeclarations
            .AsNoTracking()
            .Where(declaration => !ClosedStatuses.Contains(declaration.Status));

        if (CanViewGlobalDashboard())
        {
            return query;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return query.Where(_ => false);
        }

        var userId = currentUserService.UserId.Value;
        return query.Where(declaration =>
            declaration.AssignedToUserId == userId ||
            dbContext.TaxObligationResponsibles.Any(responsible =>
                responsible.TaxObligationId == declaration.TaxObligationId &&
                responsible.UserId == userId));
    }

    private static (string Title, string Definition) MetricMetadata(DashboardMetric metric) => metric switch
    {
        DashboardMetric.DueToday => ("Échéances du jour", "Déclarations ouvertes dont l’échéance est aujourd’hui."),
        DashboardMetric.DueSoon => ("Échéances sous 5 jours", "Déclarations ouvertes attendues dans les quatre prochains jours, hors aujourd’hui."),
        DashboardMetric.DueThisWeek => ("Échéances de la semaine", "Déclarations ouvertes arrivant à échéance pendant la semaine civile en cours."),
        DashboardMetric.DueThisMonth => ("Échéances du mois", "Déclarations ouvertes arrivant à échéance pendant le mois civil en cours."),
        DashboardMetric.Late => ("Déclarations en retard", "Déclarations ouvertes dont l’échéance est dépassée et qui ne sont ni soumises ni payées."),
        DashboardMetric.AwaitingApproval => ("Validations en attente", "Déclarations engagées dans un niveau du circuit d’approbation."),
        DashboardMetric.PendingPayments => ("Paiements en attente", "Déclarations soumises dont le paiement reste à exécuter."),
        DashboardMetric.MissingSubmissionProof => ("Preuves de dépôt manquantes", "Déclarations soumises ou payées sans preuve de dépôt active."),
        DashboardMetric.MissingPaymentProof => ("Preuves de paiement manquantes", "Déclarations payables en attente ou payées sans preuve de paiement active."),
        DashboardMetric.ObligationsWithoutResponsible => ("Taxes sans responsable", "Obligations fiscales actives sans préparateur ni responsable principal."),
        DashboardMetric.PenaltyRisk => ("Exposition aux pénalités", "Retards, échéances proches non préparées, risques élevés ou preuves réglementaires manquantes."),
        _ => ("Détail du KPI", "Éléments composant cet indicateur.")
    };

    private static string MetricIssue(DashboardMetric metric) => metric switch
    {
        DashboardMetric.DueToday => "À traiter aujourd’hui",
        DashboardMetric.DueSoon => "Échéance proche",
        DashboardMetric.DueThisWeek => "À planifier cette semaine",
        DashboardMetric.DueThisMonth => "À planifier ce mois",
        DashboardMetric.Late => "En retard",
        DashboardMetric.AwaitingApproval => "Validation attendue",
        DashboardMetric.PendingPayments => "Paiement attendu",
        DashboardMetric.MissingSubmissionProof => "Preuve de dépôt manquante",
        DashboardMetric.MissingPaymentProof => "Preuve de paiement manquante",
        DashboardMetric.PenaltyRisk => "Risque de pénalité",
        _ => "À examiner"
    };

    private bool CanViewGlobalDashboard()
    {
        return currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager) ||
            currentUserService.IsInRole(ApplicationRoles.Auditor);
    }

    private bool IsActionableForCurrentUser(DashboardDeclarationRow declaration)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return false;
        }

        if (declaration.AssignedToUserId == currentUserService.UserId.Value)
        {
            return true;
        }

        if (currentUserService.IsInRole(ApplicationRoles.Approver) && ApprovalStatuses.Contains(declaration.Status))
        {
            return true;
        }

        if (currentUserService.IsInRole(ApplicationRoles.FinanceManager) && declaration.Status == TaxDeclarationStatus.PaymentPending)
        {
            return true;
        }

        return declaration.IsCurrentUserResponsible;
    }

    private static DashboardDeclarationItemDto ToItem(
        DashboardDeclarationRow declaration,
        DateOnly today)
    {
        var daysLate = Math.Max(0, today.DayNumber - declaration.DueDate.DayNumber);
        return new DashboardDeclarationItemDto(
            declaration.Id,
            declaration.ObligationName,
            declaration.PeriodLabel,
            declaration.DueDate,
            declaration.Status,
            declaration.ObligationRiskLevel,
            declaration.PaymentRequired,
            declaration.AssignedTo,
            ResolveIssue(declaration, today),
            daysLate);
    }

    private static string ResolveIssue(DashboardDeclarationRow declaration, DateOnly today)
    {
        if (IsLate(declaration, today))
        {
            return "En retard";
        }

        if (ApprovalStatuses.Contains(declaration.Status))
        {
            return "Validation attendue";
        }

        if (declaration.Status == TaxDeclarationStatus.PaymentPending)
        {
            return "Paiement attendu";
        }

        if (MissingSubmissionProof(declaration))
        {
            return "Preuve de dépôt manquante";
        }

        if (MissingPaymentProof(declaration))
        {
            return "Preuve de paiement manquante";
        }

        if (declaration.DueDate == today)
        {
            return "Échéance aujourd’hui";
        }

        if (IsDueInLessThanFiveDays(declaration, today))
        {
            return "Échéance sous 5 jours";
        }

        return "À venir";
    }

    private static bool IsLate(DashboardDeclarationRow declaration, DateOnly today)
    {
        return declaration.DueDate < today &&
            declaration.Status is not (TaxDeclarationStatus.Submitted or TaxDeclarationStatus.Paid);
    }

    private static bool IsDueInLessThanFiveDays(DashboardDeclarationRow declaration, DateOnly today)
    {
        return declaration.DueDate > today && declaration.DueDate < today.AddDays(5);
    }

    private static bool MissingSubmissionProof(DashboardDeclarationRow declaration)
    {
        return declaration.Status is (TaxDeclarationStatus.Submitted or TaxDeclarationStatus.PaymentPending or TaxDeclarationStatus.Paid) &&
            !declaration.HasSubmissionProof;
    }

    private static bool MissingPaymentProof(DashboardDeclarationRow declaration)
    {
        return declaration.PaymentRequired &&
            declaration.Status is (TaxDeclarationStatus.PaymentPending or TaxDeclarationStatus.Paid) &&
            !declaration.HasPaymentProof;
    }

    private static bool PenaltyRisk(DashboardDeclarationRow declaration, DateOnly today)
    {
        var dueSoon = declaration.DueDate >= today && declaration.DueDate <= today.AddDays(5);
        var highRisk = declaration.ObligationRiskLevel is RiskLevel.High or RiskLevel.Critical ||
            declaration.PenaltyRiskLevel is RiskLevel.High or RiskLevel.Critical;

        return IsLate(declaration, today) ||
            (dueSoon && declaration.Status is (TaxDeclarationStatus.ToPrepare or TaxDeclarationStatus.InPreparation or TaxDeclarationStatus.Rejected)) ||
            (highRisk && declaration.DueDate <= today.AddDays(10)) ||
            MissingSubmissionProof(declaration) ||
            MissingPaymentProof(declaration);
    }

    private sealed record DashboardDeclarationRow(
        Guid Id,
        string ObligationName,
        string PeriodLabel,
        DateOnly DueDate,
        TaxDeclarationStatus Status,
        RiskLevel ObligationRiskLevel,
        RiskLevel PenaltyRiskLevel,
        bool PaymentRequired,
        Guid AssignedToUserId,
        string AssignedTo,
        bool IsCurrentUserResponsible,
        bool HasSubmissionProof,
        bool HasPaymentProof);
}
