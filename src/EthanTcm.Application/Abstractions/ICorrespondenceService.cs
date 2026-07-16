using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface ICorrespondenceService
{
    Task<CorrespondencePage> SearchAsync(CorrespondenceQuery query, CancellationToken cancellationToken = default);
    Task<CorrespondenceDetails?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorrespondenceDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CorrespondenceOption>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<CorrespondenceReferenceData> GetReferenceDataAsync(CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> CreateAsync(CorrespondenceCreateCommand command, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> RegisterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> AssignAsync(Guid id, Guid userId, DateOnly? dueDate, string? comment, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> AdvanceAsync(Guid id, CorrespondenceStatus target, string? comment, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> UploadAsync(CorrespondenceUploadCommand command, CancellationToken cancellationToken = default);
    Task<CorrespondenceDownload?> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public sealed record CorrespondenceQuery(string? Search, CorrespondenceDirection? Direction, CorrespondenceStatus? Status, bool MyItems, int Page = 1, int PageSize = 10);
public sealed record CorrespondenceCreateCommand(CorrespondenceDirection Direction, string Subject, string? BusinessReference, string? Summary, DateOnly CorrespondenceDate,
    CorrespondencePriority Priority, CorrespondenceConfidentiality Confidentiality, CorrespondenceChannel Channel,
    string? SenderName, string? SenderOrganization, string? RecipientName, string? RecipientOrganization,
    Guid CorrespondentOrganizationId, Guid InternalDepartmentId, Guid? ParentCorrespondenceId, Guid? TaxDeclarationId, Guid? TaxObligationId);
public sealed record CorrespondenceUploadCommand(Guid CorrespondenceId, CorrespondenceDocumentType Type, string FileName, string ContentType, long Length, Stream Content);
public sealed record CorrespondenceResult(bool Success, Guid? Id, string? Message);
public sealed record CorrespondenceOption(Guid Id, string Label);
public sealed record CorrespondenceReferenceData(IReadOnlyCollection<CorrespondenceOption> Organizations, IReadOnlyCollection<CorrespondenceOption> Departments);
public sealed record CorrespondenceListItem(Guid Id, string Reference, CorrespondenceDirection Direction, string Subject, string Counterparty,
    CorrespondenceStatus Status, CorrespondencePriority Priority, DateOnly CorrespondenceDate, DateOnly? DueDate, string? AssignedTo, bool IsOverdue);
public sealed record CorrespondencePage(IReadOnlyCollection<CorrespondenceListItem> Items, int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record CorrespondenceHistoryItem(string Action, CorrespondenceStatus? FromStatus, CorrespondenceStatus ToStatus, string? Comment, DateTimeOffset At, string Actor);
public sealed record CorrespondenceDocumentItem(Guid Id, CorrespondenceDocumentType Type, string FileName, long Size, DateTimeOffset UploadedAt);
public sealed record CorrespondenceDetails(Guid Id, string? Reference, string? BusinessReference, CorrespondenceDirection Direction, string Subject, string? Summary,
    CorrespondenceStatus Status, CorrespondencePriority Priority, CorrespondenceConfidentiality Confidentiality, CorrespondenceChannel Channel,
    DateOnly CorrespondenceDate, DateTimeOffset? ReceivedAt, DateTimeOffset? SentAt, DateOnly? DueDate,
    string? SenderName, string? SenderOrganization, string? RecipientName, string? RecipientOrganization,
    Guid? AssignedToUserId, string? AssignedTo, Guid? CorrespondentOrganizationId, Guid? InternalDepartmentId, string? InternalDepartment,
    Guid? ParentCorrespondenceId, Guid? TaxDeclarationId, Guid? TaxObligationId,
    IReadOnlyCollection<CorrespondenceHistoryItem> History, IReadOnlyCollection<CorrespondenceDocumentItem> Documents);
public sealed record CorrespondenceDashboard(int TotalOpen, int ReceivedToday, int Unassigned, int Overdue, int DueSoon, int AwaitingValidation,
    int ReadyToSend, decimal AverageProcessingDays, IReadOnlyCollection<CorrespondenceListItem> PriorityItems);
public sealed record CorrespondenceDownload(string FileName, string ContentType, string PhysicalPath);

public interface ICorrespondenceOrganizationService
{
    Task<IReadOnlyCollection<CorrespondenceOrganizationItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> SaveAsync(Guid? id, string code, string name, string? email, string? address, CancellationToken cancellationToken = default);
}
public sealed record CorrespondenceOrganizationItem(Guid Id, string Code, string Name, string? ContactEmail, string? Address, bool IsActive);
