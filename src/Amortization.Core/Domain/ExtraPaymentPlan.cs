namespace Amortization.Core.Domain;

/// <summary>
/// Plan for extra principal: recurring monthly amount plus optional lump sums on specific dates.
/// </summary>
public sealed class ExtraPaymentPlan
{
    public decimal RecurringExtraPrincipal { get; }
    public IReadOnlyList<ExtraPayment> LumpSums { get; }

    public ExtraPaymentPlan(decimal recurringExtraPrincipal, IReadOnlyList<ExtraPayment>? lumpSums = null)
    {
        if (recurringExtraPrincipal < 0) throw new ArgumentOutOfRangeException(nameof(recurringExtraPrincipal));
        RecurringExtraPrincipal = recurringExtraPrincipal;
        LumpSums = lumpSums ?? Array.Empty<ExtraPayment>();
    }

    public static ExtraPaymentPlan Zero => new(0);
}
