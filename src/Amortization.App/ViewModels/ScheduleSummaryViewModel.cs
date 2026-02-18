using Amortization.Core.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Amortization.App.ViewModels;

public sealed partial class ScheduleSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _monthlyPayment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveTerm))]
    private int _totalPayments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterestShare))]
    private decimal _totalInterest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterestShare))]
    private decimal _totalPaid;

    [ObservableProperty]
    private DateTime _payoffDate;

    public string EffectiveTerm
    {
        get
        {
            var years = TotalPayments / 12;
            var months = TotalPayments % 12;
            if (years > 0 && months > 0) return $"{years} yr {months} mo";
            if (years > 0) return $"{years} yr";
            if (months > 0) return $"{months} mo";
            return "—";
        }
    }

    public string InterestShare
    {
        get
        {
            if (TotalPaid <= 0) return "—";
            var pct = TotalInterest / TotalPaid * 100;
            return $"{pct:F1}%";
        }
    }

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
