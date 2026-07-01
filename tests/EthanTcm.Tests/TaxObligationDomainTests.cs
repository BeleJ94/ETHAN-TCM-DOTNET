using EthanTcm.Domain.Entities;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Tests;

public sealed class TaxObligationDomainTests
{
    [Fact]
    public void Replace_responsibles_allows_existing_preparer_to_remain_primary()
    {
        var preparerId = Guid.NewGuid();
        var obligation = new TaxObligation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            preparerId,
            "VAT Return",
            RiskLevel.Medium,
            requiresPayment: true,
            DateTimeOffset.UtcNow);

        obligation.ReplaceResponsibles(
            [(preparerId, ResponsibleType.Preparer), (Guid.NewGuid(), ResponsibleType.PaymentProcessOwner)],
            DateTimeOffset.UtcNow);

        Assert.Contains(obligation.Responsibles, responsible =>
            responsible.UserId == preparerId &&
            responsible.Type == ResponsibleType.Preparer);
    }
}
