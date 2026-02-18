using Amortization.Core.Domain;

namespace Amortization.Core.Engine;

/// <summary>
/// Generates amortization schedules for fixed-rate loans with optional extra payments.
/// StartDate is the first payment date; payment #n is StartDate.AddMonths(n-1).
/// </summary>
public sealed class AmortizationEngine
{
    /// <summary>
    /// Generate the base schedule with no extra payments.
    /// </summary>
    public ScheduleResult GenerateBaseSchedule(LoanTerms terms, CalcOptions options)
        => GenerateSchedule(terms, ExtraPaymentPlan.Zero, options);

    /// <summary>
    /// Generate the full schedule for the given terms, extra payment plan, and options.
    /// </summary>
    public ScheduleResult GenerateSchedule(LoanTerms terms, ExtraPaymentPlan extras, CalcOptions options)
    {
        decimal r = terms.AnnualInterestRatePercent == 0
            ? 0m
            : (terms.AnnualInterestRatePercent / 100m) / 12m;
        int n = terms.TermMonths;
        decimal scheduledPayment = ComputeMonthlyPayment(terms.Principal, r, n);
        var rows = new List<ScheduleRow>();
        decimal balance = terms.Principal;
        decimal cumulativeInterest = 0m;
        decimal cumulativeTotalPaid = 0m;
        decimal cumulativePrincipal = 0m;
        int paymentNumber = 0;
        DateTime paymentDate;
        int decimals = options.CurrencyDecimals;

        while (balance > 0)
        {
            paymentNumber++;
            paymentDate = terms.StartDate.AddMonths(paymentNumber - 1);
            bool isLastScheduledPayment = paymentNumber >= terms.TermMonths;

            decimal interest = RoundMoney(balance * r, decimals);
            decimal scheduledPrincipal = RoundMoney(scheduledPayment - interest, decimals);
            decimal lumpSum = ExtraPaymentMatcher.GetLumpSumForDate(paymentDate, extras);
            decimal extraPrincipal = RoundMoney(extras.RecurringExtraPrincipal + lumpSum, decimals);
            decimal totalPrincipalRaw = scheduledPrincipal + extraPrincipal;
            if (totalPrincipalRaw < 0) totalPrincipalRaw = 0;
            decimal totalPrincipal = totalPrincipalRaw > balance ? balance : totalPrincipalRaw;
            if (isLastScheduledPayment)
                totalPrincipal = balance;

            decimal endingBalance = RoundMoney(balance - totalPrincipal, decimals);
            decimal actualScheduledPayment = scheduledPayment;

            if (endingBalance <= 0 || isLastScheduledPayment)
            {
                endingBalance = 0m;
                totalPrincipal = balance;
                actualScheduledPayment = RoundMoney(interest + totalPrincipal, decimals);
                cumulativeInterest += interest;
                cumulativeTotalPaid += actualScheduledPayment + extraPrincipal;
                cumulativePrincipal += totalPrincipal;
                decimal percentPaidOff = terms.Principal > 0 ? RoundMoney(cumulativePrincipal / terms.Principal * 100m, decimals) : 100m;
                decimal totalPaymentThisPeriod = actualScheduledPayment + extraPrincipal;
                decimal interestPercent = totalPaymentThisPeriod > 0 ? RoundMoney(interest / totalPaymentThisPeriod * 100m, decimals) : 0m;
                decimal lastScheduledPrincipal = totalPrincipal - extraPrincipal;
                if (lastScheduledPrincipal < 0) lastScheduledPrincipal = 0;
                rows.Add(new ScheduleRow(
                    paymentNumber,
                    paymentDate,
                    actualScheduledPayment,
                    interest,
                    RoundMoney(lastScheduledPrincipal, decimals),
                    extraPrincipal,
                    totalPrincipal,
                    endingBalance,
                    cumulativeInterest,
                    cumulativeTotalPaid,
                    cumulativePrincipal,
                    percentPaidOff,
                    interestPercent));
                break;
            }

            cumulativeInterest += interest;
            cumulativeTotalPaid += actualScheduledPayment + extraPrincipal;
            cumulativePrincipal += totalPrincipal;
            decimal percentPaidOffNormal = terms.Principal > 0 ? RoundMoney(cumulativePrincipal / terms.Principal * 100m, decimals) : 100m;
            decimal totalPaymentNormal = actualScheduledPayment + extraPrincipal;
            decimal interestPercentNormal = totalPaymentNormal > 0 ? RoundMoney(interest / totalPaymentNormal * 100m, decimals) : 0m;
            rows.Add(new ScheduleRow(
                paymentNumber,
                paymentDate,
                actualScheduledPayment,
                interest,
                scheduledPrincipal,
                extraPrincipal,
                totalPrincipal,
                endingBalance,
                cumulativeInterest,
                cumulativeTotalPaid,
                cumulativePrincipal,
                percentPaidOffNormal,
                interestPercentNormal));
            balance = endingBalance;
        }

        var lastRow = rows[^1];
        var summary = new ScheduleSummary(
            scheduledPayment,
            rows.Count,
            lastRow.CumulativeInterest,
            terms.Principal + lastRow.CumulativeInterest,
            lastRow.PaymentDate);

        return new ScheduleResult(rows, summary);
    }

    /// <summary>
    /// Compute the fixed monthly payment. r = monthly rate, n = number of payments.
    /// If r == 0: payment = Principal / n. Else: P * r / (1 - (1+r)^(-n)).
    /// </summary>
    public static decimal ComputeMonthlyPayment(decimal principal, decimal r, int n)
    {
        if (principal <= 0 || n <= 0) throw new ArgumentOutOfRangeException(nameof(principal));
        if (r == 0)
            return decimal.Round(principal / n, 2, MidpointRounding.AwayFromZero);
        decimal factor = (decimal)Math.Pow((double)(1 + r), -n);
        decimal payment = principal * r / (1 - factor);
        return decimal.Round(payment, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundMoney(decimal value, int decimals)
    {
        return decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
}
