using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class CorrespondenceOrganizationService(EthanTcmDbContext db, ICurrentUserService user, IAuditService audit) : ICorrespondenceOrganizationService
{
    public async Task<IReadOnlyCollection<CorrespondenceOrganizationItem>> ListAsync(CancellationToken ct = default) =>
        await db.CorrespondenceOrganizations.AsNoTracking().OrderBy(x => x.Name).Select(x => new CorrespondenceOrganizationItem(x.Id, x.Code, x.Name, x.ContactEmail, x.Address, x.IsActive)).ToListAsync(ct);
    public async Task<CorrespondenceResult> SaveAsync(Guid? id, string code, string name, string? email, string? address, CancellationToken ct = default)
    {
        code = code.Trim().ToUpperInvariant(); name = name.Trim(); if (code.Length == 0 || name.Length == 0) return new(false, null, "Code and name are required.");
        if (await db.CorrespondenceOrganizations.AnyAsync(x => x.Code == code && x.Id != id, ct)) return new(false, null, "This organisation code already exists.");
        CorrespondenceOrganization entity;
        if (id.HasValue) { entity = await db.CorrespondenceOrganizations.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new InvalidOperationException("Organisation not found."); entity.Update(name, email, address, DateTimeOffset.UtcNow); }
        else { entity = new(code, name, email, address); entity.MarkCreatedBy(user.UserId ?? Guid.Empty); db.Add(entity); }
        audit.Add(new(id.HasValue ? "Update" : "Create", nameof(CorrespondenceOrganization), entity.Id.ToString(), null, new { entity.Code, entity.Name }, "Correspondence Referential", "Web"));
        await db.SaveChangesAsync(ct); return new(true, entity.Id, null);
    }
}
