using EthanTcm.Domain.Common;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Tests;

public sealed class CorrespondenceDomainTests
{
    private static Correspondence New(CorrespondenceDirection direction = CorrespondenceDirection.Incoming) =>
        new(direction, "Official tax enquiry", "DGI/2026/184", DateOnly.FromDateTime(DateTime.Today), CorrespondencePriority.High,
            CorrespondenceConfidentiality.Internal, CorrespondenceChannel.Email, "Authority", "Tax Authority", "Tax team", "ETHAN", "Response required");

    [Fact]
    public void Register_assign_and_process_preserve_auditable_history()
    {
        var actor = Guid.NewGuid(); var owner = Guid.NewGuid(); var item = New(); var now = DateTimeOffset.UtcNow;
        item.Register("IN-2026-000001", actor, now);
        item.Assign(owner, DateOnly.FromDateTime(DateTime.Today.AddDays(5)), "Please review", actor, now);
        item.Advance(CorrespondenceStatus.InProgress, owner, null, now);
        item.Advance(CorrespondenceStatus.Processed, owner, "Completed", now);
        item.Advance(CorrespondenceStatus.Closed, owner, null, now);
        Assert.Equal(CorrespondenceStatus.Closed, item.Status); Assert.Equal(owner, item.AssignedToUserId); Assert.Equal(5, item.History.Count);
    }

    [Fact]
    public void Outgoing_validation_flow_reaches_dispatch()
    {
        var actor = Guid.NewGuid(); var item = New(CorrespondenceDirection.Outgoing); var now = DateTimeOffset.UtcNow;
        item.Register("OUT-2026-000001", actor, now); item.Assign(actor, null, null, actor, now);
        item.Advance(CorrespondenceStatus.InProgress, actor, null, now);
        item.Advance(CorrespondenceStatus.SubmittedForValidation, actor, null, now);
        item.Advance(CorrespondenceStatus.Validated, actor, null, now);
        item.Advance(CorrespondenceStatus.ReadyToSend, actor, null, now);
        item.Advance(CorrespondenceStatus.Sent, actor, null, now);
        Assert.NotNull(item.SentAt); Assert.Equal(CorrespondenceStatus.Sent, item.Status);
    }

    [Fact]
    public void Invalid_transition_is_rejected() => Assert.Throws<DomainException>(() => New().Advance(CorrespondenceStatus.Closed, Guid.NewGuid(), null, DateTimeOffset.UtcNow));

    [Fact]
    public void Registering_the_same_correspondence_twice_does_not_replace_its_reference()
    {
        var item = New(); var actor = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        item.Register("IN-2026-000042", actor, now);
        Assert.Throws<DomainException>(() => item.Register("IN-2026-000043", actor, now.AddSeconds(1)));
        Assert.Equal("IN-2026-000042", item.ReferenceNumber);
        Assert.Single(item.History);
    }
}
