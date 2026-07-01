using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(DashboardView view, CancellationToken cancellationToken = default);
    Task<DashboardMetricDetailsDto> GetMetricDetailsAsync(
        DashboardMetric metric,
        int page,
        int pageSize,
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

public sealed record DashboardDto(
    DashboardView ActiveView,
    DateOnly Today,
    DateTimeOffset GeneratedAt,
    DashboardMetricsDto Metrics,
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

public enum DashboardDetailTargetType
{
    TaxDeclaration = 0,
    TaxObligation = 1
}

public sealed record DashboardMetricDetailsDto(
    DashboardMetric Metric,
    string Title,
    string Definition,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyCollection<DashboardMetricDetailItemDto> Items)
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
