using System.ComponentModel;

namespace Amortization.App.Models;

public enum AppTheme
{
    Light,
    Dark
}

public sealed class AppSettings : INotifyPropertyChanged
{
    private double _zoom = 1.0;
    private AppTheme _theme = AppTheme.Light;

    public double Zoom
    {
        get => _zoom;
        set { _zoom = value; OnPropertyChanged(nameof(Zoom)); }
    }

    public AppTheme Theme
    {
        get => _theme;
        set { _theme = value; OnPropertyChanged(nameof(Theme)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
