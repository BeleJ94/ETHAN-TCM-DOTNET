using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Web.Models;
public sealed class CorrespondenceActionsViewModel { public CorrespondenceActionDashboard Dashboard { get; init; } = null!; public IReadOnlyCollection<CorrespondenceActionItem> Items { get; init; } = []; public bool MyActions { get; init; } public CorrespondenceActionStatus? Status { get; init; } }
public sealed class CorrespondenceActionCommandViewModel { public Guid Id { get; set; } public Guid CorrespondenceId { get; set; } public string Title { get; set; } = string.Empty; public string? Description { get; set; } public Guid AssignedToUserId { get; set; } public Guid? EscalationUserId { get; set; } public DateOnly DueDate { get; set; } public CorrespondencePriority Priority { get; set; } = CorrespondencePriority.Normal; public string? WaitingFor { get; set; } public DateOnly FollowUpDate { get; set; } public string? Result { get; set; } }
