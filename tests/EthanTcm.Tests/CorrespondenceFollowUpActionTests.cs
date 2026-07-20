using EthanTcm.Domain.Common;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Tests;
public sealed class CorrespondenceFollowUpActionTests
{
 [Fact] public void Action_can_be_started_waited_resumed_and_completed(){var a=new CorrespondenceFollowUpAction(Guid.NewGuid(),"Prepare response",null,Guid.NewGuid(),DateOnly.FromDateTime(DateTime.Today.AddDays(2)),CorrespondencePriority.High,Guid.NewGuid());var now=DateTimeOffset.UtcNow;a.Start(now);a.WaitForThirdParty("Legal opinion",DateOnly.FromDateTime(DateTime.Today.AddDays(1)),now);Assert.Equal(CorrespondenceActionStatus.WaitingForThirdParty,a.Status);a.Resume(now);a.Complete("Response approved",now);Assert.Equal(CorrespondenceActionStatus.Completed,a.Status);Assert.Equal("Response approved",a.CompletionResult);}
 [Fact] public void Waiting_requires_a_follow_up_subject(){var a=new CorrespondenceFollowUpAction(Guid.NewGuid(),"Prepare response",null,Guid.NewGuid(),DateOnly.FromDateTime(DateTime.Today),CorrespondencePriority.Normal,Guid.NewGuid());Assert.Throws<DomainException>(()=>a.WaitForThirdParty(" ",DateOnly.FromDateTime(DateTime.Today),DateTimeOffset.UtcNow));}
 [Fact] public void Completion_requires_a_result(){var a=new CorrespondenceFollowUpAction(Guid.NewGuid(),"Prepare response",null,Guid.NewGuid(),DateOnly.FromDateTime(DateTime.Today),CorrespondencePriority.Normal,Guid.NewGuid());Assert.Throws<DomainException>(()=>a.Complete(" ",DateTimeOffset.UtcNow));}
}
