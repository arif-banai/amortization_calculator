using System.Windows;
using System.Windows.Threading;
using Amortization.App.Models;
using Amortization.App.Services;
using Wpf.Ui.Appearance;

namespace Amortization.App;

public partial class App : Application
{
    private ISettingsService? _settingsService;
    private const int AppThemeMergedDictionaryIndex = 2;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public ISettingsService SettingsService => _settingsService ?? throw new InvalidOperationException("Settings not initialized.");

    /// <summary>Swaps the app theme at runtime. Call after settings load or when user changes theme.</summary>
    public static void SetTheme(AppTheme theme)
    {
        var dicts = Current.Resources.MergedDictionaries;
        if (dicts.Count <= AppThemeMergedDictionaryIndex) return;

        var wpfUiTheme = theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(wpfUiTheme, Wpf.Ui.Controls.WindowBackdropType.None);

        var uri = theme == AppTheme.Dark
            ? new Uri("pack://application:,,,/Amortization.App;component/Themes/DarkTheme.xaml")
            : new Uri("pack://application:,,,/Amortization.App;component/Themes/LightTheme.xaml");
        dicts[AppThemeMergedDictionaryIndex] = new ResourceDictionary { Source = uri };
    }

    private void App_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            _settingsService = new SettingsService();
            _settingsService.Load();

            SetTheme(_settingsService.Current.Theme);

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
