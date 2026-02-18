using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Amortization.App.Services;

public sealed class SnackbarNotificationService : INotificationService
{
    private readonly ISnackbarService _snackbar;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);

    public SnackbarNotificationService(ISnackbarService snackbar)
    {
        _snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
    }

    public void ShowSuccess(string title, string message)
    {
        _snackbar.Show(title, message, ControlAppearance.Success, DefaultTimeout);
    }

    public void ShowError(string title, string message)
    {
        _snackbar.Show(title, message, ControlAppearance.Danger, DefaultTimeout);
    }
}
