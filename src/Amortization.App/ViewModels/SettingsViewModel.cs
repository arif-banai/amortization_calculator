using Amortization.App.Models;
using Amortization.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amortization.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private double _zoom;

    [ObservableProperty]
    private AppTheme _theme;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _zoom = _settingsService.Current.Zoom;
        _theme = _settingsService.Current.Theme;
    }

    public string ScalePercentText => $"{Zoom * 100:F0}%";

    partial void OnZoomChanged(double value)
    {
        OnPropertyChanged(nameof(ScalePercentText));
        ApplyAndSave();
    }

    partial void OnThemeChanged(AppTheme value)
    {
        App.SetTheme(value);
        ApplyAndSave();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        Zoom = 1.0;
        Theme = AppTheme.Light;
    }

    private void ApplyAndSave()
    {
        _settingsService.Current.Zoom = Zoom;
        _settingsService.Current.Theme = Theme;
        _settingsService.Save();
    }
}
