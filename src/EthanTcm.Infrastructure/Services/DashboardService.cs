using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;

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
                    "No primary owner"))
                .Take(50)
                .ToArrayAsync(cancellationToken)
            : [];

        var scopedDeclarations = ApplyViewFilter(visibleDeclarations, view, today).ToArray();

        var items = scopedDeclarations
            .OrderByDescending(declaration => PenaltyRisk(declaration, today))
            .ThenBy(declaration => declaration.DueDate)
            .Take(75)
            .Select(declaration => ToItem(declaration, today))
            .ToArray();

        var evidenceCompliant = scopedDeclarations.Count(declaration =>
            !MissingSubmissionProof(declaration) && !MissingPaymentProof(declaration));
        var evidenceComplianceRate = scopedDeclarations.Length == 0
            ? 100m
            : decimal.Round(evidenceCompliant * 100m / scopedDeclarations.Length, 1);

        var metrics = new DashboardMetricsDto(
            scopedDeclarations.Length,
            evidenceComplianceRate,
            scopedDeclarations.Count(declaration => declaration.DueDate == today),
            scopedDeclarations.Count(declaration => IsDueInLessThanFiveDays(declaration, today)),
            scopedDeclarations.Count(declaration => declaration.DueDate >= startOfWeek && declaration.DueDate <= endOfWeek),
            scopedDeclarations.Count(declaration => declaration.DueDate >= startOfMonth && declaration.DueDate <= endOfMonth),
            scopedDeclarations.Count(declaration => IsLate(declaration, today)),
            scopedDeclarations.Count(declaration => ApprovalStatuses.Contains(declaration.Status)),
            scopedDeclarations.Count(declaration => declaration.Status == TaxDeclarationStatus.PaymentPending),
            scopedDeclarations.Count(MissingSubmissionProof),
            scopedDeclarations.Count(MissingPaymentProof),
            obligationsWithoutResponsible.Length,
            scopedDeclarations.Count(declaration => PenaltyRisk(declaration, today)));

        return new DashboardDto(
            view,
            today,
            DateTimeOffset.UtcNow,
            metrics,
            BuildCharts(scopedDeclarations, today),
            items,
            view == DashboardView.ComplianceOverview || view == DashboardView.ManagementOverview ? obligationsWithoutResponsible : []);
    }

    public Task<DashboardMetricDetailsDto> GetMetricDetailsAsync(
        DashboardView view,
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
        var cacheKey = $"dashboard-kpi:{identity}:{roles}:{view}:{metric}:{normalizedPage}:{normalizedPageSize}";

        return memoryCache.GetOrCreateAsync(
            cacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                entry.Size = 1;
                return BuildMetricDetailsAsync(
                    view,
                    metric,
                    normalizedPage,
                    normalizedPageSize,
                    cancellationToken);
            })!;
    }

    public async Task<DashboardMetricExportDto> ExportMetricDetailsAsync(
        DashboardView view,
        DashboardMetric metric,
        DashboardExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var (title, definition) = MetricMetadata(metric);
        var items = await GetExportItemsAsync(view, metric, cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;
        var safeTitle = Slug(title);

        return format switch
        {
            DashboardExportFormat.Pdf => new DashboardMetricExportDto(
                $"{safeTitle}_{generatedAt:yyyyMMdd_HHmm}.pdf",
                "application/pdf",
                BuildProfessionalPdfExport(title, definition, view, generatedAt, items)),
            _ => new DashboardMetricExportDto(
                $"{safeTitle}_{generatedAt:yyyyMMdd_HHmm}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                BuildExcelExport(title, definition, view, generatedAt, items))
        };
    }

    public async Task<DashboardMetricDetailsDto> GetChartSegmentDetailsAsync(
        DashboardView view,
        DashboardChartType chart,
        string segmentKey,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedPageSize = Math.Clamp(pageSize, 10, 50);
        var requestedPage = Math.Max(1, page);
        var (title, definition) = ChartSegmentMetadata(chart, segmentKey);
        var rows = ApplyChartFilter(
                ApplyViewFilter(await GetVisibleDeclarationRowsAsync(cancellationToken), view, today),
                chart,
                segmentKey,
                today)
            .OrderBy(declaration => declaration.DueDate)
            .ThenBy(declaration => declaration.ObligationName)
            .ThenBy(declaration => declaration.PeriodLabel)
            .ToArray();

        var totalCount = rows.Length;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
        var pageNumber = Math.Min(requestedPage, totalPages);
        var items = rows
            .Skip((pageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(row => ToDetailItem(row, ResolveIssue(row, today), today))
            .ToArray();

        return new DashboardMetricDetailsDto(
            view,
            DashboardMetric.PenaltyRisk,
            title,
            definition,
            totalCount,
            pageNumber,
            normalizedPageSize,
            items,
            "ChartDetails",
            "ExportChartDetails",
            chart,
            segmentKey);
    }

    public async Task<DashboardMetricExportDto> ExportChartSegmentDetailsAsync(
        DashboardView view,
        DashboardChartType chart,
        string segmentKey,
        DashboardExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (title, definition) = ChartSegmentMetadata(chart, segmentKey);
        var items = ApplyChartFilter(
                ApplyViewFilter(await GetVisibleDeclarationRowsAsync(cancellationToken), view, today),
                chart,
                segmentKey,
                today)
            .OrderBy(declaration => declaration.DueDate)
            .ThenBy(declaration => declaration.ObligationName)
            .ThenBy(declaration => declaration.PeriodLabel)
            .Select(row => ToDetailItem(row, ResolveIssue(row, today), today))
            .ToArray();
        var generatedAt = DateTimeOffset.UtcNow;
        var safeTitle = Slug(title);

        return format switch
        {
            DashboardExportFormat.Pdf => new DashboardMetricExportDto(
                $"{safeTitle}_{generatedAt:yyyyMMdd_HHmm}.pdf",
                "application/pdf",
                BuildProfessionalPdfExport(title, definition, view, generatedAt, items)),
            _ => new DashboardMetricExportDto(
                $"{safeTitle}_{generatedAt:yyyyMMdd_HHmm}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                BuildExcelExport(title, definition, view, generatedAt, items))
        };
    }

    private async Task<DashboardMetricDetailsDto> BuildMetricDetailsAsync(
        DashboardView view,
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
                view,
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
                view,
                title,
                definition,
                requestedPage,
                normalizedPageSize,
                today,
                cancellationToken);
        }

        var rows = ApplyMetricFilter(
                ApplyViewFilter(await GetVisibleDeclarationRowsAsync(cancellationToken), view, today),
                metric,
                today)
            .OrderBy(declaration => declaration.DueDate)
            .ThenBy(declaration => declaration.ObligationName)
            .ThenBy(declaration => declaration.PeriodLabel)
            .ToArray();

        var totalCount = rows.Length;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
        var pageNumber = Math.Min(requestedPage, totalPages);

        var pagedRows = rows
            .Skip((pageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();

        var issue = MetricIssue(metric);
        var items = pagedRows.Select(row => new DashboardMetricDetailItemDto(
                row.Id,
                DashboardDetailTargetType.TaxDeclaration,
                row.ObligationName,
                row.PeriodLabel,
                row.DueDate,
                row.Status.ToString(),
                row.ObligationRiskLevel > row.PenaltyRiskLevel ? row.ObligationRiskLevel : row.PenaltyRiskLevel,
                row.AssignedTo,
                issue,
                Math.Max(0, today.DayNumber - row.DueDate.DayNumber)))
            .ToArray();

        return new DashboardMetricDetailsDto(
            view,
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
        DashboardView view,
        string title,
        string definition,
        int requestedPage,
        int pageSize,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var rows = ApplyMetricFilter(
                ApplyViewFilter(await GetVisibleDeclarationRowsAsync(cancellationToken), view, today),
                metric,
                today)
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
            view,
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
        DashboardView view,
        string title,
        string definition,
        int requestedPage,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!CanViewGlobalDashboard() ||
            view is not (DashboardView.ManagementOverview or DashboardView.ComplianceOverview))
        {
            return new DashboardMetricDetailsDto(view, metric, title, definition, 0, 1, pageSize, []);
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
                "Missing owner",
                0))
            .ToArray();

        return new DashboardMetricDetailsDto(
            view,
            metric,
            title,
            definition,
            totalCount,
            pageNumber,
            pageSize,
            items);
    }

    private async Task<DashboardMetricDetailItemDto[]> GetExportItemsAsync(
        DashboardView view,
        DashboardMetric metric,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (metric == DashboardMetric.ObligationsWithoutResponsible)
        {
            if (!CanViewGlobalDashboard() ||
                view is not (DashboardView.ManagementOverview or DashboardView.ComplianceOverview))
            {
                return [];
            }

            return await dbContext.TaxObligations
                .AsNoTracking()
                .Where(obligation => obligation.IsActive)
                .Where(obligation => !dbContext.TaxObligationResponsibles.Any(responsible =>
                    responsible.TaxObligationId == obligation.Id &&
                    (responsible.Type == ResponsibleType.Primary || responsible.Type == ResponsibleType.Preparer)))
                .OrderByDescending(obligation => obligation.RiskLevel)
                .ThenBy(obligation => obligation.Name)
                .Select(obligation => new DashboardMetricDetailItemDto(
                    obligation.Id,
                    DashboardDetailTargetType.TaxObligation,
                    obligation.Name,
                    null,
                    null,
                    "Active",
                    obligation.RiskLevel,
                    null,
                    "Missing owner",
                    0))
                .ToArrayAsync(cancellationToken);
        }

        var issue = MetricIssue(metric);
        return ApplyMetricFilter(
                ApplyViewFilter(await GetVisibleDeclarationRowsAsync(cancellationToken), view, today),
                metric,
                today)
            .OrderBy(declaration => declaration.DueDate)
            .ThenBy(declaration => declaration.ObligationName)
            .ThenBy(declaration => declaration.PeriodLabel)
            .Select(row => new DashboardMetricDetailItemDto(
                row.Id,
                DashboardDetailTargetType.TaxDeclaration,
                row.ObligationName,
                row.PeriodLabel,
                row.DueDate,
                row.Status.ToString(),
                row.ObligationRiskLevel > row.PenaltyRiskLevel ? row.ObligationRiskLevel : row.PenaltyRiskLevel,
                row.AssignedTo,
                issue,
                Math.Max(0, today.DayNumber - row.DueDate.DayNumber)))
            .ToArray();
    }

    private static byte[] BuildExcelExport(
        string title,
        string definition,
        DashboardView view,
        DateTimeOffset generatedAt,
        IReadOnlyCollection<DashboardMetricDetailItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("KPI details");

        worksheet.Cell(1, 1).Value = title;
        worksheet.Range(1, 1, 1, 8).Merge().Style
            .Font.SetBold()
            .Font.SetFontSize(16)
            .Font.SetFontColor(XLColor.FromHtml("#16202D"));

        worksheet.Cell(2, 1).Value = definition;
        worksheet.Range(2, 1, 2, 8).Merge().Style
            .Font.SetFontColor(XLColor.FromHtml("#445064"));

        worksheet.Cell(4, 1).Value = "View";
        worksheet.Cell(4, 2).Value = ViewLabel(view);
        worksheet.Cell(4, 4).Value = "Extraction";
        worksheet.Cell(4, 5).Value = generatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        worksheet.Cell(4, 7).Value = "Total";
        worksheet.Cell(4, 8).Value = items.Count;
        worksheet.Range(4, 1, 4, 8).Style.Font.SetBold();

        var headers = new[] { "Tax", "Period", "Deadline", "Status", "Risk", "Owner", "Expected action", "Days late" };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(6, index + 1).Value = headers[index];
        }

        var headerRange = worksheet.Range(6, 1, 6, 8);
        headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1F6692"));
        headerRange.Style.Font.SetFontColor(XLColor.White).Font.SetBold();
        headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var rowIndex = 7;
        foreach (var item in items)
        {
            worksheet.Cell(rowIndex, 1).Value = item.Name;
            worksheet.Cell(rowIndex, 2).Value = item.PeriodLabel ?? "-";
            worksheet.Cell(rowIndex, 3).Value = item.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            worksheet.Cell(rowIndex, 4).Value = item.Status;
            worksheet.Cell(rowIndex, 5).Value = item.RiskLevel.ToString();
            worksheet.Cell(rowIndex, 6).Value = item.AssignedTo ?? "Unassigned";
            worksheet.Cell(rowIndex, 7).Value = item.Issue;
            worksheet.Cell(rowIndex, 8).Value = item.DaysLate;
            rowIndex++;
        }

        var tableRange = worksheet.Range(6, 1, Math.Max(6, rowIndex - 1), 8);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#C9D8E7");
        tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D8E1EB");
        worksheet.Range(7, 1, Math.Max(7, rowIndex - 1), 8).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);
        worksheet.Column(1).Width = 32;
        worksheet.Column(2).Width = 14;
        worksheet.Column(3).Width = 14;
        worksheet.Column(4).Width = 18;
        worksheet.Column(5).Width = 12;
        worksheet.Column(6).Width = 24;
        worksheet.Column(7).Width = 28;
        worksheet.Column(8).Width = 10;
        worksheet.RangeUsed()?.SetAutoFilter();
        worksheet.SheetView.FreezeRows(6);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildProfessionalPdfExport(
        string title,
        string definition,
        DashboardView view,
        DateTimeOffset generatedAt,
        IReadOnlyCollection<DashboardMetricDetailItemDto> items)
    {
        var pages = new List<string>();
        var page = new StringBuilder();
        var pageNumber = 0;
        var y = 535;
        var columns = new[]
        {
            new ExportPdfColumn(36, 174, "DECLARATION", "Tax / obligation"),
            new ExportPdfColumn(210, 70, "PERIOD", "Tax cycle"),
            new ExportPdfColumn(280, 76, "DEADLINE", "Due date"),
            new ExportPdfColumn(356, 90, "STATUS", "Workflow"),
            new ExportPdfColumn(446, 58, "RISK", "Level"),
            new ExportPdfColumn(504, 128, "OWNER", "Current owner"),
            new ExportPdfColumn(632, 138, "ACTION", "Expected action"),
            new ExportPdfColumn(770, 36, "LATE", "Days")
        };

        void NewPage()
        {
            if (page.Length > 0)
            {
                pages.Add(page.ToString());
                page.Clear();
            }

            pageNumber++;
            y = 535;

            ExportPdfFillRect(page, 0, 0, 842, 595, "0.98 0.99 1 rg");
            ExportPdfFillRect(page, 28, 548, 786, 24, "0.05 0.22 0.36 rg");
            ExportPdfText(page, 40, 556, 13, true, title, "1 1 1 rg");

            ExportPdfText(page, 40, 528, 8, true, "View");
            ExportPdfText(page, 80, 528, 8, false, ViewLabel(view));
            ExportPdfText(page, 210, 528, 8, true, "Extraction");
            ExportPdfText(page, 286, 528, 8, false, generatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            ExportPdfText(page, 430, 528, 8, true, "Total");
            ExportPdfText(page, 470, 528, 8, false, items.Count.ToString(CultureInfo.InvariantCulture));
            ExportPdfText(page, 650, 528, 8, false, $"Page {pageNumber}");

            foreach (var line in ExportPdfWrap(definition, 128).Take(2))
            {
                ExportPdfText(page, 40, y, 8, false, line, "0.20 0.29 0.38 rg");
                y -= 11;
            }

            y -= 10;
            ExportPdfTableHeader(page, columns, y);
            y -= 38;
        }

        NewPage();
        var rowIndex = 0;
        foreach (var item in items)
        {
            var nameLines = ExportPdfWrap(item.Name, 30).Take(2).DefaultIfEmpty("-").ToArray();
            var ownerLines = ExportPdfWrap(item.AssignedTo ?? "Unassigned", 21).Take(2).DefaultIfEmpty("-").ToArray();
            var issueLines = ExportPdfWrap(item.Issue, 24).Take(2).DefaultIfEmpty("-").ToArray();
            var lineCount = new[] { nameLines.Length, ownerLines.Length, issueLines.Length }.Max();
            var rowHeight = Math.Max(26, 13 + (lineCount * 10));

            if (y - rowHeight < 38)
            {
                NewPage();
            }

            var rowTop = y;
            var fill = rowIndex % 2 == 0 ? "1 1 1 rg" : "0.94 0.97 0.99 rg";
            ExportPdfFillRect(page, 36, rowTop - rowHeight + 5, 770, rowHeight, fill);
            ExportPdfStrokeRect(page, 36, rowTop - rowHeight + 5, 770, rowHeight, "0.78 0.84 0.90 RG");

            ExportPdfWrappedCell(page, columns[0], rowTop - 9, nameLines);
            ExportPdfText(page, columns[1].X + 6, rowTop - 9, 7, false, item.PeriodLabel ?? "-");
            ExportPdfText(page, columns[2].X + 6, rowTop - 9, 7, false, item.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-");
            ExportPdfText(page, columns[3].X + 6, rowTop - 9, 7, false, ExportPdfTruncate(item.Status, 18));
            ExportPdfText(page, columns[4].X + 6, rowTop - 9, 7, false, item.RiskLevel.ToString());
            ExportPdfWrappedCell(page, columns[5], rowTop - 9, ownerLines);
            ExportPdfWrappedCell(page, columns[6], rowTop - 9, issueLines);
            ExportPdfText(page, columns[7].X + 10, rowTop - 9, 7, true, item.DaysLate > 0 ? item.DaysLate.ToString(CultureInfo.InvariantCulture) : "-");

            foreach (var column in columns.Skip(1))
            {
                ExportPdfLine(page, column.X, rowTop + 5, column.X, rowTop - rowHeight + 5, "0.86 0.90 0.94 RG");
            }

            y -= rowHeight;
            rowIndex++;
        }

        pages.Add(page.ToString());
        return ExportPdfDocument(pages);
    }

    private static void ExportPdfTableHeader(StringBuilder builder, IReadOnlyList<ExportPdfColumn> columns, int y)
    {
        const int headerHeight = 34;
        ExportPdfFillRect(builder, 36, y - 15, 770, headerHeight, "0.04 0.20 0.33 rg");
        ExportPdfFillRect(builder, 36, y + 15, 770, 4, "0.11 0.45 0.67 rg");
        ExportPdfLine(builder, 36, y - 15, 806, y - 15, "0.52 0.64 0.75 RG");

        foreach (var column in columns)
        {
            ExportPdfText(builder, column.X + 6, y + 5, 8, true, column.Header, "1 1 1 rg");
            ExportPdfText(builder, column.X + 6, y - 7, 5, false, column.SubHeader, "0.78 0.88 0.96 rg");
        }

        foreach (var column in columns.Skip(1))
        {
            ExportPdfLine(builder, column.X, y + 15, column.X, y - 15, "0.80 0.90 0.98 RG");
        }
    }

    private static void ExportPdfWrappedCell(StringBuilder builder, ExportPdfColumn column, int y, IReadOnlyList<string> lines)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            ExportPdfText(builder, column.X + 6, y - (index * 10), 7, false, ExportPdfTruncate(lines[index], Math.Max(8, column.Width / 6)));
        }
    }

    private static void ExportPdfText(StringBuilder builder, int x, int y, int size, bool bold, string text, string color = "0.05 0.10 0.16 rg")
    {
        builder.Append(color).Append('\n')
            .Append("BT /")
            .Append(bold ? "F2" : "F1")
            .Append(' ')
            .Append(size)
            .Append(" Tf ")
            .Append(x)
            .Append(' ')
            .Append(y)
            .Append(" Td ")
            .Append(ExportPdfLiteral(text))
            .Append(" Tj ET\n");
    }

    private static void ExportPdfLine(StringBuilder builder, int x1, int y1, int x2, int y2, string color = "0.70 0.76 0.82 RG")
    {
        builder.Append(color).Append('\n')
            .Append(x1).Append(' ').Append(y1).Append(" m ")
            .Append(x2).Append(' ').Append(y2).Append(" l S\n");
    }

    private static void ExportPdfFillRect(StringBuilder builder, int x, int y, int width, int height, string color)
    {
        builder.Append(color).Append('\n')
            .Append(x).Append(' ').Append(y).Append(' ').Append(width).Append(' ').Append(height).Append(" re f\n");
    }

    private static void ExportPdfStrokeRect(StringBuilder builder, int x, int y, int width, int height, string color)
    {
        builder.Append(color).Append('\n')
            .Append(x).Append(' ').Append(y).Append(' ').Append(width).Append(' ').Append(height).Append(" re S\n");
    }

    private static byte[] ExportPdfDocument(IReadOnlyList<string> pageContents)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
        };
        var kids = new List<string>();
        foreach (var content in pageContents)
        {
            var pageObjectNumber = objects.Count + 1;
            var contentObjectNumber = objects.Count + 2;
            kids.Add($"{pageObjectNumber} 0 R");
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', kids)}] /Count {kids.Count} >>";

        var output = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(index + 1).Append(" 0 obj\n")
                .Append(objects[index])
                .Append("\nendobj\n");
        }

        var xref = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append("xref\n0 ").Append(objects.Count + 1).Append("\n")
            .Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            output.Append(offset.ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        output.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n")
            .Append("startxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string ExportPdfLiteral(string text)
    {
        return $"({ExportPdfEscape(ExportPdfSafe(text))})";
    }

    private static string ExportPdfEscape(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static string ExportPdfSafe(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                '\u2019' or '\u2018' => '\'',
                '\u201C' or '\u201D' => '"',
                '\u2013' or '\u2014' => '-',
                '\u2026' => '.',
                _ when character >= 32 && character <= 126 => character,
                _ => ' '
            });
        }

        return builder.ToString();
    }

    private static IEnumerable<string> ExportPdfWrap(string value, int maxLength)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        foreach (var word in words)
        {
            if (word.Length > maxLength)
            {
                if (line.Length > 0)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                for (var index = 0; index < word.Length; index += maxLength)
                {
                    yield return word.Substring(index, Math.Min(maxLength, word.Length - index));
                }

                continue;
            }

            if (line.Length > 0 && line.Length + word.Length + 1 > maxLength)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private static string ExportPdfTruncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, Math.Max(0, maxLength - 3)), "...");
    }

    private sealed record ExportPdfColumn(int X, int Width, string Header, string SubHeader);

    private static byte[] BuildPdfExport(
        string title,
        string definition,
        DashboardView view,
        DateTimeOffset generatedAt,
        IReadOnlyCollection<DashboardMetricDetailItemDto> items)
    {
        var pages = new List<string>();
        var current = new StringBuilder();
        var y = 800;

        void NewPage()
        {
            if (current.Length > 0)
            {
                pages.Add(current.ToString());
                current.Clear();
            }

            y = 800;
            PdfText(current, 40, y, 16, true, title);
            y -= 22;
            foreach (var line in Wrap(definition, 96).Take(3))
            {
                PdfText(current, 40, y, 9, false, line);
                y -= 12;
            }

            y -= 6;
            PdfText(current, 40, y, 9, true, $"View: {ViewLabel(view)}");
            PdfText(current, 250, y, 9, true, $"Extraction: {generatedAt.ToLocalTime():yyyy-MM-dd HH:mm}");
            PdfText(current, 455, y, 9, true, $"Total: {items.Count}");
            y -= 22;
            PdfText(current, 40, y, 8, true, "Tax");
            PdfText(current, 190, y, 8, true, "Period");
            PdfText(current, 250, y, 8, true, "Deadline");
            PdfText(current, 310, y, 8, true, "Status");
            PdfText(current, 385, y, 8, true, "Risk");
            PdfText(current, 435, y, 8, true, "Action");
            y -= 12;
            PdfLine(current, 40, y + 7, 555, y + 7);
        }

        NewPage();
        foreach (var item in items)
        {
            if (y < 58)
            {
                NewPage();
            }

            PdfText(current, 40, y, 8, false, Truncate(item.Name, 34));
            PdfText(current, 190, y, 8, false, Truncate(item.PeriodLabel ?? "-", 12));
            PdfText(current, 250, y, 8, false, item.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-");
            PdfText(current, 310, y, 8, false, Truncate(item.Status, 15));
            PdfText(current, 385, y, 8, false, item.RiskLevel.ToString());
            PdfText(current, 435, y, 8, false, Truncate(item.Issue, 24));
            y -= 14;
            if (!string.IsNullOrWhiteSpace(item.AssignedTo) || item.DaysLate > 0)
            {
                var meta = $"Owner: {item.AssignedTo ?? "Unassigned"}";
                if (item.DaysLate > 0)
                {
                    meta += $" | Late: {item.DaysLate} day(s)";
                }

                PdfText(current, 52, y, 7, false, Truncate(meta, 95));
                y -= 12;
            }
        }

        pages.Add(current.ToString());
        return PdfDocument(pages);
    }

    private static void PdfText(StringBuilder builder, int x, int y, int size, bool bold, string text)
    {
        builder.Append("BT /")
            .Append(bold ? "F2" : "F1")
            .Append(' ')
            .Append(size)
            .Append(" Tf ")
            .Append(x)
            .Append(' ')
            .Append(y)
            .Append(" Td ")
            .Append(PdfHex(text))
            .Append(" Tj ET\n");
    }

    private static void PdfLine(StringBuilder builder, int x1, int y1, int x2, int y2)
    {
        builder.Append(x1).Append(' ').Append(y1).Append(" m ")
            .Append(x2).Append(' ').Append(y2).Append(" l S\n");
    }

    private static byte[] PdfDocument(IReadOnlyList<string> pageContents)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
        };
        var kids = new List<string>();
        foreach (var content in pageContents)
        {
            var pageObjectNumber = objects.Count + 1;
            var contentObjectNumber = objects.Count + 2;
            kids.Add($"{pageObjectNumber} 0 R");
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', kids)}] /Count {kids.Count} >>";

        var output = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(output.ToString()));
            output.Append(index + 1).Append(" 0 obj\n")
                .Append(objects[index])
                .Append("\nendobj\n");
        }

        var xref = Encoding.ASCII.GetByteCount(output.ToString());
        output.Append("xref\n0 ").Append(objects.Count + 1).Append("\n")
            .Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            output.Append(offset.ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        output.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n")
            .Append("startxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string PdfHex(string text)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes("\uFEFF" + text);
        return $"<{Convert.ToHexString(bytes)}>";
    }

    private static IEnumerable<string> Wrap(string value, int maxLength)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > maxLength)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 1), "…");
    }

    private static string Slug(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_');
        }

        return string.Join('_', builder.ToString().Split('_', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ViewLabel(DashboardView view) => view switch
    {
        DashboardView.MyTasks => "My tasks",
        DashboardView.TeamTasks => "Team",
        DashboardView.ManagementOverview => "Management overview",
        DashboardView.LateItems => "Late items",
        DashboardView.ComplianceOverview => "Compliance",
        DashboardView.PaymentPending => "Pending payments",
        DashboardView.LatePayments => "Late payments",
        DashboardView.MissingPaymentProof => "Missing payment evidence",
        _ => view.ToString()
    };


    private IEnumerable<DashboardDeclarationRow> ApplyViewFilter(
        IEnumerable<DashboardDeclarationRow> declarations,
        DashboardView view,
        DateOnly today)
    {
        return view switch
        {
            DashboardView.MyTasks => declarations.Where(IsActionableForCurrentUser),
            DashboardView.TeamTasks => declarations,
            DashboardView.ManagementOverview => declarations,
            DashboardView.LateItems => declarations.Where(declaration => IsLate(declaration, today)),
            DashboardView.ComplianceOverview => declarations.Where(declaration =>
                MissingSubmissionProof(declaration) ||
                MissingPaymentProof(declaration) ||
                PenaltyRisk(declaration, today)),
            DashboardView.PaymentPending => declarations.Where(declaration => declaration.Status == TaxDeclarationStatus.PaymentPending),
            DashboardView.LatePayments => declarations.Where(declaration =>
                declaration.PaymentRequired &&
                declaration.Status == TaxDeclarationStatus.PaymentPending &&
                declaration.DueDate < today),
            DashboardView.MissingPaymentProof => declarations.Where(MissingPaymentProof),
            _ => declarations.Where(IsActionableForCurrentUser)
        };
    }

    private static IEnumerable<DashboardDeclarationRow> ApplyMetricFilter(
        IEnumerable<DashboardDeclarationRow> declarations,
        DashboardMetric metric,
        DateOnly today)
    {
        var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return metric switch
        {
            DashboardMetric.DueToday => declarations.Where(declaration => declaration.DueDate == today),
            DashboardMetric.DueSoon => declarations.Where(declaration => IsDueInLessThanFiveDays(declaration, today)),
            DashboardMetric.DueThisWeek => declarations.Where(declaration =>
                declaration.DueDate >= startOfWeek && declaration.DueDate <= endOfWeek),
            DashboardMetric.DueThisMonth => declarations.Where(declaration =>
                declaration.DueDate >= startOfMonth && declaration.DueDate <= endOfMonth),
            DashboardMetric.Late => declarations.Where(declaration => IsLate(declaration, today)),
            DashboardMetric.AwaitingApproval => declarations.Where(declaration => ApprovalStatuses.Contains(declaration.Status)),
            DashboardMetric.PendingPayments => declarations.Where(declaration => declaration.Status == TaxDeclarationStatus.PaymentPending),
            DashboardMetric.MissingSubmissionProof => declarations.Where(MissingSubmissionProof),
            DashboardMetric.MissingPaymentProof => declarations.Where(MissingPaymentProof),
            DashboardMetric.PenaltyRisk => declarations.Where(declaration => PenaltyRisk(declaration, today)),
            _ => declarations
        };
    }

    private static IEnumerable<DashboardDeclarationRow> ApplyChartFilter(
        IEnumerable<DashboardDeclarationRow> declarations,
        DashboardChartType chart,
        string segmentKey,
        DateOnly today)
    {
        var normalizedKey = segmentKey.Trim();
        var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return chart switch
        {
            DashboardChartType.DueDateBuckets => normalizedKey switch
            {
                "late" => declarations.Where(declaration => IsLate(declaration, today)),
                "today" => declarations.Where(declaration => !IsLate(declaration, today) && declaration.DueDate == today),
                "due-soon" => declarations.Where(declaration => IsDueInLessThanFiveDays(declaration, today)),
                "this-week" => declarations.Where(declaration =>
                    declaration.DueDate >= today.AddDays(5) &&
                    declaration.DueDate <= endOfWeek),
                "this-month" => declarations.Where(declaration =>
                    declaration.DueDate > endOfWeek &&
                    declaration.DueDate <= endOfMonth),
                "later" => declarations.Where(declaration => declaration.DueDate > endOfMonth),
                _ => []
            },
            DashboardChartType.StatusDistribution => Enum.TryParse<TaxDeclarationStatus>(normalizedKey, true, out var status)
                ? declarations.Where(declaration => declaration.Status == status)
                : [],
            DashboardChartType.RiskDistribution => Enum.TryParse<RiskLevel>(normalizedKey, true, out var risk)
                ? declarations.Where(declaration => EffectiveRisk(declaration) == risk)
                : [],
            DashboardChartType.OwnerWorkload => declarations.Where(declaration =>
                declaration.AssignedTo.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase)),
            _ => []
        };
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
        DashboardMetric.DueToday => ("Due today", "Open declarations due today."),
        DashboardMetric.DueSoon => ("Due within 5 days", "Open declarations due in the next four days, excluding today."),
        DashboardMetric.DueThisWeek => ("Due this week", "Open declarations due during the current calendar week."),
        DashboardMetric.DueThisMonth => ("Due this month", "Open declarations due during the current calendar month."),
        DashboardMetric.Late => ("Late declarations", "Open declarations past their deadline that have not been submitted or paid."),
        DashboardMetric.AwaitingApproval => ("Pending approvals", "Declarations currently in an approval stage."),
        DashboardMetric.PendingPayments => ("Pending payments", "Submitted declarations for which payment remains outstanding."),
        DashboardMetric.MissingSubmissionProof => ("Missing submission evidence", "Submitted or paid declarations without active submission evidence."),
        DashboardMetric.MissingPaymentProof => ("Missing payment evidence", "Payable or paid declarations without active payment evidence."),
        DashboardMetric.ObligationsWithoutResponsible => ("Taxes without an owner", "Active tax obligations without a preparer or primary owner."),
        DashboardMetric.PenaltyRisk => ("Penalty exposure", "Late items, unprepared upcoming deadlines, high risks or missing regulatory evidence."),
        _ => ("KPI details", "Items included in this indicator.")
    };

    private static string MetricIssue(DashboardMetric metric) => metric switch
    {
        DashboardMetric.DueToday => "Action required today",
        DashboardMetric.DueSoon => "Upcoming deadline",
        DashboardMetric.DueThisWeek => "Plan this week",
        DashboardMetric.DueThisMonth => "Plan this month",
        DashboardMetric.Late => "Late",
        DashboardMetric.AwaitingApproval => "Approval required",
        DashboardMetric.PendingPayments => "Payment required",
        DashboardMetric.MissingSubmissionProof => "Missing submission evidence",
        DashboardMetric.MissingPaymentProof => "Missing payment evidence",
        DashboardMetric.PenaltyRisk => "Penalty risk",
        _ => "Review required"
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

    private static DashboardMetricDetailItemDto ToDetailItem(
        DashboardDeclarationRow row,
        string issue,
        DateOnly today)
    {
        return new DashboardMetricDetailItemDto(
            row.Id,
            DashboardDetailTargetType.TaxDeclaration,
            row.ObligationName,
            row.PeriodLabel,
            row.DueDate,
            row.Status.ToString(),
            EffectiveRisk(row),
            row.AssignedTo,
            issue,
            Math.Max(0, today.DayNumber - row.DueDate.DayNumber));
    }

    private static (string Title, string Definition) ChartSegmentMetadata(DashboardChartType chart, string segmentKey)
    {
        var label = ChartSegmentLabel(chart, segmentKey);
        return chart switch
        {
            DashboardChartType.DueDateBuckets => ($"Deadline - {label}", "Declarations within the active scope matching this deadline range."),
            DashboardChartType.StatusDistribution => ($"Status - {label}", "Declarations within the active scope with this workflow status."),
            DashboardChartType.RiskDistribution => ($"Risk - {label}", "Declarations within the active scope matching this risk level."),
            DashboardChartType.OwnerWorkload => ($"Owner - {label}", "Open declarations currently assigned to this owner."),
            _ => (label, "Details du segment selectionne.")
        };
    }

    private static string ChartSegmentLabel(DashboardChartType chart, string segmentKey)
    {
        return chart switch
        {
            DashboardChartType.DueDateBuckets => segmentKey switch
            {
                "late" => "Late",
                "today" => "Today",
                "due-soon" => "Within 5 days",
                "this-week" => "This week",
                "this-month" => "This month",
                "later" => "Later",
                _ => segmentKey
            },
            DashboardChartType.StatusDistribution when Enum.TryParse<TaxDeclarationStatus>(segmentKey, true, out var status) => StatusLabel(status),
            DashboardChartType.RiskDistribution when Enum.TryParse<RiskLevel>(segmentKey, true, out var risk) => risk.ToString(),
            _ => string.IsNullOrWhiteSpace(segmentKey) ? "Segment" : segmentKey
        };
    }

    private static DashboardChartsDto BuildCharts(
        IReadOnlyCollection<DashboardDeclarationRow> declarations,
        DateOnly today)
    {
        var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var dueBuckets = ToChartPoints(
            [
                new DashboardChartSeed("late", "Late", declarations.Count(declaration => IsLate(declaration, today)), "danger"),
                new DashboardChartSeed("today", "Today", declarations.Count(declaration => !IsLate(declaration, today) && declaration.DueDate == today), "warning"),
                new DashboardChartSeed("due-soon", "Within 5 days", declarations.Count(declaration => IsDueInLessThanFiveDays(declaration, today)), "warning"),
                new DashboardChartSeed("this-week", "This week", declarations.Count(declaration =>
                    declaration.DueDate >= today.AddDays(5) &&
                    declaration.DueDate <= endOfWeek), "info"),
                new DashboardChartSeed("this-month", "This month", declarations.Count(declaration =>
                    declaration.DueDate > endOfWeek &&
                    declaration.DueDate <= endOfMonth), "info"),
                new DashboardChartSeed("later", "Later", declarations.Count(declaration => declaration.DueDate > endOfMonth), "neutral")
            ]);

        var statusDistribution = declarations.Count == 0
            ? ToChartPoints([new DashboardChartSeed("empty", "No items", 0, "neutral")])
            : ToChartPoints(declarations
                .GroupBy(declaration => declaration.Status)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.ToString())
                .Select(group => new DashboardChartSeed(group.Key.ToString(), StatusLabel(group.Key), group.Count(), StatusTone(group.Key)))
                .ToArray());

        var riskDistribution = ToChartPoints(
            Enum.GetValues<RiskLevel>()
                .Select(risk => new DashboardChartSeed(
                    risk.ToString(),
                    risk.ToString(),
                    declarations.Count(declaration => EffectiveRisk(declaration) == risk),
                    RiskTone(risk)))
                .OrderByDescending(point => point.Value)
                .ToArray());

        var ownerWorkload = ToChartPoints(
            declarations
                .GroupBy(declaration => declaration.AssignedTo)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Take(5)
                .Select(group => new DashboardChartSeed(group.Key, group.Key, group.Count(), "neutral"))
                .ToArray());

        return new DashboardChartsDto(dueBuckets, statusDistribution, riskDistribution, ownerWorkload);
    }

    private static IReadOnlyCollection<DashboardChartPointDto> ToChartPoints(IReadOnlyCollection<DashboardChartSeed> seeds)
    {
        var max = Math.Max(1, seeds.Count == 0 ? 0 : seeds.Max(seed => seed.Value));
        return seeds
            .Select(seed => new DashboardChartPointDto(
                seed.Key,
                seed.Label,
                seed.Value,
                decimal.Round(seed.Value * 100m / max, 1),
                seed.Tone))
            .ToArray();
    }

    private static RiskLevel EffectiveRisk(DashboardDeclarationRow declaration)
    {
        return declaration.ObligationRiskLevel > declaration.PenaltyRiskLevel
            ? declaration.ObligationRiskLevel
            : declaration.PenaltyRiskLevel;
    }

    private static string StatusLabel(TaxDeclarationStatus status) => status switch
    {
        TaxDeclarationStatus.ToPrepare => "To prepare",
        TaxDeclarationStatus.InPreparation => "In preparation",
        TaxDeclarationStatus.SubmittedForReview => "Under review",
        TaxDeclarationStatus.ApprovedLevel1 => "Level 1 approved",
        TaxDeclarationStatus.ApprovedLevel2 => "Level 2 approved",
        TaxDeclarationStatus.ApprovedLevel3 => "Level 3 approved",
        TaxDeclarationStatus.ReadyForSubmission => "Ready for submission",
        TaxDeclarationStatus.Rejected => "Rejected",
        TaxDeclarationStatus.Submitted => "Submitted",
        TaxDeclarationStatus.PaymentPending => "Payment pending",
        TaxDeclarationStatus.Paid => "Paid",
        TaxDeclarationStatus.Late => "Late",
        TaxDeclarationStatus.Closed => "Closed",
        TaxDeclarationStatus.Cancelled => "Cancelled",
        TaxDeclarationStatus.NotApplicable => "Non applicable",
        _ => status.ToString()
    };

    private static string StatusTone(TaxDeclarationStatus status) => status switch
    {
        TaxDeclarationStatus.Rejected or TaxDeclarationStatus.Late => "danger",
        TaxDeclarationStatus.PaymentPending => "warning",
        TaxDeclarationStatus.SubmittedForReview or TaxDeclarationStatus.ApprovedLevel1 or TaxDeclarationStatus.ApprovedLevel2 or TaxDeclarationStatus.ApprovedLevel3 or TaxDeclarationStatus.ReadyForSubmission => "info",
        TaxDeclarationStatus.Paid or TaxDeclarationStatus.Submitted or TaxDeclarationStatus.Closed => "success",
        _ => "neutral"
    };

    private static string RiskTone(RiskLevel risk) => risk switch
    {
        RiskLevel.Critical or RiskLevel.High => "danger",
        RiskLevel.Medium => "warning",
        RiskLevel.Low => "success",
        _ => "neutral"
    };

    private static string ResolveIssue(DashboardDeclarationRow declaration, DateOnly today)
    {
        if (IsLate(declaration, today))
        {
            return "Late";
        }

        if (ApprovalStatuses.Contains(declaration.Status))
        {
            return "Approval required";
        }

        if (declaration.Status == TaxDeclarationStatus.PaymentPending)
        {
            return "Payment pending";
        }

        if (MissingSubmissionProof(declaration))
        {
            return "Missing submission evidence";
        }

        if (MissingPaymentProof(declaration))
        {
            return "Missing payment evidence";
        }

        if (declaration.DueDate == today)
        {
            return "Due today";
        }

        if (IsDueInLessThanFiveDays(declaration, today))
        {
            return "Due within 5 days";
        }

        return "Upcoming";
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

    private sealed record DashboardChartSeed(string Key, string Label, int Value, string Tone);
}
