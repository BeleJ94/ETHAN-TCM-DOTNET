using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class CorrespondenceOrganization : AuditableEntity
{
    private CorrespondenceOrganization() { }
    public CorrespondenceOrganization(string code, string name, string? contactEmail = null, string? address = null)
    { Code = EntityGuards.Required(code, nameof(code)).ToUpperInvariant(); Name = EntityGuards.Required(name, nameof(name)); ContactEmail = contactEmail?.Trim(); Address = address?.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void Update(string name, string? email, string? address, DateTimeOffset at) { Name = EntityGuards.Required(name, nameof(name)); ContactEmail = email?.Trim(); Address = address?.Trim(); MarkUpdated(at); }
}
