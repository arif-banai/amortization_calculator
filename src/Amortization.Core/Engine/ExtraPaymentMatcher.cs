using Amortization.Core.Domain;

namespace Amortization.Core.Engine;

/// <summary>
/// Returns the sum of lump-sum extra payments that apply to a given payment date.
/// A lump sum applies to the first scheduled payment on or after the lump sum date.
/// E.g. if payments are on the 18th, a lump sum on the 10th applies to that month's payment (18th).
/// </summary>
public static class ExtraPaymentMatcher
{
    public static decimal GetLumpSumForDate(DateTime paymentDate, ExtraPaymentPlan plan)
    {
        if (plan.LumpSums.Count == 0) return 0m;
        var paymentDay = paymentDate.Date;
        var previousPaymentDay = paymentDate.AddMonths(-1).Date;
        decimal sum = 0m;
        foreach (var lump in plan.LumpSums)
        {
            var d = lump.Date.Date;
            if (d > previousPaymentDay && d <= paymentDay)
                sum += lump.Amount;
        }
        return sum;
    }
}
