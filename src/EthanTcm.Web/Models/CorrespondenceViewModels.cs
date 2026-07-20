using System.ComponentModel.DataAnnotations;
using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EthanTcm.Web.Models;

public sealed class CorrespondenceIndexViewModel { public CorrespondenceDashboard Dashboard { get; init; } = null!; public CorrespondencePage Page { get; init; } = null!; public string? Search { get; init; } public CorrespondenceDirection? Direction { get; init; } public CorrespondenceStatus? Status { get; init; } public bool MyItems { get; init; } public Guid? AssignedToUserId { get; init; } public Guid? DepartmentId { get; init; } public CorrespondenceRiskFilter? Risk { get; init; } public IReadOnlyCollection<SelectListItem> Users { get; init; }=[]; public IReadOnlyCollection<SelectListItem> Departments { get; init; }=[]; }
public sealed class CorrespondenceCreateViewModel
{
    [Required] public CorrespondenceDirection Direction { get; set; } = CorrespondenceDirection.Incoming;
    [Required, StringLength(300)] public string Subject { get; set; } = string.Empty;
    [StringLength(100)] public string? BusinessReference { get; set; }
    [StringLength(2000)] public string? Summary { get; set; }
    [Required] public DateOnly CorrespondenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public CorrespondencePriority Priority { get; set; } = CorrespondencePriority.Normal;
    public CorrespondenceConfidentiality Confidentiality { get; set; } = CorrespondenceConfidentiality.Internal;
    public CorrespondenceChannel Channel { get; set; } = CorrespondenceChannel.Email;
    public string? SenderName { get; set; } public string? SenderOrganization { get; set; } public string? RecipientName { get; set; } public string? RecipientOrganization { get; set; }
    [Required] public Guid CorrespondentOrganizationId { get; set; }
    [Required] public Guid InternalDepartmentId { get; set; }
    public IReadOnlyCollection<SelectListItem> Organizations { get; set; } = [];
    public IReadOnlyCollection<SelectListItem> Departments { get; set; } = [];
    public Guid? ParentCorrespondenceId { get; set; } public Guid? TaxDeclarationId { get; set; } public Guid? TaxObligationId { get; set; }
}
public sealed class CorrespondenceDetailsViewModel { public CorrespondenceDetails Item { get; init; } = null!; public IReadOnlyCollection<SelectListItem> Users { get; init; } = []; public IReadOnlyCollection<CorrespondenceActionItem> FollowUpActions { get; init; } = []; }
public sealed class CorrespondenceActionViewModel { public Guid Id { get; set; } public Guid AssignedToUserId { get; set; } public DateOnly? DueDate { get; set; } public CorrespondenceStatus Target { get; set; } public string? Comment { get; set; } public string? FollowUpTitle { get; set; } public string? FollowUpDescription { get; set; } public CorrespondencePriority FollowUpPriority { get; set; } = CorrespondencePriority.Normal; public Guid? EscalationUserId { get; set; } public CorrespondenceDocumentType DocumentType { get; set; } public IFormFile? Upload { get; set; } }
public sealed class CorrespondenceOrganizationViewModel { public IReadOnlyCollection<CorrespondenceOrganizationItem> Items { get; set; } = []; public Guid? Id { get; set; } [Required, StringLength(40)] public string Code { get; set; } = string.Empty; [Required, StringLength(200)] public string Name { get; set; } = string.Empty; [EmailAddress] public string? ContactEmail { get; set; } public string? Address { get; set; } }
