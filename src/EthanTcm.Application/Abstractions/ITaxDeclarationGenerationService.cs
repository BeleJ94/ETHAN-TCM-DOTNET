namespace EthanTcm.Application.Abstractions;

public interface ITaxDeclarationGenerationService
{
    Task<TaxDeclarationGenerationResult> GenerateAnnualAsync(
        int fiscalYear,
        CancellationToken cancellationToken = default);

    Task<TaxDeclarationManualCreationResult> CreateManualAsync(
        TaxDeclarationManualCreationCommand command,
        CancellationToken cancellationToken = default);

    Task<TaxDeclarationManualCreationOptions> GetManualCreationOptionsAsync(
        CancellationToken cancellationToken = default);
}

public sealed record TaxDeclarationGenerationResult(
    int FiscalYear,
    int CreatedDeclarations,
    int SkippedDuplicates,
    int SkippedInactiveObligations,
    IReadOnlyCollection<TaxDeclarationGenerationItem> Items);

public sealed record TaxDeclarationGenerationItem(
    Guid TaxObligationId,
    string ObligationName,
    string PeriodLabel,
    DateOnly DueDate,
    DateOnly? ReminderDate,
    bool Created,
    string? SkipReason);

public sealed record TaxDeclarationManualCreationCommand(
    Guid TaxObligationId,
    DateOnly PeriodDate,
    DateOnly DueDate,
    DateOnly? ReminderDate,
    string PeriodLabel,
    Guid AssignedToUserId,
    bool IsNotApplicable);

public sealed record TaxDeclarationManualCreationResult(
    Guid? TaxDeclarationId,
    bool Created,
    string? ErrorMessage);

public sealed record TaxDeclarationManualCreationOptions(
    IReadOnlyCollection<LookupItemDto> Obligations,
    IReadOnlyCollection<LookupItemDto> Users);
