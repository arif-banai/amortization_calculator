using System.IO;
using System.Text.Json;
using Amortization.App.Models;

namespace Amortization.App.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;
    private AppSettings _current = new();

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AmortizationCalculator");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Current => _current;

    public event EventHandler? SettingsChanged;

    public void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return;
            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (loaded != null)
            {
                _current = loaded;
                ClampZoom();
            }
        }
        catch
        {
            // Keep defaults on load error
        }
    }

    public void Save()
    {
        try
        {
            ClampZoom();
            var json = JsonSerializer.Serialize(_current, JsonOptions);
            File.WriteAllText(_settingsPath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private void ClampZoom()
    {
        if (_current.Zoom < 0.8) _current.Zoom = 0.8;
        if (_current.Zoom > 1.5) _current.Zoom = 1.5;
    }
}
