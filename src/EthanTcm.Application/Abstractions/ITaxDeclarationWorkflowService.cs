using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface ITaxDeclarationWorkflowService
{
    Task<TaxDeclarationWorkflowDetailsDto?> GetAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TaxDeclarationWorkflowListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowPage> SearchAsync(
        TaxDeclarationWorkflowSearchCriteria criteria,
        CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> StartPreparationAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> SubmitForReviewAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> ApproveAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> RejectAsync(Guid taxDeclarationId, string comment, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> MarkSubmittedAsync(Guid taxDeclarationId, string submissionReference, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> MarkPaidAsync(Guid taxDeclarationId, decimal amount, string currency, string paymentReference, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> AttachDocumentAsync(TaxDeclarationDocumentCommand command, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> ReassignAsync(TaxDeclarationReassignmentCommand command, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> CloseAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default);
    Task<TaxDeclarationWorkflowResult> CancelAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default);
}

public sealed record TaxDeclarationWorkflowResult(
    bool Success,
    string? ErrorMessage);

public sealed record TaxDeclarationWorkflowListItemDto(
    Guid Id,
    string ObligationName,
    string PeriodLabel,
    DateOnly DueDate,
    DateOnly? ReminderDate,
    TaxDeclarationStatus Status,
    bool PaymentRequired,
    string AssignedTo,
    DateTimeOffset? PreparedAt,
    Guid? PreparedByUserId,
    string PreparedBy);

public sealed record TaxDeclarationWorkflowSearchCriteria(
    string? Search = null,
    TaxDeclarationStatus? Status = null,
    int Page = 1,
    int PageSize = 10);

public sealed record TaxDeclarationWorkflowPage(
    IReadOnlyCollection<TaxDeclarationWorkflowListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed record TaxDeclarationWorkflowDetailsDto(
    Guid Id,
    string ObligationName,
    string PeriodLabel,
    DateOnly DueDate,
    DateOnly? ReminderDate,
    TaxDeclarationStatus Status,
    bool PaymentRequired,
    Guid AssignedToUserId,
    string AssignedTo,
    DateTimeOffset? PreparedAt,
    Guid? PreparedByUserId,
    string PreparedBy,
    string? SubmissionReference,
    DateTimeOffset? SubmittedAt,
    Guid? SubmittedByUserId,
    string SubmittedBy,
    DateTimeOffset? ClosedAt,
    Guid? ClosedByUserId,
    string ClosedBy,
    string ExpectedSubmissionOwner,
    string ExpectedPaymentOwner,
    bool CanRecordPayment,
    string ExpectedClosureOwner,
    bool CanCloseDeclaration,
    IReadOnlyCollection<TaxDeclarationApprovalStepDto> ApprovalSteps,
    IReadOnlyCollection<TaxDeclarationApprovalDto> Approvals,
    IReadOnlyCollection<TaxDeclarationPaymentDto> Payments,
    IReadOnlyCollection<TaxDeclarationDocumentDto> Documents);

public enum TaxDeclarationApprovalStepStatus
{
    NotReached = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public sealed record TaxDeclarationApprovalStepDto(
    int Level,
    string Title,
    ResponsibleType ResponsibleType,
    IReadOnlyCollection<Guid> ExpectedApproverUserIds,
    string ExpectedApprovers,
    TaxDeclarationApprovalStepStatus Status,
    Guid? DecidedByUserId,
    string DecidedBy,
    string? Comment,
    DateTimeOffset? DecidedAt,
    bool CanAct);

public sealed record TaxDeclarationApprovalDto(
    int CycleNumber,
    int Level,
    Guid ApproverUserId,
    ApprovalDecision Decision,
    string? Comment,
    DateTimeOffset DecidedAt);

public sealed record TaxDeclarationPaymentDto(
    decimal Amount,
    string Currency,
    string PaymentReference,
    Guid? PaidByUserId,
    string PaidBy,
    DateTimeOffset PaidAt);

public sealed record TaxDeclarationDocumentDto(
    Guid Id,
    DocumentType DocumentType,
    string FileName,
    string FilePath,
    string ContentType,
    long FileSizeBytes,
    int Version,
    bool IsDeleted,
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId,
    string UploadedBy);

public sealed record TaxDeclarationDocumentCommand(
    Guid TaxDeclarationId,
    DocumentType DocumentType,
    string FileName,
    string FilePath,
    string ContentType);

public sealed record TaxDeclarationReassignmentCommand(
    Guid TaxDeclarationId,
    Guid AssignedToUserId,
    string Comment);
