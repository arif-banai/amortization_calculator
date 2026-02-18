using System.Windows;

namespace Amortization.App.Services;

/// <summary>
/// Stub implementation using MessageBox until SnackbarPresenter is available (e.g. after FluentWindow).
/// </summary>
public sealed class MessageBoxNotificationService : INotificationService
{
    public void ShowSuccess(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
