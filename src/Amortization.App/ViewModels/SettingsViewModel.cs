using System.Windows;
using Amortization.App.Models;
using Amortization.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amortization.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private TextSizeKind _textSize;

    [ObservableProperty]
    private bool _useZoom;

    [ObservableProperty]
    private double _zoom;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        var s = _settingsService.Current;
        _textSize = s.TextSize;
        _useZoom = s.UseZoom;
        _zoom = s.Zoom;
    }

    public string ZoomPercentText => $"{Zoom * 100:F0}%";

    public TextSizeKind[] TextSizeOptions { get; } = [TextSizeKind.Small, TextSizeKind.Medium, TextSizeKind.Large];

    partial void OnTextSizeChanged(TextSizeKind value) => ApplyAndSave();
    partial void OnUseZoomChanged(bool value) => ApplyAndSave();
    partial void OnZoomChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentText));
        ApplyAndSave();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        TextSize = TextSizeKind.Medium;
        Zoom = 1.0;
        UseZoom = false;
    }

    private void ApplyAndSave()
    {
        var s = _settingsService.Current;
        s.TextSize = TextSize;
        s.UseZoom = UseZoom;
        s.Zoom = Zoom;
        _settingsService.Save();
        Application.Current.Resources["AppFontSize"] = s.TextSizeToFontSize();
    }
}
