using System.Globalization;
using System.Windows.Data;
using Amortization.App.ViewModels;

namespace Amortization.App.Converters;

public sealed class ScenarioToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SelectedScenario scenario && parameter is string param)
            return param == "Base" ? scenario == SelectedScenario.Base : scenario == SelectedScenario.WithExtras;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string param)
            return param == "Base" ? SelectedScenario.Base : SelectedScenario.WithExtras;
        return Binding.DoNothing;
    }
}
