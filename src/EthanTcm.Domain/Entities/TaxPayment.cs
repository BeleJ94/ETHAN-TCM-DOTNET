using EthanTcm.Domain.Common;

namespace EthanTcm.Domain.Entities;

public sealed class TaxPayment : AuditableEntity
{
    private TaxPayment()
    {
    }

    public TaxPayment(Guid taxDeclarationId, decimal amount, string currency, string paymentReference, DateTimeOffset paidAt, Guid? paidByUserId = null)
    {
        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        TaxDeclarationId = EntityGuards.Required(taxDeclarationId, nameof(TaxDeclarationId));
        Amount = amount;
        Currency = EntityGuards.Required(currency, nameof(Currency));
        PaymentReference = EntityGuards.Required(paymentReference, nameof(PaymentReference));
        PaidByUserId = paidByUserId;
        PaidAt = paidAt;
    }

    public Guid TaxDeclarationId { get; private set; }
    public Guid? PaidByUserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string PaymentReference { get; private set; } = string.Empty;
    public DateTimeOffset PaidAt { get; private set; }
}
