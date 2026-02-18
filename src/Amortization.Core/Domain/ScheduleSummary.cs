namespace Amortization.Core.Domain;

/// <summary>
/// Summary of an amortization schedule (payment, totals, payoff date).
/// </summary>
public sealed class ScheduleSummary
{
    public decimal MonthlyPayment { get; }
    public int TotalPayments { get; }
    public decimal TotalInterest { get; }
    public decimal TotalPaid { get; }
    public DateTime PayoffDate { get; }

    public ScheduleSummary(decimal monthlyPayment, int totalPayments, decimal totalInterest, decimal totalPaid, DateTime payoffDate)
    {
        MonthlyPayment = monthlyPayment;
        TotalPayments = totalPayments;
        TotalInterest = totalInterest;
        TotalPaid = totalPaid;
        PayoffDate = payoffDate;
    }
}
