using System.ComponentModel;
using Amortization.Core.Domain;

namespace Amortization.App.ViewModels;

public sealed class ScheduleSummaryViewModel : INotifyPropertyChanged
{
    private decimal _monthlyPayment;
    private int _totalPayments;
    private decimal _totalInterest;
    private decimal _totalPaid;
    private DateTime _payoffDate;

    public decimal MonthlyPayment { get => _monthlyPayment; set { _monthlyPayment = value; OnPropertyChanged(nameof(MonthlyPayment)); } }
    public int TotalPayments { get => _totalPayments; set { _totalPayments = value; OnPropertyChanged(nameof(TotalPayments)); } }
    public decimal TotalInterest { get => _totalInterest; set { _totalInterest = value; OnPropertyChanged(nameof(TotalInterest)); } }
    public decimal TotalPaid { get => _totalPaid; set { _totalPaid = value; OnPropertyChanged(nameof(TotalPaid)); } }
    public DateTime PayoffDate { get => _payoffDate; set { _payoffDate = value; OnPropertyChanged(nameof(PayoffDate)); } }

    public void SetFrom(ScheduleSummary? summary)
    {
        if (summary == null) return;
        MonthlyPayment = summary.MonthlyPayment;
        TotalPayments = summary.TotalPayments;
        TotalInterest = summary.TotalInterest;
        TotalPaid = summary.TotalPaid;
        PayoffDate = summary.PayoffDate;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
