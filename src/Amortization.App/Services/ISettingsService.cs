using Amortization.App.Models;

namespace Amortization.App.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Load();
    void Save();
    /// <summary>Raised when settings are saved so the app can re-apply font and zoom.</summary>
    event EventHandler? SettingsChanged;
}
