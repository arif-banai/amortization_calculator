using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Amortization.Core.Domain;
using Amortization.Core.Engine;
using Amortization.Core.Export;

namespace Amortization.App.ViewModels;

public enum SelectedScenario { Base, WithExtras }

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AmortizationEngine _engine = new();
    private string _principalText = "300000";
    private string _rateText = "6.5";
    private string _termYearsText = "30";
    private DateTime _startDate = DateTime.Today;
    private string _recurringExtraText = "";
    private SelectedScenario _selectedScenario = SelectedScenario.Base;
    private decimal _interestSaved;
    private int _paymentsSaved;
    private int _selectedLumpSumIndex = -1;

    public MainViewModel()
    {
        LumpSums = new ObservableCollection<ExtraPaymentRowViewModel>();
        ScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        BaseSummary = new ScheduleSummaryViewModel();
        ExtraSummary = new ScheduleSummaryViewModel();

        CalculateCommand = new RelayCommand(Calculate);
        ExportCsvCommand = new RelayCommand(ExportCsv);
        AddLumpSumCommand = new RelayCommand(AddLumpSum);
        RemoveSelectedLumpSumCommand = new RelayCommand(RemoveSelectedLumpSum, () => SelectedLumpSumIndex >= 0);
    }

    public Func<string?>? GetExportFilePath { get; set; }
    public Action<string>? ShowMessage { get; set; }

    #region Inputs
    public string PrincipalText { get => _principalText; set { _principalText = value ?? ""; OnPropertyChanged(nameof(PrincipalText)); } }
    public string RateText { get => _rateText; set { _rateText = value ?? ""; OnPropertyChanged(nameof(RateText)); } }
    public string TermYearsText { get => _termYearsText; set { _termYearsText = value ?? ""; OnPropertyChanged(nameof(TermYearsText)); } }
    public DateTime StartDate { get => _startDate; set { _startDate = value; OnPropertyChanged(nameof(StartDate)); } }
    public string RecurringExtraText { get => _recurringExtraText; set { _recurringExtraText = value ?? ""; OnPropertyChanged(nameof(RecurringExtraText)); } }
    #endregion

    public ObservableCollection<ExtraPaymentRowViewModel> LumpSums { get; }
    public int SelectedLumpSumIndex
    {
        get => _selectedLumpSumIndex;
        set { _selectedLumpSumIndex = value; OnPropertyChanged(nameof(SelectedLumpSumIndex)); CommandManager.InvalidateRequerySuggested(); }
    }

    public ObservableCollection<ScheduleRowViewModel> ScheduleRows { get; }
    public ScheduleSummaryViewModel BaseSummary { get; }
    public ScheduleSummaryViewModel ExtraSummary { get; }
    public decimal InterestSaved { get => _interestSaved; private set { _interestSaved = value; OnPropertyChanged(nameof(InterestSaved)); } }
    public int PaymentsSaved { get => _paymentsSaved; private set { _paymentsSaved = value; OnPropertyChanged(nameof(PaymentsSaved)); } }

    public SelectedScenario SelectedScenario
    {
        get => _selectedScenario;
        set { _selectedScenario = value; OnPropertyChanged(nameof(SelectedScenario)); RefreshScheduleDisplay(); }
    }

    public ICommand CalculateCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand AddLumpSumCommand { get; }
    public ICommand RemoveSelectedLumpSumCommand { get; }

    private ScheduleResult? _baseResult;
    private ScheduleResult? _extraResult;

    private void Calculate()
    {
        if (!TryParseInputs(out var principal, out var rate, out var termYears, out var recurringExtra))
            return;

        int termMonths = termYears * 12;
        var terms = new LoanTerms(principal, rate, termMonths, StartDate);
        var lumpSums = LumpSums
            .Where(x => decimal.TryParse(x.AmountText, out var a) && a > 0)
            .Select(x => new ExtraPayment(x.Date, decimal.Parse(x.AmountText)))
            .ToList();
        var extras = new ExtraPaymentPlan(recurringExtra, lumpSums);
        var options = CalcOptions.Default;

        _baseResult = _engine.GenerateBaseSchedule(terms, options);
        _extraResult = _engine.GenerateSchedule(terms, extras, options);

        BaseSummary.SetFrom(_baseResult.Summary);
        ExtraSummary.SetFrom(_extraResult.Summary);
        InterestSaved = _baseResult.Summary.TotalInterest - _extraResult.Summary.TotalInterest;
        PaymentsSaved = _baseResult.Summary.TotalPayments - _extraResult.Summary.TotalPayments;
        RefreshScheduleDisplay();
    }

    private void RefreshScheduleDisplay()
    {
        ScheduleRows.Clear();
        var result = SelectedScenario == SelectedScenario.WithExtras ? _extraResult : _baseResult;
        if (result != null)
            foreach (var row in result.Rows)
                ScheduleRows.Add(new ScheduleRowViewModel(row));
    }

    private bool TryParseInputs(out decimal principal, out decimal rate, out int termYears, out decimal recurringExtra)
    {
        principal = 0; rate = 0; termYears = 0; recurringExtra = 0;
        if (!decimal.TryParse(PrincipalText, out principal) || principal < 0)
        {
            ShowMessage?.Invoke("Please enter a valid non-negative Principal.");
            return false;
        }
        if (!decimal.TryParse(RateText, out rate) || rate < 0)
        {
            ShowMessage?.Invoke("Please enter a valid non-negative Interest Rate.");
            return false;
        }
        if (!int.TryParse(TermYearsText, out termYears) || termYears <= 0)
        {
            ShowMessage?.Invoke("Please enter a valid Term in years (positive number).");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(RecurringExtraText) && (!decimal.TryParse(RecurringExtraText, out recurringExtra) || recurringExtra < 0))
        {
            ShowMessage?.Invoke("Please enter a valid non-negative Recurring Extra amount.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(RecurringExtraText)) recurringExtra = 0;
        return true;
    }

    private void ExportCsv()
    {
        var path = GetExportFilePath?.Invoke();
        if (string.IsNullOrEmpty(path)) return;
        var result = SelectedScenario == SelectedScenario.WithExtras ? _extraResult : _baseResult;
        if (result == null || result.Rows.Count == 0)
        {
            ShowMessage?.Invoke("No schedule to export. Run Calculate first.");
            return;
        }
        try
        {
            File.WriteAllText(path, CsvExporter.ExportSchedule(result.Rows));
            ShowMessage?.Invoke("Schedule exported successfully.");
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke($"Export failed: {ex.Message}");
        }
    }

    private void AddLumpSum()
    {
        LumpSums.Add(new ExtraPaymentRowViewModel { Date = DateTime.Today, AmountText = "" });
    }

    private void RemoveSelectedLumpSum()
    {
        if (SelectedLumpSumIndex >= 0 && SelectedLumpSumIndex < LumpSums.Count)
        {
            LumpSums.RemoveAt(SelectedLumpSumIndex);
            SelectedLumpSumIndex = -1;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
