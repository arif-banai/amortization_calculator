using System.Windows;

namespace Amortization.App.Helpers;

/// <summary>
/// Freezable proxy that holds a reference to the DataContext so bindings work
/// for elements outside the visual tree (e.g. DataGridColumn).
/// </summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data),
        typeof(object),
        typeof(BindingProxy),
        new PropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
