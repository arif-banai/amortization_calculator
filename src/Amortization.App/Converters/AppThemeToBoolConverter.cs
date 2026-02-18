using System.Globalization;
using System.Windows.Data;
using Amortization.App.Models;

namespace Amortization.App.Converters;

/// <summary>Converts AppTheme to bool for radio buttons. Parameter is theme name ("Light" or "Dark").</summary>
public sealed class AppThemeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppTheme theme || parameter is not string name)
            return false;
        return Enum.TryParse<AppTheme>(name, true, out var p) && theme == p;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not true || parameter is not string name)
            return Binding.DoNothing;
        return Enum.TryParse<AppTheme>(name, true, out var theme) ? theme : Binding.DoNothing;
    }
}
