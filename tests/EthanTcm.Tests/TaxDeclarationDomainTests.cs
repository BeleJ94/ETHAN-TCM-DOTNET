using EthanTcm.Domain.Common;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Tests;

public sealed class TaxDeclarationDomainTests
{
    [Fact]
    public void Declaration_cannot_be_closed_without_submission_proof()
    {
        var declaration = CreateDeclaration(paymentRequired: false);

        Assert.Throws<DomainException>(() => declaration.Close(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Declaration_requiring_payment_cannot_be_closed_without_payment_proof()
    {
        var declaration = CreateSubmittedDeclaration(paymentRequired: true);
        declaration.AddPayment(100m, "USD", "PAY-001", DateTimeOffset.UtcNow);
        declaration.AddDocument(DocumentType.SubmissionProof, "submission.pdf", "docs/submission.pdf", "application/pdf", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => declaration.Close(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Declaration_can_be_closed_when_required_proofs_exist()
    {
        var declaration = CreateSubmittedDeclaration(paymentRequired: true);
        var userId = Guid.NewGuid();

        declaration.AddPayment(100m, "USD", "PAY-001", DateTimeOffset.UtcNow);
        declaration.AddDocument(DocumentType.SubmissionProof, "submission.pdf", "docs/submission.pdf", "application/pdf", userId, DateTimeOffset.UtcNow);
        declaration.AddDocument(DocumentType.PaymentProof, "payment.pdf", "docs/payment.pdf", "application/pdf", userId, DateTimeOffset.UtcNow);
        declaration.Close(DateTimeOffset.UtcNow);

        Assert.Equal(TaxDeclarationStatus.Closed, declaration.Status);
    }

    [Fact]
    public void Rejection_requires_comment()
    {
        var declaration = CreateDeclaration(paymentRequired: false);
        declaration.StartPreparation(DateTimeOffset.UtcNow);
        declaration.SubmitForReview(DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => declaration.Reject(Guid.NewGuid(), "", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Approvals_move_declaration_to_ready_for_submission_in_order()
    {
        var declaration = CreateDeclaration(paymentRequired: false);
        declaration.StartPreparation(DateTimeOffset.UtcNow);
        declaration.SubmitForReview(DateTimeOffset.UtcNow);

        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(TaxDeclarationStatus.ApprovedLevel1, declaration.Status);
        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(TaxDeclarationStatus.ApprovedLevel2, declaration.Status);
        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(TaxDeclarationStatus.ReadyForSubmission, declaration.Status);
    }

    [Fact]
    public void Submitted_declaration_can_be_returned_for_submission_correction()
    {
        var declaration = CreateSubmittedDeclaration(paymentRequired: false);

        declaration.ReturnToPreviousStep(DateTimeOffset.UtcNow);

        Assert.Equal(TaxDeclarationStatus.ReadyForSubmission, declaration.Status);
        Assert.Null(declaration.SubmissionReference);
        Assert.Null(declaration.SubmittedAt);
        Assert.Null(declaration.SubmittedByUserId);
    }

    [Fact]
    public void Paid_declaration_requires_formal_financial_correction()
    {
        var declaration = CreateSubmittedDeclaration(paymentRequired: true);
        declaration.AddPayment(100m, "USD", "PAY-001", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => declaration.ReturnToPreviousStep(DateTimeOffset.UtcNow));
        Assert.Equal(TaxDeclarationStatus.Paid, declaration.Status);
    }

    [Fact]
    public void Returning_an_approval_archives_the_old_cycle_and_starts_clean_review()
    {
        var declaration = CreateDeclaration(paymentRequired: false);
        declaration.StartPreparation(DateTimeOffset.UtcNow);
        declaration.SubmitForReview(DateTimeOffset.UtcNow);
        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var archivedCycle = declaration.ApprovalCycleNumber;

        declaration.ReturnToPreviousStep(DateTimeOffset.UtcNow);

        Assert.Equal(TaxDeclarationStatus.Rejected, declaration.Status);
        Assert.Equal(archivedCycle + 1, declaration.ApprovalCycleNumber);
        Assert.DoesNotContain(declaration.Approvals, approval => approval.ApprovalCycleNumber == declaration.ApprovalCycleNumber);

        declaration.SubmitForReview(DateTimeOffset.UtcNow);

        Assert.Equal(TaxDeclarationStatus.SubmittedForReview, declaration.Status);
        Assert.Equal(archivedCycle + 1, declaration.ApprovalCycleNumber);
        Assert.Equal(1, declaration.NextApprovalLevel());
    }

    private static TaxDeclaration CreateSubmittedDeclaration(bool paymentRequired)
    {
        var declaration = CreateDeclaration(paymentRequired);
        declaration.StartPreparation(DateTimeOffset.UtcNow);
        declaration.SubmitForReview(DateTimeOffset.UtcNow);
        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        declaration.ApproveNextLevel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        declaration.MarkSubmitted("SUB-001", DateTimeOffset.UtcNow);
        return declaration;
    }

    private static TaxDeclaration CreateDeclaration(bool paymentRequired)
    {
        return new TaxDeclaration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10)),
            "2026-06",
            paymentRequired,
            Guid.NewGuid());
    }
}
