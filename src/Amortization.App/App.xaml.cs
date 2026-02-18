using System.Windows;
using System.Windows.Threading;
using Amortization.App.Services;

namespace Amortization.App;

public partial class App : Application
{
    private ISettingsService? _settingsService;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public ISettingsService SettingsService => _settingsService ?? throw new InvalidOperationException("Settings not initialized.");

    private void App_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            _settingsService = new SettingsService();
            _settingsService.Load();
            ApplyFontSizeFromSettings();
            _settingsService.SettingsChanged += (_, _) => ApplyFontSizeFromSettings();

            var main = new MainWindow(_settingsService);
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The application failed to start.\n\n" + ex.ToString(),
                "Amortization Calculator - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ApplyFontSizeFromSettings()
    {
        if (_settingsService == null) return;
        var fontSize = _settingsService.Current.TextSizeToFontSize();
        Current.Resources["AppFontSize"] = fontSize;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "An error occurred.\n\n" + e.Exception.ToString(),
            "Amortization Calculator - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
        Shutdown(1);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        MessageBox.Show(
            "An unexpected error occurred.\n\n" + ex.ToString(),
            "Amortization Calculator - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(1);
    }
}
