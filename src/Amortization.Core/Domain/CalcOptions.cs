namespace Amortization.Core.Domain;

/// <summary>
/// When interest is applied relative to the payment (v1: end of period only).
/// </summary>
public enum PaymentTiming
{
    EndOfPeriod
}

/// <summary>
/// How extra payments affect the schedule. KeepPayment = scheduled payment unchanged; loan pays off earlier.
/// </summary>
public enum CalcMode
{
    KeepPayment,
    Recast // placeholder for future
}

/// <summary>
/// Options for schedule generation: rounding and calculation mode.
/// </summary>
public sealed class CalcOptions
{
    public int CurrencyDecimals { get; }
    public PaymentTiming PaymentTiming { get; }
    public CalcMode Mode { get; }

    public CalcOptions(int currencyDecimals = 2, PaymentTiming paymentTiming = PaymentTiming.EndOfPeriod, CalcMode mode = CalcMode.KeepPayment)
    {
        if (currencyDecimals < 0) throw new ArgumentOutOfRangeException(nameof(currencyDecimals));
        CurrencyDecimals = currencyDecimals;
        PaymentTiming = paymentTiming;
        Mode = mode;
    }

    public static CalcOptions Default => new();
}
