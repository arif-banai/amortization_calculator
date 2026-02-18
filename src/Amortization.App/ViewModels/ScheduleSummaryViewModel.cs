using Amortization.Core.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Amortization.App.ViewModels;

public sealed partial class ScheduleSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _monthlyPayment;

    [ObservableProperty]
    private int _totalPayments;

    [ObservableProperty]
    private decimal _totalInterest;

    [ObservableProperty]
    private decimal _totalPaid;

    [ObservableProperty]
    private DateTime _payoffDate;

    public void SetFrom(ScheduleSummary? summary)
    {
        if (summary == null) return;
        MonthlyPayment = summary.MonthlyPayment;
        TotalPayments = summary.TotalPayments;
        TotalInterest = summary.TotalInterest;
        TotalPaid = summary.TotalPaid;
        PayoffDate = summary.PayoffDate;
    }
}
