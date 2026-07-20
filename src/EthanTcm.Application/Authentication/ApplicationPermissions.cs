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
    public const string ViewUserAdministration = "Permissions.UserAdministration.View";
    public const string ManageUsers = "Permissions.UserAdministration.ManageUsers";
    public const string ManageRoles = "Permissions.UserAdministration.ManageRoles";
    public const string ViewAccessAudit = "Permissions.UserAdministration.ViewAccessAudit";
    public const string ViewCorrespondence = "Permissions.Correspondence.View";
    public const string CreateCorrespondence = "Permissions.Correspondence.Create";
    public const string AssignCorrespondence = "Permissions.Correspondence.Assign";
    public const string ProcessCorrespondence = "Permissions.Correspondence.Process";
    public const string ValidateCorrespondence = "Permissions.Correspondence.Validate";

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
            ,ViewUserAdministration, ManageUsers, ManageRoles, ViewAccessAudit
            ,ViewCorrespondence, CreateCorrespondence, AssignCorrespondence, ProcessCorrespondence, ValidateCorrespondence
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
            ,ViewCorrespondence, CreateCorrespondence, AssignCorrespondence, ProcessCorrespondence, ValidateCorrespondence
        ],
        [ApplicationRoles.Preparer] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            PrepareTaxDeclarations,
            UploadTaxDocuments,
            DownloadTaxDocuments
            ,ViewCorrespondence, CreateCorrespondence, ProcessCorrespondence
        ],
        [ApplicationRoles.Approver] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            ApproveTaxDeclarations,
            DownloadTaxDocuments
            ,ViewCorrespondence, ProcessCorrespondence, ValidateCorrespondence
        ],
        [ApplicationRoles.FinanceManager] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            ManageTaxPayments,
            UploadTaxDocuments,
            DownloadTaxDocuments
            ,ViewCorrespondence, CreateCorrespondence, ProcessCorrespondence
        ],
        [ApplicationRoles.Auditor] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            DownloadTaxDocuments,
            ViewAuditLogs
            ,ViewCorrespondence
        ],
        [ApplicationRoles.ReadOnly] =
        [
            ViewDashboard,
            ViewTaxDeclarations,
            DownloadTaxDocuments
            ,ViewCorrespondence
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
