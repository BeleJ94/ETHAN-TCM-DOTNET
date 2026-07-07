using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Common;
using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthanTcm.Infrastructure.Services;

public sealed class TaxDeclarationWorkflowService(
    EthanTcmDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditService auditService)
    : ITaxDeclarationWorkflowService
{
    private const string AuditModule = "Tax Declaration Workflow";

    public async Task<IReadOnlyCollection<TaxDeclarationWorkflowListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await SearchAsync(new TaxDeclarationWorkflowSearchCriteria(PageSize: 100), cancellationToken);
        return page.Items;
    }

    public async Task<TaxDeclarationWorkflowPage> SearchAsync(
        TaxDeclarationWorkflowSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(criteria.PageSize, 10, 100);
        var requestedPage = Math.Max(1, criteria.Page);

        var query =
            from declaration in dbContext.TaxDeclarations.AsNoTracking()
            join obligation in dbContext.TaxObligations.AsNoTracking()
                on declaration.TaxObligationId equals obligation.Id
            join assignedUser in dbContext.Users.AsNoTracking()
                on declaration.AssignedToUserId equals assignedUser.Id
            select new { Declaration = declaration, Obligation = obligation, AssignedUser = assignedUser };

        if (!currentUserService.IsInRole(ApplicationRoles.Administrator) &&
            !currentUserService.IsInRole(ApplicationRoles.TaxManager) &&
            !currentUserService.IsInRole(ApplicationRoles.Auditor))
        {
            if (!currentUserService.UserId.HasValue)
            {
                return new TaxDeclarationWorkflowPage([], 1, pageSize, 0);
            }

            var currentUserId = currentUserService.UserId.Value;
            query = query.Where(item =>
                item.Declaration.AssignedToUserId == currentUserId ||
                dbContext.TaxObligationResponsibles.Any(responsible =>
                    responsible.TaxObligationId == item.Declaration.TaxObligationId &&
                    responsible.UserId == currentUserId));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(item =>
                item.Obligation.Name.Contains(search) ||
                item.Declaration.PeriodLabel.Contains(search) ||
                item.AssignedUser.DisplayName.Contains(search));
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(item => item.Declaration.Status == criteria.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var pageNumber = Math.Min(requestedPage, totalPages);

        var declarations = await query
            .OrderBy(item => item.Declaration.DueDate)
            .ThenBy(item => item.Declaration.PeriodLabel)
            .ThenBy(item => item.Declaration.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Declaration.Id,
                ObligationName = item.Obligation.Name,
                item.Declaration.PeriodLabel,
                item.Declaration.DueDate,
                item.Declaration.ReminderDate,
                item.Declaration.Status,
                item.Declaration.PaymentRequired,
                AssignedTo = item.AssignedUser.DisplayName,
                item.Declaration.PreparedAt
            })
            .ToArrayAsync(cancellationToken);

        var declarationIds = declarations.Select(declaration => declaration.Id).ToArray();
        var preparationProofItems = await dbContext.TaxDocuments
            .AsNoTracking()
            .Where(document =>
                declarationIds.Contains(document.TaxDeclarationId) &&
                document.DocumentType == DocumentType.TaxReturnDraft &&
                !document.IsDeleted)
            .Select(document => new
            {
                document.TaxDeclarationId,
                document.UploadedByUserId,
                document.UploadedAt,
                document.Version
            })
            .ToArrayAsync(cancellationToken);
        var preparationProofs = preparationProofItems
            .GroupBy(document => document.TaxDeclarationId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(document => document.UploadedAt)
                    .ThenByDescending(document => document.Version)
                    .First());

        var userIds = preparationProofs.Values
            .Select(document => document.UploadedByUserId)
            .Distinct()
            .ToArray();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        var items = declarations.Select(declaration =>
        {
            preparationProofs.TryGetValue(declaration.Id, out var preparationProof);

            return new TaxDeclarationWorkflowListItemDto(
                declaration.Id,
                declaration.ObligationName,
                declaration.PeriodLabel,
                declaration.DueDate,
                declaration.ReminderDate,
                declaration.Status,
                declaration.PaymentRequired,
                declaration.AssignedTo,
                declaration.PreparedAt,
                preparationProof?.UploadedByUserId,
                preparationProof is null ? "-" : users.GetValueOrDefault(preparationProof.UploadedByUserId, "-"));
        }).ToArray();

        return new TaxDeclarationWorkflowPage(items, pageNumber, pageSize, totalCount);
    }

    public async Task<TaxDeclarationWorkflowDetailsDto?> GetAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default)
    {
        var declaration = await LoadDeclarationAsync(taxDeclarationId, asNoTracking: true, cancellationToken);
        if (declaration is null)
        {
            return null;
        }

        if (!CanViewDeclaration(declaration))
        {
            return null;
        }

        var activeDocuments = declaration.Documents
            .Where(document => !document.IsDeleted)
            .OrderBy(document => document.UploadedAt)
            .ToArray();
        var preparationProof = activeDocuments
            .Where(document => document.DocumentType == DocumentType.TaxReturnDraft)
            .OrderByDescending(document => document.UploadedAt)
            .ThenByDescending(document => document.Version)
            .FirstOrDefault();
        var approvals = declaration.Approvals
            .OrderBy(approval => approval.ApprovalCycleNumber)
            .ThenBy(approval => approval.ApprovalLevel)
            .ThenBy(approval => approval.DecidedAt)
            .Select(approval => new TaxDeclarationApprovalDto(
                approval.ApprovalCycleNumber,
                approval.ApprovalLevel,
                approval.ApproverUserId,
                approval.Decision,
                approval.Comment,
                approval.DecidedAt))
            .ToArray();
        var currentCycleApprovals = approvals
            .Where(approval => approval.CycleNumber == declaration.ApprovalCycleNumber)
            .ToArray();
        var expectedApproverIds = Enumerable.Range(1, 3)
            .SelectMany(level => GetExpectedApproverIds(declaration, level))
            .ToArray();
        var expectedSubmissionOwnerIds = GetResponsibleIds(declaration, ResponsibleType.SubmissionProcessOwner);
        var expectedPaymentOwnerIds = GetResponsibleIds(declaration, ResponsibleType.PaymentProcessOwner);
        var expectedClosureOwnerIds = GetResponsibleIds(declaration, ResponsibleType.FollowUpOwner);
        var userIds = new[] { declaration.AssignedToUserId }
            .Concat(new[] { declaration.SubmittedByUserId, declaration.ClosedByUserId }.OfType<Guid>())
            .Concat(activeDocuments.Select(document => document.UploadedByUserId))
            .Concat(approvals.Select(approval => approval.ApproverUserId))
            .Concat(expectedApproverIds)
            .Concat(expectedSubmissionOwnerIds)
            .Concat(expectedPaymentOwnerIds)
            .Concat(expectedClosureOwnerIds)
            .Concat(declaration.Payments.Select(payment => payment.PaidByUserId).OfType<Guid>())
            .Distinct()
            .ToArray();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        var approvalSteps = BuildApprovalSteps(declaration, currentCycleApprovals, users);

        return new TaxDeclarationWorkflowDetailsDto(
            declaration.Id,
            declaration.TaxObligation?.Name ?? "-",
            declaration.PeriodLabel,
            declaration.DueDate,
            declaration.ReminderDate,
            declaration.Status,
            declaration.PaymentRequired,
            declaration.AssignedToUserId,
            users.GetValueOrDefault(declaration.AssignedToUserId, "-"),
            declaration.PreparedAt,
            preparationProof?.UploadedByUserId,
            preparationProof is null ? "-" : users.GetValueOrDefault(preparationProof.UploadedByUserId, "-"),
            declaration.SubmissionReference,
            declaration.SubmittedAt,
            declaration.SubmittedByUserId,
            declaration.SubmittedByUserId.HasValue ? users.GetValueOrDefault(declaration.SubmittedByUserId.Value, "-") : "-",
            declaration.ClosedAt,
            declaration.ClosedByUserId,
            declaration.ClosedByUserId.HasValue ? users.GetValueOrDefault(declaration.ClosedByUserId.Value, "-") : "-",
            FormatResponsibleNames(expectedSubmissionOwnerIds, users, "No submission owner configured"),
            FormatResponsibleNames(expectedPaymentOwnerIds, users, declaration.PaymentRequired ? "No payment owner configured" : "Not required"),
            CanRecordPayment(declaration),
            FormatResponsibleNames(expectedClosureOwnerIds, users, "Tax manager"),
            CanCloseDeclaration(declaration),
            approvalSteps,
            approvals,
            declaration.Payments
                .OrderBy(payment => payment.PaidAt)
                .Select(payment => new TaxDeclarationPaymentDto(
                    payment.Amount,
                    payment.Currency,
                    payment.PaymentReference,
                    payment.PaidByUserId,
                    payment.PaidByUserId.HasValue ? users.GetValueOrDefault(payment.PaidByUserId.Value, "-") : "-",
                    payment.PaidAt))
                .ToArray(),
            activeDocuments
                .Select(document => new TaxDeclarationDocumentDto(
                    document.Id,
                    document.DocumentType,
                    document.FileName,
                    document.FilePath,
                    document.ContentType,
                    document.FileSizeBytes,
                    document.Version,
                    document.IsDeleted,
                    document.UploadedAt,
                    document.UploadedByUserId,
                    users.GetValueOrDefault(document.UploadedByUserId, "-")))
                .ToArray());
    }

    public Task<TaxDeclarationWorkflowResult> StartPreparationAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "StartPreparation", declaration =>
        {
            EnsureCanPrepare(declaration);
            declaration.StartPreparation(DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> SubmitForReviewAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "SubmitForReview", declaration =>
        {
            EnsureCanPrepare(declaration);
            EnsureActiveDocument(declaration, DocumentType.TaxReturnDraft, "Preparation proof is required before submitting a declaration for review.");
            declaration.SubmitForReview(DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> ApproveAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "Approve", declaration =>
        {
            EnsureCanApprove(declaration);
            declaration.ApproveNextLevel(RequireCurrentUserId(), DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> RejectAsync(Guid taxDeclarationId, string comment, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "Reject", declaration =>
        {
            EnsureCanApprove(declaration);
            declaration.Reject(RequireCurrentUserId(), comment, DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> MarkSubmittedAsync(Guid taxDeclarationId, string submissionReference, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "MarkSubmitted", declaration =>
        {
            EnsureCanSubmit(declaration);
            EnsureActiveDocument(declaration, DocumentType.SubmissionProof, "Submission proof is required before marking a declaration as submitted.");
            declaration.MarkSubmitted(submissionReference, DateTimeOffset.UtcNow, RequireCurrentUserId());
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> MarkPaidAsync(Guid taxDeclarationId, decimal amount, string currency, string paymentReference, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "MarkPaid", declaration =>
        {
            EnsureCanRecordPayment(declaration);
            EnsureActiveDocument(declaration, DocumentType.PaymentProof, "Payment proof is required before marking a declaration as paid.");
            declaration.AddPayment(amount, currency, paymentReference, DateTimeOffset.UtcNow, RequireCurrentUserId());
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> AttachDocumentAsync(TaxDeclarationDocumentCommand command, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(command.TaxDeclarationId, "AttachDocument", declaration =>
        {
            EnsureRole(ApplicationRoles.Preparer, ApplicationRoles.TaxManager, ApplicationRoles.FinanceManager, ApplicationRoles.Administrator);
            declaration.AddDocument(
                command.DocumentType,
                command.FileName,
                command.FilePath,
                command.ContentType,
                RequireCurrentUserId(),
                DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    public async Task<TaxDeclarationWorkflowResult> ReassignAsync(TaxDeclarationReassignmentCommand command, CancellationToken cancellationToken = default)
    {
        var declaration = await LoadDeclarationAsync(command.TaxDeclarationId, asNoTracking: false, cancellationToken);
        if (declaration is null)
        {
            return new TaxDeclarationWorkflowResult(false, "Tax declaration was not found.");
        }

        try
        {
            EnsureRole(ApplicationRoles.TaxManager, ApplicationRoles.Administrator);

            if (command.AssignedToUserId == Guid.Empty)
            {
                return new TaxDeclarationWorkflowResult(false, "Assigned user is required.");
            }

            if (string.IsNullOrWhiteSpace(command.Comment))
            {
                return new TaxDeclarationWorkflowResult(false, "Reassignment comment is required.");
            }

            var userExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == command.AssignedToUserId && user.IsActive, cancellationToken);

            if (!userExists)
            {
                return new TaxDeclarationWorkflowResult(false, "Assigned user was not found or is inactive.");
            }

            var oldAssignedToUserId = declaration.AssignedToUserId;
            declaration.Reassign(command.AssignedToUserId, DateTimeOffset.UtcNow);
            AddReassignmentAudit(declaration, oldAssignedToUserId, command.AssignedToUserId, command.Comment.Trim());
            await dbContext.SaveChangesAsync(cancellationToken);
            return new TaxDeclarationWorkflowResult(true, null);
        }
        catch (DomainException ex)
        {
            return new TaxDeclarationWorkflowResult(false, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new TaxDeclarationWorkflowResult(false, ex.Message);
        }
    }

    public Task<TaxDeclarationWorkflowResult> CloseAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "Close", declaration =>
        {
            EnsureCanCloseDeclaration(declaration);
            declaration.Close(DateTimeOffset.UtcNow, RequireCurrentUserId());
        }, cancellationToken);
    }

    public Task<TaxDeclarationWorkflowResult> CancelAsync(Guid taxDeclarationId, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(taxDeclarationId, "Cancel", declaration =>
        {
            EnsureRole(ApplicationRoles.TaxManager, ApplicationRoles.Administrator);
            declaration.Cancel(DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    private async Task<TaxDeclarationWorkflowResult> ExecuteAsync(
        Guid taxDeclarationId,
        string action,
        Action<TaxDeclaration> operation,
        CancellationToken cancellationToken)
    {
        var declaration = await LoadDeclarationAsync(taxDeclarationId, asNoTracking: false, cancellationToken);
        if (declaration is null)
        {
            return new TaxDeclarationWorkflowResult(false, "Tax declaration was not found.");
        }

        var oldStatus = declaration.Status;

        try
        {
            operation(declaration);
            AddAudit(action, declaration, oldStatus);
            MarkNewWorkflowChildrenAsAdded();
            await dbContext.SaveChangesAsync(cancellationToken);
            return new TaxDeclarationWorkflowResult(true, null);
        }
        catch (DomainException ex)
        {
            return new TaxDeclarationWorkflowResult(false, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new TaxDeclarationWorkflowResult(false, ex.Message);
        }
    }

    private Task<TaxDeclaration?> LoadDeclarationAsync(Guid taxDeclarationId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.TaxDeclarations
            .Include(declaration => declaration.TaxObligation)
            .ThenInclude(obligation => obligation!.Responsibles)
            .Include(declaration => declaration.Approvals)
            .Include(declaration => declaration.Payments)
            .Include(declaration => declaration.Documents)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(declaration => declaration.Id == taxDeclarationId, cancellationToken);
    }

    private IReadOnlyCollection<TaxDeclarationApprovalStepDto> BuildApprovalSteps(
        TaxDeclaration declaration,
        IReadOnlyCollection<TaxDeclarationApprovalDto> approvals,
        IReadOnlyDictionary<Guid, string> users)
    {
        var nextLevel = GetNextApprovalLevel(declaration.Status);
        var currentUserId = currentUserService.UserId;
        var isPrivilegedApprover = currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager);
        var hasApprovalRole = isPrivilegedApprover || currentUserService.IsInRole(ApplicationRoles.Approver);

        return Enumerable.Range(1, 3)
            .Select(level =>
            {
                var expectedType = GetExpectedApproverType(level);
                var expectedApproverIds = GetExpectedApproverIds(declaration, level);
                var decision = approvals.FirstOrDefault(approval => approval.Level == level);
                var status = GetApprovalStepStatus(level, nextLevel, decision);
                var expectedApprovers = expectedApproverIds.Length == 0
                    ? "No approver configured"
                    : string.Join(", ", expectedApproverIds.Select(userId => users.GetValueOrDefault(userId, "-")));
                var isExpectedApprover = currentUserId.HasValue && expectedApproverIds.Contains(currentUserId.Value);
                var canAct = status == TaxDeclarationApprovalStepStatus.Pending &&
                    currentUserId.HasValue &&
                    (isExpectedApprover || isPrivilegedApprover || expectedApproverIds.Length == 0 && hasApprovalRole) &&
                    (isPrivilegedApprover || currentUserId.Value != declaration.AssignedToUserId) &&
                    !approvals.Any(approval => approval.ApproverUserId == currentUserId.Value);

                return new TaxDeclarationApprovalStepDto(
                    level,
                    $"Approval level {level}",
                    expectedType,
                    expectedApproverIds,
                    expectedApprovers,
                    status,
                    decision?.ApproverUserId,
                    decision is null ? "-" : users.GetValueOrDefault(decision.ApproverUserId, "-"),
                    decision?.Comment,
                    decision?.DecidedAt,
                    canAct);
            })
            .ToArray();
    }

    private static TaxDeclarationApprovalStepStatus GetApprovalStepStatus(
        int level,
        int nextLevel,
        TaxDeclarationApprovalDto? decision)
    {
        if (decision?.Decision == ApprovalDecision.Rejected)
        {
            return TaxDeclarationApprovalStepStatus.Rejected;
        }

        if (decision?.Decision == ApprovalDecision.Approved)
        {
            return TaxDeclarationApprovalStepStatus.Approved;
        }

        return level == nextLevel
            ? TaxDeclarationApprovalStepStatus.Pending
            : TaxDeclarationApprovalStepStatus.NotReached;
    }

    private static int GetNextApprovalLevel(TaxDeclarationStatus status)
    {
        return status switch
        {
            TaxDeclarationStatus.SubmittedForReview => 1,
            TaxDeclarationStatus.ApprovedLevel1 => 2,
            TaxDeclarationStatus.ApprovedLevel2 => 3,
            _ => 0
        };
    }

    private static ResponsibleType GetExpectedApproverType(int level)
    {
        return level switch
        {
            1 => ResponsibleType.Approver1,
            2 => ResponsibleType.Approver2,
            3 => ResponsibleType.Approver3,
            _ => ResponsibleType.Approver
        };
    }

    private static Guid[] GetExpectedApproverIds(TaxDeclaration declaration, int level)
    {
        var expectedType = GetExpectedApproverType(level);
        var expectedApprovers = declaration.TaxObligation?.Responsibles
            .Where(responsible => responsible.Type == expectedType)
            .Select(responsible => responsible.UserId)
            .ToArray() ?? [];

        if (expectedApprovers.Length > 0)
        {
            return expectedApprovers;
        }

        return declaration.TaxObligation?.Responsibles
            .Where(responsible => responsible.Type == ResponsibleType.Approver)
            .Select(responsible => responsible.UserId)
            .ToArray() ?? [];
    }

    private static Guid[] GetResponsibleIds(TaxDeclaration declaration, ResponsibleType type)
    {
        return declaration.TaxObligation?.Responsibles
            .Where(responsible => responsible.Type == type)
            .Select(responsible => responsible.UserId)
            .ToArray() ?? [];
    }

    private static string FormatResponsibleNames(
        IReadOnlyCollection<Guid> responsibleIds,
        IReadOnlyDictionary<Guid, string> users,
        string fallback)
    {
        return responsibleIds.Count == 0
            ? fallback
            : string.Join(", ", responsibleIds.Select(userId => users.GetValueOrDefault(userId, "-")));
    }

    private void EnsureCanPrepare(TaxDeclaration declaration)
    {
        EnsureRole(ApplicationRoles.Preparer, ApplicationRoles.TaxManager, ApplicationRoles.Administrator);

        if (!currentUserService.IsInRole(ApplicationRoles.Administrator) &&
            !currentUserService.IsInRole(ApplicationRoles.TaxManager) &&
            !IsAssignedOrConfiguredPreparer(declaration))
        {
            throw new InvalidOperationException("Only the assigned or configured preparer can perform this action.");
        }
    }

    private bool CanViewDeclaration(TaxDeclaration declaration)
    {
        if (currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager) ||
            currentUserService.IsInRole(ApplicationRoles.Auditor))
        {
            return true;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return false;
        }

        var currentUserId = currentUserService.UserId.Value;
        return declaration.AssignedToUserId == currentUserId ||
            declaration.TaxObligation?.Responsibles.Any(responsible => responsible.UserId == currentUserId) == true;
    }

    private void EnsureCanApprove(TaxDeclaration declaration)
    {
        var currentUserId = RequireCurrentUserId();
        var isPrivilegedApprover = currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager);

        if (currentUserId == declaration.AssignedToUserId &&
            !isPrivilegedApprover)
        {
            throw new InvalidOperationException("The assigned preparer cannot approve this declaration.");
        }

        if (declaration.Approvals.Any(approval =>
                approval.ApprovalCycleNumber == declaration.ApprovalCycleNumber &&
                approval.ApproverUserId == currentUserId))
        {
            throw new InvalidOperationException("The same approver cannot approve multiple levels of a declaration.");
        }

        var nextLevel = declaration.NextApprovalLevel();
        var expectedApprovers = GetExpectedApproverIds(declaration, nextLevel);
        var isExpectedApprover = expectedApprovers.Contains(currentUserId);
        var hasGlobalApprovalRole = currentUserService.IsInRole(ApplicationRoles.Approver);

        if (!isPrivilegedApprover &&
            !isExpectedApprover &&
            !(expectedApprovers.Length == 0 && hasGlobalApprovalRole))
        {
            throw new InvalidOperationException($"Only the configured level {nextLevel} approver can approve this declaration.");
        }
    }

    private void EnsureCanSubmit(TaxDeclaration declaration)
    {
        EnsureRole(ApplicationRoles.Preparer, ApplicationRoles.TaxManager, ApplicationRoles.Administrator);

        if (!currentUserService.IsInRole(ApplicationRoles.Administrator) &&
            !currentUserService.IsInRole(ApplicationRoles.TaxManager) &&
            !IsAssignedOrConfiguredPreparer(declaration))
        {
            throw new InvalidOperationException("Only the assigned or configured preparer can submit this declaration.");
        }
    }

    private bool IsAssignedOrConfiguredPreparer(TaxDeclaration declaration)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return false;
        }

        var currentUserId = currentUserService.UserId.Value;
        return declaration.AssignedToUserId == currentUserId ||
            declaration.TaxObligation?.Responsibles.Any(responsible =>
                responsible.Type == ResponsibleType.Preparer &&
                responsible.UserId == currentUserId) == true;
    }

    private bool CanRecordPayment(TaxDeclaration declaration)
    {
        if (!declaration.PaymentRequired ||
            declaration.Status != TaxDeclarationStatus.PaymentPending ||
            !currentUserService.UserId.HasValue)
        {
            return false;
        }

        if (currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager) ||
            currentUserService.IsInRole(ApplicationRoles.FinanceManager))
        {
            return true;
        }

        var currentUserId = currentUserService.UserId.Value;
        return declaration.TaxObligation?.Responsibles.Any(responsible =>
            responsible.Type == ResponsibleType.PaymentProcessOwner &&
            responsible.UserId == currentUserId) == true;
    }

    private void EnsureCanRecordPayment(TaxDeclaration declaration)
    {
        if (CanRecordPayment(declaration))
        {
            return;
        }

        throw new InvalidOperationException("Only the configured payment process owner, finance manager, tax manager or administrator can record payment.");
    }

    private bool CanCloseDeclaration(TaxDeclaration declaration)
    {
        if (declaration.Status is not (TaxDeclarationStatus.Submitted or TaxDeclarationStatus.Paid) ||
            !currentUserService.UserId.HasValue)
        {
            return false;
        }

        if (currentUserService.IsInRole(ApplicationRoles.Administrator) ||
            currentUserService.IsInRole(ApplicationRoles.TaxManager))
        {
            return true;
        }

        var currentUserId = currentUserService.UserId.Value;
        return declaration.TaxObligation?.Responsibles.Any(responsible =>
            responsible.Type == ResponsibleType.FollowUpOwner &&
            responsible.UserId == currentUserId) == true;
    }

    private void EnsureCanCloseDeclaration(TaxDeclaration declaration)
    {
        if (CanCloseDeclaration(declaration))
        {
            return;
        }

        throw new InvalidOperationException("Only the configured follow-up owner, tax manager or administrator can close this declaration.");
    }

    private static void EnsureActiveDocument(TaxDeclaration declaration, DocumentType documentType, string message)
    {
        if (declaration.Documents.Any(document => document.DocumentType == documentType && !document.IsDeleted))
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private Guid RequireCurrentUserId()
    {
        return currentUserService.UserId ?? throw new InvalidOperationException("Current user is required.");
    }

    private void EnsureRole(params string[] roles)
    {
        if (roles.Any(currentUserService.IsInRole))
        {
            return;
        }

        throw new InvalidOperationException("The current user is not allowed to perform this action.");
    }

    private void AddAudit(string action, TaxDeclaration declaration, TaxDeclarationStatus oldStatus)
    {
        auditService.Add(new AuditEntry(
            action,
            nameof(TaxDeclaration),
            declaration.Id.ToString(),
            new { Status = oldStatus },
            new
            {
                declaration.Status,
                declaration.SubmissionReference,
                declaration.PaymentRequired,
                declaration.AssignedToUserId
            },
            AuditModule,
            "Web"));
    }

    private void AddReassignmentAudit(TaxDeclaration declaration, Guid oldAssignedToUserId, Guid newAssignedToUserId, string comment)
    {
        auditService.Add(new AuditEntry(
            "Reassign",
            nameof(TaxDeclaration),
            declaration.Id.ToString(),
            new
            {
                declaration.Status,
                AssignedToUserId = oldAssignedToUserId
            },
            new
            {
                declaration.Status,
                AssignedToUserId = newAssignedToUserId,
                Comment = comment
            },
            AuditModule,
            "Web"));
    }

    private void MarkNewWorkflowChildrenAsAdded()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<TaxDeclarationApproval>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<TaxPayment>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<TaxDocument>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }
    }
}
