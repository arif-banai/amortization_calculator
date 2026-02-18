using Amortization.Core.Domain;

namespace Amortization.App.ViewModels;

public sealed class ScheduleRowViewModel
{
    public ScheduleRowViewModel(ScheduleRow row)
    {
        PaymentNumber = row.PaymentNumber;
        PaymentDate = row.PaymentDate;
        ScheduledPayment = row.ScheduledPayment;
        Interest = row.Interest;
        ScheduledPrincipal = row.ScheduledPrincipal;
        ExtraPrincipal = row.ExtraPrincipal;
        TotalPrincipal = row.TotalPrincipal;
        EndingBalance = row.EndingBalance;
        CumulativeInterest = row.CumulativeInterest;
        CumulativeTotalPaid = row.CumulativeTotalPaid;
        CumulativePrincipal = row.CumulativePrincipal;
        PercentPaidOff = row.PercentPaidOff;
        InterestPercentOfPayment = row.InterestPercentOfPayment;
    }

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
    /// <summary>Loan-to-value (EndingBalance / PropertyValue * 100). Set by MainViewModel when property value is available.</summary>
    public decimal? LoanToValue { get; set; }
}
