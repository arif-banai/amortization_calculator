using System.Globalization;
using System.Windows.Data;
using Amortization.App.ViewModels;

namespace Amortization.App.Converters;

public sealed class TableScheduleModeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TableScheduleMode mode && parameter is string param)
        {
            return param switch
            {
                "Base" => mode == TableScheduleMode.Base,
                "WithExtras" => mode == TableScheduleMode.WithExtras,
                _ => false
            };
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string param)
        {
            return param switch
            {
                "Base" => TableScheduleMode.Base,
                "WithExtras" => TableScheduleMode.WithExtras,
                _ => Binding.DoNothing
            };
        }
        return Binding.DoNothing;
    }
}
