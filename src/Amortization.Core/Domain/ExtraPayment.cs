namespace Amortization.Core.Domain;

/// <summary>
/// A one-time extra principal payment on a specific date.
/// </summary>
public sealed class ExtraPayment
{
    public DateTime Date { get; }
    public decimal Amount { get; }

    public ExtraPayment(DateTime date, decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Date = date.Date;
        Amount = amount;
    }
}
