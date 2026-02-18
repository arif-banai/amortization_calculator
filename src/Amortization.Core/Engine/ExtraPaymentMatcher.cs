using Amortization.Core.Domain;

namespace Amortization.Core.Engine;

/// <summary>
/// Returns the sum of lump-sum extra payments that apply to a given payment date.
/// v1: exact date match (Date.Date equals paymentDate.Date). Future: month/year matching.
/// </summary>
public static class ExtraPaymentMatcher
{
    public static decimal GetLumpSumForDate(DateTime paymentDate, ExtraPaymentPlan plan)
    {
        if (plan.LumpSums.Count == 0) return 0m;
        var date = paymentDate.Date;
        decimal sum = 0m;
        foreach (var lump in plan.LumpSums)
        {
            if (lump.Date.Date == date)
                sum += lump.Amount;
        }
        return sum;
    }
}
