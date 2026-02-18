using System.ComponentModel;

namespace Amortization.App.Models;

/// <summary>
/// Text size options mapped to font size (Small=12, Medium=14, Large=16).
/// </summary>
public enum TextSizeKind
{
    Small = 12,
    Medium = 14,
    Large = 16
}

public sealed class AppSettings : INotifyPropertyChanged
{
    private TextSizeKind _textSize = TextSizeKind.Medium;
    private double _zoom = 1.0;
    private bool _useZoom;

    public TextSizeKind TextSize
    {
        get => _textSize;
        set { _textSize = value; OnPropertyChanged(nameof(TextSize)); }
    }

    public double Zoom
    {
        get => _zoom;
        set { _zoom = value; OnPropertyChanged(nameof(Zoom)); OnPropertyChanged(nameof(EffectiveZoom)); }
    }

    public bool UseZoom
    {
        get => _useZoom;
        set { _useZoom = value; OnPropertyChanged(nameof(UseZoom)); OnPropertyChanged(nameof(EffectiveZoom)); }
    }

    /// <summary>Effective scale for layout (UseZoom ? Zoom : 1.0).</summary>
    public double EffectiveZoom => UseZoom ? Zoom : 1.0;

    /// <summary>Returns the font size (double) for the current TextSize.</summary>
    public double TextSizeToFontSize() => (double)(int)TextSize;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
