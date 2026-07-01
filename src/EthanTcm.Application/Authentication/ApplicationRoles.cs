namespace EthanTcm.Application.Authentication;

public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string TaxManager = "TaxManager";
    public const string Preparer = "Preparer";
    public const string Approver = "Approver";
    public const string FinanceManager = "FinanceManager";
    public const string Auditor = "Auditor";
    public const string ReadOnly = "ReadOnly";

    public static readonly string[] All =
    [
        Administrator,
        TaxManager,
        Preparer,
        Approver,
        FinanceManager,
        Auditor,
        ReadOnly
    ];
}
