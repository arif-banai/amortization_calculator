using System.Windows;
using Microsoft.Win32;
using Amortization.App.Models;
using Amortization.App.Services;
using Wpf.Ui.Controls;

namespace Amortization.App;

public partial class MainWindow : FluentWindow
{
    private readonly ISettingsService _settingsService;
    private INotificationService _notificationService = null!;

    public AppSettings Settings => _settingsService.Current;

    public MainWindow(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var snackbarService = new Wpf.Ui.SnackbarService();
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        _notificationService = new SnackbarNotificationService(snackbarService);
        var vm = new ViewModels.MainViewModel(_notificationService);
        vm.GetExportFilePath = GetExportFilePath;
        vm.ScrollToRow = index =>
        {
            if (vm.HasExtras)
            {
                var grid = vm.SelectedScheduleTabIndex == 0 ? BaseScheduleGrid : ExtraScheduleGrid;
                var rows = vm.SelectedScheduleTabIndex == 0 ? vm.BaseDisplayScheduleRows : vm.ExtraDisplayScheduleRows;
                if (index >= 0 && index < rows.Count)
                    grid.ScrollIntoView(rows[index]);
            }
            else
            {
                if (index >= 0 && index < vm.BaseDisplayScheduleRows.Count)
                    ScheduleGrid.ScrollIntoView(vm.BaseDisplayScheduleRows[index]);
            }
        };
        DataContext = vm;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsVm = new ViewModels.SettingsViewModel(_settingsService);
        var settingsWindow = new Views.SettingsWindow(settingsVm) { Owner = this };
        settingsWindow.ShowDialog();
    }

    private string? GetExportFilePath()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = "amortization_schedule.csv"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
