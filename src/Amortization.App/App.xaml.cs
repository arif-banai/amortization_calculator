using System.Windows;
using System.Windows.Threading;

namespace Amortization.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void App_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var main = new MainWindow();
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
