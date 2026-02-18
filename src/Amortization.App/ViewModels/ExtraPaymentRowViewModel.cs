using System.ComponentModel;

namespace Amortization.App.ViewModels;

public sealed class ExtraPaymentRowViewModel : INotifyPropertyChanged
{
    private DateTime _date = DateTime.Today;
    private string _amountText = "";

    public DateTime Date
    {
        get => _date;
        set { _date = value; OnPropertyChanged(nameof(Date)); }
    }

    public string AmountText
    {
        get => _amountText;
        set { _amountText = value ?? ""; OnPropertyChanged(nameof(AmountText)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
