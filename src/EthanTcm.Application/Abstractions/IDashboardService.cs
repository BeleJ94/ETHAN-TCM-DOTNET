using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(DashboardView view, CancellationToken cancellationToken = default);
    Task<DashboardMetricDetailsDto> GetMetricDetailsAsync(
        DashboardView view,
        DashboardMetric metric,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<DashboardMetricExportDto> ExportMetricDetailsAsync(
        DashboardView view,
        DashboardMetric metric,
        DashboardExportFormat format,
        CancellationToken cancellationToken = default);
    Task<DashboardMetricDetailsDto> GetChartSegmentDetailsAsync(
        DashboardView view,
        DashboardChartType chart,
        string segmentKey,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<DashboardMetricExportDto> ExportChartSegmentDetailsAsync(
        DashboardView view,
        DashboardChartType chart,
        string segmentKey,
        DashboardExportFormat format,
        CancellationToken cancellationToken = default);
}

public enum DashboardView
{
    MyTasks = 0,
    TeamTasks = 1,
    ManagementOverview = 2,
    LateItems = 3,
    ComplianceOverview = 4,
    PaymentPending = 5,
    LatePayments = 6,
    MissingPaymentProof = 7
}

public enum DashboardMetric
{
    DueToday = 0,
    DueSoon = 1,
    DueThisWeek = 2,
    DueThisMonth = 3,
    Late = 4,
    AwaitingApproval = 5,
    PendingPayments = 6,
    MissingSubmissionProof = 7,
    MissingPaymentProof = 8,
    ObligationsWithoutResponsible = 9,
    PenaltyRisk = 10
}

public enum DashboardExportFormat
{
    Excel = 0,
    Pdf = 1
}

public enum DashboardChartType
{
    DueDateBuckets = 0,
    StatusDistribution = 1,
    RiskDistribution = 2,
    OwnerWorkload = 3
}

public sealed record DashboardDto(
    DashboardView ActiveView,
    DateOnly Today,
    DateTimeOffset GeneratedAt,
    DashboardMetricsDto Metrics,
    DashboardChartsDto Charts,
    IReadOnlyCollection<DashboardDeclarationItemDto> Items,
    IReadOnlyCollection<DashboardObligationIssueDto> ObligationIssues);

public sealed record DashboardMetricsDto(
    int OpenDeclarations,
    decimal EvidenceComplianceRate,
    int DueToday,
    int DueInLessThanFiveDays,
    int DueThisWeek,
    int DueThisMonth,
    int Late,
    int AwaitingApproval,
    int PendingPayments,
    int MissingSubmissionProof,
    int MissingPaymentProof,
    int ObligationsWithoutResponsible,
    int PenaltyRisk);

public sealed record DashboardDeclarationItemDto(
    Guid Id,
    string ObligationName,
    string PeriodLabel,
    DateOnly DueDate,
    TaxDeclarationStatus Status,
    RiskLevel RiskLevel,
    bool PaymentRequired,
    string AssignedTo,
    string Issue,
    int DaysLate);

public sealed record DashboardObligationIssueDto(
    Guid Id,
    string Name,
    RiskLevel RiskLevel,
    string Issue);

public sealed record DashboardChartsDto(
    IReadOnlyCollection<DashboardChartPointDto> DueDateBuckets,
    IReadOnlyCollection<DashboardChartPointDto> StatusDistribution,
    IReadOnlyCollection<DashboardChartPointDto> RiskDistribution,
    IReadOnlyCollection<DashboardChartPointDto> OwnerWorkload);

public sealed record DashboardChartPointDto(
    string Key,
    string Label,
    int Value,
    decimal Percent,
    string Tone);

public enum DashboardDetailTargetType
{
    TaxDeclaration = 0,
    TaxObligation = 1
}

public sealed record DashboardMetricDetailsDto(
    DashboardView View,
    DashboardMetric Metric,
    string Title,
    string Definition,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyCollection<DashboardMetricDetailItemDto> Items,
    string DetailAction = "KpiDetails",
    string ExportAction = "ExportKpiDetails",
    DashboardChartType? Chart = null,
    string? SegmentKey = null)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed record DashboardMetricDetailItemDto(
    Guid Id,
    DashboardDetailTargetType TargetType,
    string Name,
    string? PeriodLabel,
    DateOnly? DueDate,
    string Status,
    RiskLevel RiskLevel,
    string? AssignedTo,
    string Issue,
    int DaysLate);

public sealed record DashboardMetricExportDto(
    string FileName,
    string ContentType,
    byte[] Content);
