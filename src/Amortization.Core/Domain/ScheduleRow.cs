namespace Amortization.Core.Domain;

/// <summary>
/// One row of the amortization schedule.
/// </summary>
public sealed class ScheduleRow
{
    public int PaymentNumber { get; }
    public DateTime PaymentDate { get; }
    public decimal ScheduledPayment { get; }
    public decimal Interest { get; }
    public decimal ScheduledPrincipal { get; }
    public decimal ExtraPrincipal { get; }
    public decimal TotalPrincipal { get; }
    public decimal EndingBalance { get; }
    public decimal CumulativeInterest { get; }
    public decimal CumulativeTotalPaid { get; }
    public decimal CumulativePrincipal { get; }
    public decimal PercentPaidOff { get; }
    public decimal InterestPercentOfPayment { get; }

    public ScheduleRow(
        int paymentNumber,
        DateTime paymentDate,
        decimal scheduledPayment,
        decimal interest,
        decimal scheduledPrincipal,
        decimal extraPrincipal,
        decimal totalPrincipal,
        decimal endingBalance,
        decimal cumulativeInterest,
        decimal cumulativeTotalPaid,
        decimal cumulativePrincipal,
        decimal percentPaidOff,
        decimal interestPercentOfPayment)
    {
        PaymentNumber = paymentNumber;
        PaymentDate = paymentDate;
        ScheduledPayment = scheduledPayment;
        Interest = interest;
        ScheduledPrincipal = scheduledPrincipal;
        ExtraPrincipal = extraPrincipal;
        TotalPrincipal = totalPrincipal;
        EndingBalance = endingBalance;
        CumulativeInterest = cumulativeInterest;
        CumulativeTotalPaid = cumulativeTotalPaid;
        CumulativePrincipal = cumulativePrincipal;
        PercentPaidOff = percentPaidOff;
        InterestPercentOfPayment = interestPercentOfPayment;
    }
}
