namespace Amortization.Core.Engine;

/// <summary>
/// Currency rounding policy: 2 decimals, midpoint away from zero.
/// </summary>
public static class Rounding
{
    private const int CurrencyDecimals = 2;
    private static readonly MidpointRounding Mode = MidpointRounding.AwayFromZero;

    public static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, CurrencyDecimals, Mode);
    }

    public static decimal RoundMoney(decimal value, int decimals)
    {
        return decimal.Round(value, decimals, Mode);
    }
}
