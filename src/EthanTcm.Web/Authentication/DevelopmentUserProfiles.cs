using EthanTcm.Application.Authentication;

namespace EthanTcm.Web.Authentication;

public sealed record DevelopmentUserProfile(
    string Key,
    string Login,
    string DisplayName,
    string Email,
    string[] Roles);

public static class DevelopmentUserProfiles
{
    public const string CookieName = "EthanTcm.DevUser";

    public static readonly IReadOnlyCollection<DevelopmentUserProfile> All =
    [
        new("administrator", "ETHAN\\admin.local", "Local Administrator", "admin.local@ethantcm.local", [ApplicationRoles.Administrator]),
        new("tax-manager", "ETHAN\\tax.manager", "Local Tax Manager", "tax.manager@ethantcm.local", [ApplicationRoles.TaxManager]),
        new("preparer", "ETHAN\\preparer01", "Local Preparer", "preparer01@ethantcm.local", [ApplicationRoles.Preparer]),
        new("approver", "ETHAN\\approver01", "Local Approver", "approver01@ethantcm.local", [ApplicationRoles.Approver]),
        new("finance-manager", "ETHAN\\finance01", "Local Finance Manager", "finance01@ethantcm.local", [ApplicationRoles.FinanceManager]),
        new("auditor", "ETHAN\\auditor01", "Local Auditor", "auditor01@ethantcm.local", [ApplicationRoles.Auditor]),
        new("read-only", "ETHAN\\readonly01", "Local Read Only", "readonly01@ethantcm.local", [ApplicationRoles.ReadOnly])
    ];

    public static DevelopmentUserProfile? Find(string? key)
    {
        return All.FirstOrDefault(profile => profile.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
