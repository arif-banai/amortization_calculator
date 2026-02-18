using CommunityToolkit.Mvvm.ComponentModel;

namespace Amortization.App.ViewModels;

public sealed partial class ExtraPaymentRowViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private string _amountText = "";
}
