using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class CorrespondenceActionService(EthanTcmDbContext db, ICurrentUserService user, IAuditService audit) : ICorrespondenceActionService
{
    private static readonly CorrespondenceActionStatus[] Final = [CorrespondenceActionStatus.Completed, CorrespondenceActionStatus.Cancelled];
    public async Task<CorrespondenceActionDashboard> GetDashboardAsync(bool myActions, CancellationToken ct = default)
    {
        var today=DateOnly.FromDateTime(DateTime.Today); var q=Visible(db.CorrespondenceFollowUpActions.AsNoTracking(),myActions); var open=q.Where(x=>!Final.Contains(x.Status));
        return new(await open.CountAsync(ct),await open.CountAsync(x=>x.DueDate<today,ct),await open.CountAsync(x=>x.DueDate==today,ct),await open.CountAsync(x=>x.DueDate>today&&x.DueDate<=today.AddDays(5),ct),await open.CountAsync(x=>x.Status==CorrespondenceActionStatus.WaitingForThirdParty,ct),await open.CountAsync(x=>x.IsEscalated,ct),await db.Correspondences.CountAsync(x=>x.AssignedToUserId==null&&x.Status!=CorrespondenceStatus.Draft&&x.Status!=CorrespondenceStatus.Closed,ct));
    }
    public async Task<IReadOnlyCollection<CorrespondenceActionItem>> ListAsync(bool myActions, CorrespondenceActionStatus? status, CancellationToken ct = default)
    { var q=Visible(db.CorrespondenceFollowUpActions.AsNoTracking(),myActions); if(status.HasValue)q=q.Where(x=>x.Status==status); q=q.OrderBy(x=>x.Status==CorrespondenceActionStatus.Completed||x.Status==CorrespondenceActionStatus.Cancelled).ThenBy(x=>x.DueDate); return await Project(q).Take(300).ToListAsync(ct); }
    public async Task<IReadOnlyCollection<CorrespondenceActionItem>> ListForCorrespondenceAsync(Guid correspondenceId,CancellationToken ct=default){var q=db.CorrespondenceFollowUpActions.AsNoTracking().Where(x=>x.CorrespondenceId==correspondenceId).OrderBy(x=>x.Status==CorrespondenceActionStatus.Completed||x.Status==CorrespondenceActionStatus.Cancelled).ThenBy(x=>x.DueDate);return await Project(q).ToListAsync(ct);}
    public async Task<CorrespondenceResult> CreateAsync(CorrespondenceActionCreateCommand c,CancellationToken ct=default)
    { if(!user.UserId.HasValue)return Fail("Current user is required."); if(!await db.Correspondences.AnyAsync(x=>x.Id==c.CorrespondenceId&&x.Status!=CorrespondenceStatus.Closed,ct))return Fail("An open correspondence is required."); if(!await db.Users.AnyAsync(x=>x.Id==c.AssignedToUserId&&x.IsActive,ct))return Fail("An active owner is required."); var item=new CorrespondenceFollowUpAction(c.CorrespondenceId,c.Title,c.Description,c.AssignedToUserId,c.DueDate,c.Priority,user.UserId.Value,c.EscalationUserId); db.Add(item); AddAudit("Create",item); await db.SaveChangesAsync(ct); return new(true,c.CorrespondenceId,"Follow-up action created."); }
    public Task<CorrespondenceResult> StartAsync(Guid id,CancellationToken ct=default)=>Mutate(id,x=>x.Start(DateTimeOffset.UtcNow),"Start",ct);
    public Task<CorrespondenceResult> WaitAsync(Guid id,string waitingFor,DateOnly followUpDate,CancellationToken ct=default)=>Mutate(id,x=>x.WaitForThirdParty(waitingFor,followUpDate,DateTimeOffset.UtcNow),"WaitForThirdParty",ct);
    public Task<CorrespondenceResult> ResumeAsync(Guid id,CancellationToken ct=default)=>Mutate(id,x=>x.Resume(DateTimeOffset.UtcNow),"Resume",ct);
    public Task<CorrespondenceResult> CompleteAsync(Guid id,string result,CancellationToken ct=default)=>Mutate(id,x=>x.Complete(result,DateTimeOffset.UtcNow),"Complete",ct);
    public Task<CorrespondenceResult> CancelAsync(Guid id,string reason,CancellationToken ct=default)=>Mutate(id,x=>x.Cancel(reason,DateTimeOffset.UtcNow),"Cancel",ct);
    private async Task<CorrespondenceResult> Mutate(Guid id,Action<CorrespondenceFollowUpAction> change,string action,CancellationToken ct){var item=await Visible(db.CorrespondenceFollowUpActions,false).FirstOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return Fail("Follow-up action not found.");try{change(item);item.MarkUpdatedBy(user.UserId??Guid.Empty,DateTimeOffset.UtcNow);AddAudit(action,item);await db.SaveChangesAsync(ct);return new(true,item.CorrespondenceId,null);}catch(Exception ex)when(ex is EthanTcm.Domain.Common.DomainException or DbUpdateConcurrencyException){return Fail(ex.Message);}}
    private IQueryable<CorrespondenceFollowUpAction> Visible(IQueryable<CorrespondenceFollowUpAction> q,bool mine){if(mine&&user.UserId.HasValue)return q.Where(x=>x.AssignedToUserId==user.UserId);if(user.IsInRole(ApplicationRoles.Administrator)||user.IsInRole(ApplicationRoles.TaxManager)||user.IsInRole(ApplicationRoles.Auditor))return q;return user.UserId.HasValue?q.Where(x=>x.AssignedToUserId==user.UserId):q.Where(_=>false);}
    private IQueryable<CorrespondenceActionItem> Project(IQueryable<CorrespondenceFollowUpAction> q){var today=DateOnly.FromDateTime(DateTime.Today);return from a in q join c in db.Correspondences.AsNoTracking() on a.CorrespondenceId equals c.Id join u in db.Users.AsNoTracking() on a.AssignedToUserId equals u.Id select new CorrespondenceActionItem(a.Id,a.CorrespondenceId,c.ReferenceNumber??"DRAFT",c.Subject,a.Title,a.Description,a.AssignedToUserId,u.DisplayName,a.DueDate,a.FollowUpDate,a.Priority,a.Status,a.WaitingFor,a.CompletionResult,a.DueDate<today&&!Final.Contains(a.Status),a.DueDate>=today&&a.DueDate<=today.AddDays(5)&&!Final.Contains(a.Status),a.IsEscalated);}
    private void AddAudit(string action,CorrespondenceFollowUpAction x)=>audit.Add(new(action,nameof(CorrespondenceFollowUpAction),x.Id.ToString(),null,new{x.CorrespondenceId,x.Title,x.AssignedToUserId,x.DueDate,x.Status},"Correspondence Actions","Web"));
    private static CorrespondenceResult Fail(string m)=>new(false,null,m);
}
