using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class Correspondence : AuditableEntity
{
    private readonly List<CorrespondenceHistory> _history = [];
    private readonly List<CorrespondenceDocument> _documents = [];
    private Correspondence() { }

    public Correspondence(CorrespondenceDirection direction, string subject, string? businessReference, DateOnly correspondenceDate,
        CorrespondencePriority priority, CorrespondenceConfidentiality confidentiality, CorrespondenceChannel channel,
        string? senderName, string? senderOrganization, string? recipientName, string? recipientOrganization, string? summary)
    {
        Direction = direction; BusinessReference = businessReference?.Trim();
        Subject = EntityGuards.Required(subject, nameof(subject));
        CorrespondenceDate = correspondenceDate;
        Priority = priority; Confidentiality = confidentiality; Channel = channel;
        SenderName = senderName?.Trim(); SenderOrganization = senderOrganization?.Trim();
        RecipientName = recipientName?.Trim(); RecipientOrganization = recipientOrganization?.Trim();
        Summary = summary?.Trim(); Status = CorrespondenceStatus.Draft;
    }

    public string? ReferenceNumber { get; private set; }
    public string? BusinessReference { get; private set; }
    public CorrespondenceDirection Direction { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public CorrespondenceStatus Status { get; private set; }
    public CorrespondencePriority Priority { get; private set; }
    public CorrespondenceConfidentiality Confidentiality { get; private set; }
    public CorrespondenceChannel Channel { get; private set; }
    public DateOnly CorrespondenceDate { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string? SenderName { get; private set; }
    public string? SenderOrganization { get; private set; }
    public string? RecipientName { get; private set; }
    public string? RecipientOrganization { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid? CorrespondentOrganizationId { get; private set; }
    public Guid? InternalDepartmentId { get; private set; }
    public Guid? ParentCorrespondenceId { get; private set; }
    public Guid? TaxDeclarationId { get; private set; }
    public Guid? TaxObligationId { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public IReadOnlyCollection<CorrespondenceHistory> History => _history;
    public IReadOnlyCollection<CorrespondenceDocument> Documents => _documents;

    public void Register(string reference, Guid actor, DateTimeOffset at)
    {
        Ensure(CorrespondenceStatus.Draft); ReferenceNumber = EntityGuards.Required(reference, nameof(reference));
        Status = CorrespondenceStatus.Registered; if (Direction == CorrespondenceDirection.Incoming) ReceivedAt = at;
        AddHistory(actor, "Registered", null, Status, null, at);
    }
    public void Assign(Guid userId, DateOnly? dueDate, string? comment, Guid actor, DateTimeOffset at)
    {
        if (Status is CorrespondenceStatus.Closed or CorrespondenceStatus.Cancelled) throw new DomainException("A closed correspondence cannot be assigned.");
        var old = Status; AssignedToUserId = EntityGuards.Required(userId, nameof(userId)); DueDate = dueDate;
        Status = CorrespondenceStatus.Assigned; AddHistory(actor, "Assigned", old, Status, comment, at);
    }
    public void Advance(CorrespondenceStatus target, Guid actor, string? comment, DateTimeOffset at)
    {
        var allowed = Status switch
        {
            CorrespondenceStatus.Assigned => target is CorrespondenceStatus.InProgress,
            CorrespondenceStatus.InProgress => target is CorrespondenceStatus.AwaitingResponse or CorrespondenceStatus.Processed or CorrespondenceStatus.SubmittedForValidation,
            CorrespondenceStatus.AwaitingResponse => target is CorrespondenceStatus.InProgress or CorrespondenceStatus.Processed,
            CorrespondenceStatus.SubmittedForValidation => target is CorrespondenceStatus.Validated or CorrespondenceStatus.Rejected,
            CorrespondenceStatus.Rejected => target is CorrespondenceStatus.InProgress,
            CorrespondenceStatus.Validated => target is CorrespondenceStatus.ReadyToSend,
            CorrespondenceStatus.ReadyToSend => target is CorrespondenceStatus.Sent,
            CorrespondenceStatus.Sent => target is CorrespondenceStatus.Acknowledged or CorrespondenceStatus.Closed,
            CorrespondenceStatus.Acknowledged or CorrespondenceStatus.Processed => target is CorrespondenceStatus.Closed,
            _ => false
        };
        if (!allowed) throw new DomainException($"Transition from {Status} to {target} is not allowed.");
        if (target == CorrespondenceStatus.Rejected && string.IsNullOrWhiteSpace(comment)) throw new DomainException("A rejection comment is required.");
        var old = Status; Status = target; if (target == CorrespondenceStatus.Sent) SentAt = at;
        if (target == CorrespondenceStatus.Closed) ClosedAt = at;
        AddHistory(actor, target.ToString(), old, target, comment, at);
    }
    public void Link(Guid? parentId, Guid? declarationId, Guid? obligationId) { ParentCorrespondenceId = parentId; TaxDeclarationId = declarationId; TaxObligationId = obligationId; }
    public void SetRouting(Guid correspondentOrganizationId, string organizationName, Guid internalDepartmentId)
    {
        CorrespondentOrganizationId = EntityGuards.Required(correspondentOrganizationId, nameof(correspondentOrganizationId));
        InternalDepartmentId = EntityGuards.Required(internalDepartmentId, nameof(internalDepartmentId));
        if (Direction == CorrespondenceDirection.Incoming) SenderOrganization = EntityGuards.Required(organizationName, nameof(organizationName));
        else RecipientOrganization = EntityGuards.Required(organizationName, nameof(organizationName));
    }
    private void Ensure(CorrespondenceStatus status) { if (Status != status) throw new DomainException($"Correspondence must be {status}."); }
    private void AddHistory(Guid actor, string action, CorrespondenceStatus? from, CorrespondenceStatus to, string? comment, DateTimeOffset at)
    { _history.Add(new CorrespondenceHistory(Id, actor, action, from, to, comment, at)); MarkUpdatedBy(actor, at); }
}

public sealed class CorrespondenceHistory : AuditableEntity
{
    private CorrespondenceHistory() { }
    public CorrespondenceHistory(Guid correspondenceId, Guid actorUserId, string action, CorrespondenceStatus? fromStatus, CorrespondenceStatus toStatus, string? comment, DateTimeOffset occurredAt)
    { CorrespondenceId = correspondenceId; ActorUserId = actorUserId; Action = action; FromStatus = fromStatus; ToStatus = toStatus; Comment = comment?.Trim(); OccurredAt = occurredAt; }
    public Guid CorrespondenceId { get; private set; } public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty; public CorrespondenceStatus? FromStatus { get; private set; }
    public CorrespondenceStatus ToStatus { get; private set; } public string? Comment { get; private set; } public DateTimeOffset OccurredAt { get; private set; }
}

public sealed class CorrespondenceDocument : AuditableEntity
{
    private CorrespondenceDocument() { }
    public CorrespondenceDocument(Guid correspondenceId, CorrespondenceDocumentType type, string fileName, string path, string contentType, long size, Guid userId)
    { CorrespondenceId = correspondenceId; Type = type; FileName = fileName; FilePath = path; ContentType = contentType; FileSizeBytes = size; UploadedByUserId = userId; }
    public Guid CorrespondenceId { get; private set; } public CorrespondenceDocumentType Type { get; private set; }
    public string FileName { get; private set; } = string.Empty; public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty; public long FileSizeBytes { get; private set; } public Guid UploadedByUserId { get; private set; }
}

public sealed class CorrespondenceSequence
{
    private CorrespondenceSequence() { }
    public CorrespondenceSequence(int year, CorrespondenceDirection direction) { Year = year; Direction = direction; }
    public int Year { get; private set; } public CorrespondenceDirection Direction { get; private set; } public int LastNumber { get; private set; }
    public int Next() => ++LastNumber;
}
