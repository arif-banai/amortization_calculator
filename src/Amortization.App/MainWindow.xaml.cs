using System.Windows;
using Microsoft.Win32;

namespace Amortization.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = (ViewModels.MainViewModel)DataContext;
        vm.GetExportFilePath = GetExportFilePath;
        vm.ShowMessage = msg => MessageBox.Show(msg, "Amortization Calculator", MessageBoxButton.OK, MessageBoxImage.Information);
        vm.ScrollToRow = index =>
        {
            if (index >= 0 && index < vm.DisplayScheduleRows.Count)
                ScheduleGrid.ScrollIntoView(vm.DisplayScheduleRows[index]);
        };
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
