namespace EthanTcm.Application.Authentication;

public static class ApplicationPermissions
{
    public const string ViewDashboard = "Permissions.Dashboard.View";
    public const string ViewTaxDeclarations = "Permissions.TaxDeclarations.View";
    public const string GenerateTaxDeclarations = "Permissions.TaxDeclarations.Generate";
    public const string CreateTaxDeclarations = "Permissions.TaxDeclarations.Create";
    public const string PrepareTaxDeclarations = "Permissions.TaxDeclarations.Prepare";
    public const string ApproveTaxDeclarations = "Permissions.TaxDeclarations.Approve";
    public const string ManageTaxDeclarationLifecycle = "Permissions.TaxDeclarations.ManageLifecycle";
    public const string ManageTaxPayments = "Permissions.TaxPayments.Manage";
    public const string UploadTaxDocuments = "Permissions.TaxDocuments.Upload";
    public const string DownloadTaxDocuments = "Permissions.TaxDocuments.Download";
    public const string DeleteTaxDocuments = "Permissions.TaxDocuments.Delete";
    public const string ManageTaxObligations = "Permissions.TaxObligations.Manage";
    public const string ImportTaxMatrix = "Permissions.TaxMatrix.Import";
    public const string ViewAuditLogs = "Permissions.AuditLogs.View";
    public const string RunAdministrationTasks = "Permissions.Administration.Run";

    public static readonly IReadOnlyDictionary<string, string[]> RolePermissions = new Dictionary<string, string[]>
    {
        [ApplicationRoles.Administrator] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            GenerateTaxDeclarations,
            CreateTaxDeclarations,
            PrepareTaxDeclarations,
            ApproveTaxDeclarations,
            ManageTaxDeclarationLifecycle,
            ManageTaxPayments,
            UploadTaxDocuments,
            DownloadTaxDocuments,
            DeleteTaxDocuments,
            ManageTaxObligations,
            ImportTaxMatrix,
            ViewAuditLogs,
            RunAdministrationTasks
        ],
        [ApplicationRoles.TaxManager] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            GenerateTaxDeclarations,
            CreateTaxDeclarations,
            PrepareTaxDeclarations,
            ApproveTaxDeclarations,
            ManageTaxDeclarationLifecycle,
            ManageTaxPayments,
            UploadTaxDocuments,
            DownloadTaxDocuments,
            DeleteTaxDocuments,
            ManageTaxObligations,
            ImportTaxMatrix
        ],
        [ApplicationRoles.Preparer] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            PrepareTaxDeclarations,
            UploadTaxDocuments,
            DownloadTaxDocuments
        ],
        [ApplicationRoles.Approver] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            ApproveTaxDeclarations,
            DownloadTaxDocuments
        ],
        [ApplicationRoles.FinanceManager] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            ManageTaxPayments,
            UploadTaxDocuments,
            DownloadTaxDocuments
        ],
        [ApplicationRoles.Auditor] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            DownloadTaxDocuments,
            ViewAuditLogs
        ],
        [ApplicationRoles.ReadOnly] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            DownloadTaxDocuments
        ]
    };

    public static readonly string[] All = RolePermissions.Values
        .SelectMany(permission => permission)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(permission => permission, StringComparer.Ordinal)
        .ToArray();

    public static string[] RolesFor(string permission)
    {
        return RolePermissions
            .Where(item => item.Value.Contains(permission, StringComparer.Ordinal))
            .Select(item => item.Key)
            .ToArray();
    }
}
