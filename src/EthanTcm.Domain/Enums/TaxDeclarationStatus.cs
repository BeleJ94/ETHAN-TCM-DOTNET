namespace EthanTcm.Domain.Enums;

public enum TaxDeclarationStatus
{
    ToPrepare = 1,
    InPreparation = 2,
    SubmittedForReview = 3,
    ApprovedLevel1 = 4,
    ApprovedLevel2 = 5,
    ApprovedLevel3 = 6,
    ReadyForSubmission = 7,
    Submitted = 8,
    PaymentPending = 9,
    Paid = 10,
    Closed = 11,
    Late = 12,
    Cancelled = 13,
    NotApplicable = 14,
    Rejected = 15,

    Draft = ToPrepare,
    InProgress = InPreparation,
    ReadyForApproval = SubmittedForReview,
    Approved = ApprovedLevel3
}
