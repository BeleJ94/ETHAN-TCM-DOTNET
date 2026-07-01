namespace EthanTcm.Domain.Enums;

public enum DocumentType
{
    TaxReturnDraft = 0,
    SubmissionProof = 1,
    PaymentProof = 2,
    TaxAuthorityCorrespondence = 3,
    InternalNote = 4,
    Other = 5,
    CalculationWorkpaper = 6,
    DeclarationForm = 7,
    ApprovalEvidence = 8,
    FiledDeclaration = 9,
    FilingReceipt = 10,
    PerceptionNote = 11,
    FinancialStatements = 12,
    PermitOrLicence = 13,

    TaxReturn = TaxReturnDraft,
    Correspondence = TaxAuthorityCorrespondence
}
