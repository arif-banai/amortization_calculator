namespace Amortization.App.Services;

public interface INotificationService
{
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message);
}
