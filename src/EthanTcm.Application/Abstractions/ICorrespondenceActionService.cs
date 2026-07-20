using EthanTcm.Domain.Enums;

namespace EthanTcm.Application.Abstractions;

public interface ICorrespondenceActionService
{
    Task<CorrespondenceActionDashboard> GetDashboardAsync(bool myActions, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CorrespondenceActionItem>> ListAsync(bool myActions, CorrespondenceActionStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CorrespondenceActionItem>> ListForCorrespondenceAsync(Guid correspondenceId, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> CreateAsync(CorrespondenceActionCreateCommand command, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> WaitAsync(Guid id, string waitingFor, DateOnly followUpDate, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> ResumeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> CompleteAsync(Guid id, string result, CancellationToken cancellationToken = default);
    Task<CorrespondenceResult> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default);
}
public sealed record CorrespondenceActionCreateCommand(Guid CorrespondenceId, string Title, string? Description, Guid AssignedToUserId, DateOnly DueDate, CorrespondencePriority Priority, Guid? EscalationUserId);
public sealed record CorrespondenceActionItem(Guid Id, Guid CorrespondenceId, string CorrespondenceReference, string CorrespondenceSubject, string Title, string? Description,
    Guid AssignedToUserId, string AssignedTo, DateOnly DueDate, DateOnly? FollowUpDate, CorrespondencePriority Priority, CorrespondenceActionStatus Status,
    string? WaitingFor, string? CompletionResult, bool IsOverdue, bool IsDueSoon, bool IsEscalated);
public sealed record CorrespondenceActionDashboard(int Open, int Overdue, int DueToday, int DueSoon, int Waiting, int Escalated, int UnassignedCorrespondences);
