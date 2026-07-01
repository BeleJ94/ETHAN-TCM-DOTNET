using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EthanTcm.Tests;

public sealed class TaxDeclarationWorkflowServiceTests
{
    [Fact]
    public async Task Preparer_can_start_and_submit_for_review()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer);

        var start = await service.StartPreparationAsync(setup.DeclarationId);
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.TaxReturnDraft);
        var submit = await service.SubmitForReviewAsync(setup.DeclarationId);

        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.True(start.Success);
        Assert.True(submit.Success);
        Assert.Equal(TaxDeclarationStatus.SubmittedForReview, declaration.Status);
    }

    [Fact]
    public async Task Submit_for_review_requires_preparation_proof()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer);

        var start = await service.StartPreparationAsync(setup.DeclarationId);
        var submit = await service.SubmitForReviewAsync(setup.DeclarationId);

        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.True(start.Success);
        Assert.False(submit.Success);
        Assert.Contains("Preparation proof", submit.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TaxDeclarationStatus.InPreparation, declaration.Status);
    }

    [Fact]
    public async Task Approvers_validate_in_order_until_ready_for_submission()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);

        Assert.True((await CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);
        Assert.True((await CreateService(dbContext, setup.Approver2Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);
        Assert.True((await CreateService(dbContext, setup.Approver3Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);

        var declaration = await dbContext.TaxDeclarations
            .Include(item => item.Approvals)
            .SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.Equal(TaxDeclarationStatus.ReadyForSubmission, declaration.Status);
        Assert.Equal(3, declaration.Approvals.Count);
    }

    [Fact]
    public async Task Details_split_approval_steps_by_level_and_current_approver()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver);

        var details = await service.GetAsync(setup.DeclarationId);

        Assert.NotNull(details);
        Assert.Equal(3, details.ApprovalSteps.Count);
        var level1 = details.ApprovalSteps.Single(step => step.Level == 1);
        var level2 = details.ApprovalSteps.Single(step => step.Level == 2);
        var level3 = details.ApprovalSteps.Single(step => step.Level == 3);

        Assert.Equal(TaxDeclarationApprovalStepStatus.Pending, level1.Status);
        Assert.True(level1.CanAct);
        Assert.Contains(setup.Approver1Id, level1.ExpectedApproverUserIds);
        Assert.Equal(TaxDeclarationApprovalStepStatus.NotReached, level2.Status);
        Assert.False(level2.CanAct);
        Assert.Equal(TaxDeclarationApprovalStepStatus.NotReached, level3.Status);
        Assert.False(level3.CanAct);
    }

    [Fact]
    public async Task Configured_level_3_approver_can_act_without_global_approver_role()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        Assert.True((await CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);
        Assert.True((await CreateService(dbContext, setup.Approver2Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);
        var level3Service = CreateService(dbContext, setup.Approver3Id, ApplicationRoles.ReadOnly);

        var details = await level3Service.GetAsync(setup.DeclarationId);
        var approved = await level3Service.ApproveAsync(setup.DeclarationId);

        Assert.NotNull(details);
        var level3 = details.ApprovalSteps.Single(step => step.Level == 3);
        Assert.Equal(TaxDeclarationApprovalStepStatus.Pending, level3.Status);
        Assert.True(level3.CanAct);
        Assert.True(approved.Success, approved.ErrorMessage);
    }

    [Fact]
    public async Task Rejection_requires_comment_and_sets_rejected()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver);

        var missingComment = await service.RejectAsync(setup.DeclarationId, "");
        var rejected = await service.RejectAsync(setup.DeclarationId, "Missing supporting schedule.");

        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.False(missingComment.Success);
        Assert.True(rejected.Success);
        Assert.Equal(TaxDeclarationStatus.Rejected, declaration.Status);
    }

    [Fact]
    public async Task Rejected_declaration_starts_new_approval_cycle_after_resubmission()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);

        Assert.True((await CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);
        Assert.True((await CreateService(dbContext, setup.Approver2Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId)).Success);
        Assert.True((await CreateService(dbContext, setup.Approver3Id, ApplicationRoles.Approver).RejectAsync(setup.DeclarationId, "Please correct the supporting schedule.")).Success);

        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.TaxReturnDraft);
        var resubmitted = await CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer).SubmitForReviewAsync(setup.DeclarationId);
        var details = await CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver).GetAsync(setup.DeclarationId);
        var reapproved = await CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver).ApproveAsync(setup.DeclarationId);

        var declaration = await dbContext.TaxDeclarations
            .Include(item => item.Approvals)
            .SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.True(resubmitted.Success, resubmitted.ErrorMessage);
        Assert.NotNull(details);
        Assert.Equal(2, declaration.ApprovalCycleNumber);
        Assert.Equal(4, declaration.Approvals.Count);
        Assert.Contains(declaration.Approvals, approval => approval.ApprovalCycleNumber == 1 && approval.ApprovalLevel == 3 && approval.Decision == ApprovalDecision.Rejected);

        var level1 = details.ApprovalSteps.Single(step => step.Level == 1);
        var level2 = details.ApprovalSteps.Single(step => step.Level == 2);
        var level3 = details.ApprovalSteps.Single(step => step.Level == 3);
        Assert.Equal(TaxDeclarationApprovalStepStatus.Pending, level1.Status);
        Assert.True(level1.CanAct);
        Assert.Equal(TaxDeclarationApprovalStepStatus.NotReached, level2.Status);
        Assert.Equal(TaxDeclarationApprovalStepStatus.NotReached, level3.Status);
        Assert.True(reapproved.Success, reapproved.ErrorMessage);
    }

    [Fact]
    public async Task Same_approver_cannot_approve_multiple_levels()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, setup.Approver1Id, ApplicationRoles.Approver);

        var firstApproval = await service.ApproveAsync(setup.DeclarationId);
        var secondApproval = await service.ApproveAsync(setup.DeclarationId);

        Assert.True(firstApproval.Success);
        Assert.False(secondApproval.Success);
        Assert.Contains("same approver", secondApproval.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unassigned_preparer_cannot_mark_declaration_submitted()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationReadyForSubmissionAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.SubmissionProof);
        var service = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.Preparer);

        var result = await service.MarkSubmittedAsync(setup.DeclarationId, "SUB-001");

        Assert.False(result.Success);
        Assert.Contains("assigned preparer", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Payment_required_declaration_goes_to_payment_pending_then_paid_then_closed_with_proofs()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationReadyForSubmissionAsync(options, paymentRequired: true);
        await using var dbContext = CreateDbContext(options);
        var managerService = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.SubmissionProof);
        var submitted = await managerService.MarkSubmittedAsync(setup.DeclarationId, "SUB-001");
        var closeBeforePayment = await managerService.CloseAsync(setup.DeclarationId);
        var financeService = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.FinanceManager);
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.PaymentProof);
        var paid = await financeService.MarkPaidAsync(setup.DeclarationId, 1200m, "USD", "PAY-001");
        var closed = await managerService.CloseAsync(setup.DeclarationId);

        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.True(submitted.Success);
        Assert.False(closeBeforePayment.Success);
        Assert.True(paid.Success);
        Assert.True(closed.Success);
        Assert.Equal(TaxDeclarationStatus.Closed, declaration.Status);
    }

    [Fact]
    public async Task Configured_payment_process_owner_can_record_payment()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationReadyForSubmissionAsync(options, paymentRequired: true);
        await using var dbContext = CreateDbContext(options);
        var managerService = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.SubmissionProof);
        var submitted = await managerService.MarkSubmittedAsync(setup.DeclarationId, "SUB-001");
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.PaymentProof);
        var paymentOwnerService = CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer);
        var paid = await paymentOwnerService.MarkPaidAsync(setup.DeclarationId, 1200m, "USD", "PAY-001");

        Assert.True(submitted.Success, submitted.ErrorMessage);
        Assert.True(paid.Success, paid.ErrorMessage);
    }

    [Fact]
    public async Task Configured_follow_up_owner_can_close_declaration()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationReadyForSubmissionAsync(options, paymentRequired: true);
        await using var dbContext = CreateDbContext(options);
        var managerService = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.SubmissionProof);
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.PaymentProof);
        var submitted = await managerService.MarkSubmittedAsync(setup.DeclarationId, "SUB-001");
        var paid = await CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer)
            .MarkPaidAsync(setup.DeclarationId, 1200m, "USD", "PAY-001");
        var closed = await CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer)
            .CloseAsync(setup.DeclarationId);

        Assert.True(submitted.Success, submitted.ErrorMessage);
        Assert.True(paid.Success, paid.ErrorMessage);
        Assert.True(closed.Success, closed.ErrorMessage);
    }

    [Fact]
    public async Task Workflow_records_actual_submission_payment_and_closure_actors()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationReadyForSubmissionAsync(options, paymentRequired: true);
        await using var dbContext = CreateDbContext(options);
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.SubmissionProof);
        await UploadDocumentAsync(dbContext, setup.DeclarationId, DocumentType.PaymentProof);
        var managerId = Guid.NewGuid();
        var financeId = Guid.NewGuid();

        var submitted = await CreateService(dbContext, managerId, ApplicationRoles.TaxManager).MarkSubmittedAsync(setup.DeclarationId, "SUB-001");
        var paid = await CreateService(dbContext, financeId, ApplicationRoles.FinanceManager).MarkPaidAsync(setup.DeclarationId, 1200m, "USD", "PAY-001");
        var closed = await CreateService(dbContext, managerId, ApplicationRoles.TaxManager).CloseAsync(setup.DeclarationId);

        var declaration = await dbContext.TaxDeclarations
            .Include(item => item.Payments)
            .SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.True(submitted.Success, submitted.ErrorMessage);
        Assert.True(paid.Success, paid.ErrorMessage);
        Assert.True(closed.Success, closed.ErrorMessage);
        Assert.Equal(managerId, declaration.SubmittedByUserId);
        Assert.Equal(managerId, declaration.ClosedByUserId);
        Assert.Equal(financeId, declaration.Payments.Single().PaidByUserId);
    }

    [Fact]
    public async Task Workflow_actions_are_audited()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, setup.PreparerId, ApplicationRoles.Preparer);

        await service.StartPreparationAsync(setup.DeclarationId);

        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.EntityId == setup.DeclarationId.ToString() && log.Action == "StartPreparation"));
    }

    [Fact]
    public async Task Tax_manager_can_reassign_active_declaration_with_audit()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var newAssignee = new User("new.preparer", "New Preparer", "new.preparer@local");
        dbContext.Users.Add(newAssignee);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        var result = await service.ReassignAsync(new TaxDeclarationReassignmentCommand(
            setup.DeclarationId,
            newAssignee.Id,
            "Workload transfer"));

        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(newAssignee.Id, declaration.AssignedToUserId);
        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.EntityId == setup.DeclarationId.ToString() && log.Action == "Reassign"));
    }

    [Fact]
    public async Task Reassignment_requires_active_target_user()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var inactiveUser = new User("inactive.preparer", "Inactive Preparer", "inactive.preparer@local");
        inactiveUser.Deactivate(DateTimeOffset.UtcNow);
        dbContext.Users.Add(inactiveUser);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        var result = await service.ReassignAsync(new TaxDeclarationReassignmentCommand(
            setup.DeclarationId,
            inactiveUser.Id,
            "Invalid target"));

        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        Assert.False(result.Success);
        Assert.Equal(setup.PreparerId, declaration.AssignedToUserId);
        Assert.Contains("inactive", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Terminal_declaration_cannot_be_reassigned()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);
        declaration.Cancel(DateTimeOffset.UtcNow);
        var newAssignee = new User("new.preparer", "New Preparer", "new.preparer@local");
        dbContext.Users.Add(newAssignee);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        var result = await service.ReassignAsync(new TaxDeclarationReassignmentCommand(
            setup.DeclarationId,
            newAssignee.Id,
            "Late transfer"));

        Assert.False(result.Success);
        Assert.Contains("cannot be reassigned", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unassigned_user_cannot_view_declaration_list_or_details()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var service = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.Preparer);

        var list = await service.ListAsync();
        var details = await service.GetAsync(setup.DeclarationId);

        Assert.Empty(list);
        Assert.Null(details);
    }

    [Fact]
    public async Task Declaration_search_is_filtered_and_paginated_on_the_server()
    {
        var options = CreateOptions();
        var setup = await CreateDeclarationAsync(options, paymentRequired: false);
        await using var dbContext = CreateDbContext(options);
        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);

        for (var index = 1; index < 15; index++)
        {
            var year = 2026 + index / 12;
            var month = index % 12 + 1;
            var start = new DateOnly(year, month, 1);
            var period = new TaxPeriod(year, month, null, start, start.AddMonths(1).AddDays(-1), $"{year}-{month:00}");
            dbContext.TaxPeriods.Add(period);
            dbContext.TaxDeclarations.Add(new TaxDeclaration(
                declaration.TaxObligationId,
                period.Id,
                start.AddMonths(1).AddDays(14),
                period.Label,
                false,
                setup.PreparerId));
        }

        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Guid.NewGuid(), ApplicationRoles.TaxManager);

        var firstPage = await service.SearchAsync(new TaxDeclarationWorkflowSearchCriteria("VAT", Page: 1, PageSize: 10));
        var secondPage = await service.SearchAsync(new TaxDeclarationWorkflowSearchCriteria("VAT", Page: 2, PageSize: 10));

        Assert.Equal(15, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(10, firstPage.Items.Count);
        Assert.Equal(5, secondPage.Items.Count);
        Assert.Empty(firstPage.Items.Select(item => item.Id).Intersect(secondPage.Items.Select(item => item.Id)));
    }

    private static async Task<WorkflowSetup> CreateDeclarationUnderReviewAsync(DbContextOptions<EthanTcmDbContext> options, bool paymentRequired)
    {
        var setup = await CreateDeclarationAsync(options, paymentRequired);
        await using var dbContext = CreateDbContext(options);
        var declaration = await dbContext.TaxDeclarations.SingleAsync(item => item.Id == setup.DeclarationId);
        declaration.StartPreparation(DateTimeOffset.UtcNow);
        declaration.SubmitForReview(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
        return setup;
    }

    private static async Task<WorkflowSetup> CreateDeclarationReadyForSubmissionAsync(DbContextOptions<EthanTcmDbContext> options, bool paymentRequired)
    {
        var setup = await CreateDeclarationUnderReviewAsync(options, paymentRequired);
        await using var dbContext = CreateDbContext(options);
        var declaration = await dbContext.TaxDeclarations
            .Include(item => item.Approvals)
            .SingleAsync(item => item.Id == setup.DeclarationId);
        declaration.ApproveNextLevel(setup.Approver1Id, DateTimeOffset.UtcNow);
        declaration.ApproveNextLevel(setup.Approver2Id, DateTimeOffset.UtcNow);
        declaration.ApproveNextLevel(setup.Approver3Id, DateTimeOffset.UtcNow);
        foreach (var entry in dbContext.ChangeTracker.Entries<TaxDeclarationApproval>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        await dbContext.SaveChangesAsync();
        return setup;
    }

    private static async Task<WorkflowSetup> CreateDeclarationAsync(DbContextOptions<EthanTcmDbContext> options, bool paymentRequired)
    {
        await using var dbContext = CreateDbContext(options);
        var legalEntity = new LegalEntity("ETHAN", "ETHAN TCM", "CD", null);
        var department = new Department("FINANCE", "Finance");
        var category = new TaxCategory("VAT", "VAT");
        var frequency = new TaxFrequency("MONTHLY", "Monthly", 12);
        var preparer = new User("preparer", "Preparer", "preparer@local");
        var approver1 = new User("approver1", "Approver 1", "approver1@local");
        var approver2 = new User("approver2", "Approver 2", "approver2@local");
        var approver3 = new User("approver3", "Approver 3", "approver3@local");
        var obligation = new TaxObligation(
            legalEntity.Id,
            department.Id,
            category.Id,
            frequency.Id,
            preparer.Id,
            "VAT Return",
            RiskLevel.Medium,
            paymentRequired,
            DateTimeOffset.UtcNow);
        obligation.AddResponsible(approver1.Id, ResponsibleType.Approver1, DateTimeOffset.UtcNow);
        obligation.AddResponsible(approver2.Id, ResponsibleType.Approver2, DateTimeOffset.UtcNow);
        obligation.AddResponsible(approver3.Id, ResponsibleType.Approver3, DateTimeOffset.UtcNow);
        obligation.AddResponsible(preparer.Id, ResponsibleType.PaymentProcessOwner, DateTimeOffset.UtcNow);
        obligation.AddResponsible(preparer.Id, ResponsibleType.FollowUpOwner, DateTimeOffset.UtcNow);
        var period = new TaxPeriod(2026, 1, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "2026-01");
        var declaration = new TaxDeclaration(
            obligation.Id,
            period.Id,
            new DateOnly(2026, 2, 15),
            "2026-01",
            paymentRequired,
            preparer.Id);

        dbContext.AddRange(legalEntity, department, category, frequency, preparer, approver1, approver2, approver3, obligation, period, declaration);
        await dbContext.SaveChangesAsync();
        return new WorkflowSetup(declaration.Id, preparer.Id, approver1.Id, approver2.Id, approver3.Id);
    }

    private static EthanTcmDbContext CreateDbContext(DbContextOptions<EthanTcmDbContext> options)
    {
        return new EthanTcmDbContext(options);
    }

    private static DbContextOptions<EthanTcmDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static TaxDeclarationWorkflowService CreateService(EthanTcmDbContext dbContext, Guid userId, params string[] roles)
    {
        return new TaxDeclarationWorkflowService(dbContext, new TestCurrentUserService(userId, roles), new TestAuditService(dbContext, userId));
    }

    private static async Task UploadDocumentAsync(EthanTcmDbContext dbContext, Guid taxDeclarationId, DocumentType documentType)
    {
        var assignedToUserId = await dbContext.TaxDeclarations
            .Where(declaration => declaration.Id == taxDeclarationId)
            .Select(declaration => declaration.AssignedToUserId)
            .SingleAsync();
        var uploadUserId = assignedToUserId;
        var uploadRole = ApplicationRoles.Preparer;

        var service = new TaxDocumentService(
            dbContext,
            new TestCurrentUserService(uploadUserId, [uploadRole]),
            new TestAuditService(dbContext, uploadUserId),
            Options.Create(new TaxDocumentStorageOptions
            {
                RootPath = Path.Combine(Environment.CurrentDirectory, "TestDocuments", Guid.NewGuid().ToString("N"))
            }));
        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.UploadAsync(new TaxDocumentUploadCommand(
            taxDeclarationId,
            documentType,
            $"{documentType}.pdf",
            "application/pdf",
            stream.Length,
            stream));

        Assert.True(result.Success, result.ErrorMessage);
    }

    private sealed record WorkflowSetup(Guid DeclarationId, Guid PreparerId, Guid Approver1Id, Guid Approver2Id, Guid Approver3Id);

    private sealed class TestCurrentUserService(Guid userId, IReadOnlyCollection<string> roles) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public string? Login => "test.user";
        public string? DisplayName => "Test User";
        public string? Email => "test.user@local";
        public Guid? DepartmentId => null;
        public bool IsAuthenticated => true;
        public bool IsActive => true;
        public IReadOnlyCollection<string> Roles => roles;

        public bool IsInRole(string role)
        {
            return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}
