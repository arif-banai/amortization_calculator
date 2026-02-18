using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amortization.App.Converters;

/// <summary>
/// Non-empty string -> Visible, empty/null -> Collapsed.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
