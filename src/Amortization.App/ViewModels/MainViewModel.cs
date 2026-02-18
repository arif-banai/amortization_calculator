using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Amortization.Core.Domain;
using Amortization.Core.Engine;
using Amortization.Core.Export;

namespace Amortization.App.ViewModels;

public enum SelectedScenario { Base, WithExtras }

public enum ExportScope { Current, Base, WithExtras }

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AmortizationEngine _engine = new();
    private DispatcherTimer? _autoRecalcTimer;
    private DispatcherTimer? _exportStatusClearTimer;
    private string _principalText = "300000";
    private string _rateText = "6.5";
    private string _termYearsText = "30";
    private DateTime _startDate = DateTime.Today;
    private string _recurringExtraText = "";
    private SelectedScenario _selectedScenario = SelectedScenario.Base;
    private decimal _interestSaved;
    private int _paymentsSaved;
    private int _selectedLumpSumIndex = -1;
    private bool _hasExtras;
    private DateTime? _lastUpdated;
    private bool _autoRecalculate;
    private string _exportStatusMessage = "";
    private DateTime _quickAddDate = DateTime.Today;
    private string _quickAddAmountText = "";
    private bool _isYearlyView;
    private string _jumpToPaymentText = "";
    private int? _scrollToPaymentNumber;
    private bool _showCumulativeInterestColumn = true;
    private string _principalError = "";
    private string _rateError = "";
    private string _termYearsError = "";
    private string _recurringExtraError = "";

    public MainViewModel()
    {
        LumpSums = new ObservableCollection<ExtraPaymentRowViewModel>();
        ScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        DisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        BaseSummary = new ScheduleSummaryViewModel();
        ExtraSummary = new ScheduleSummaryViewModel();

        CalculateCommand = new RelayCommand(Calculate);
        ExportCsvCommand = new RelayCommand<ExportScope>(s => ExportCsv(s));
        AddLumpSumCommand = new RelayCommand(AddLumpSum);
        RemoveSelectedLumpSumCommand = new RelayCommand(RemoveSelectedLumpSum, () => SelectedLumpSumIndex >= 0);
        AddQuickLumpSumCommand = new RelayCommand(AddQuickLumpSum);
        AddRecurringPresetCommand = new RelayCommand<object>(o =>
        {
            if (o is decimal d) AddRecurringPreset(d);
            else if (o is int i) AddRecurringPreset(i);
            else if (o is string s && decimal.TryParse(s, out var parsed)) AddRecurringPreset(parsed);
        });
        JumpToPaymentCommand = new RelayCommand(JumpToPayment);
    }

    public Func<string?>? GetExportFilePath { get; set; }
    public Action<string>? ShowMessage { get; set; }
    public Action<int>? ScrollToRow { get; set; }

    #region Inputs
    public string PrincipalText
    {
        get => _principalText;
        set { _principalText = value ?? ""; OnPropertyChanged(nameof(PrincipalText)); ScheduleAutoRecalc(); }
    }
    public string RateText
    {
        get => _rateText;
        set { _rateText = value ?? ""; OnPropertyChanged(nameof(RateText)); ScheduleAutoRecalc(); }
    }
    public string TermYearsText
    {
        get => _termYearsText;
        set { _termYearsText = value ?? ""; OnPropertyChanged(nameof(TermYearsText)); ScheduleAutoRecalc(); }
    }
    public DateTime StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(nameof(StartDate)); ScheduleAutoRecalc(); }
    }
    public string RecurringExtraText
    {
        get => _recurringExtraText;
        set { _recurringExtraText = value ?? ""; OnPropertyChanged(nameof(RecurringExtraText)); ScheduleAutoRecalc(); }
    }
    #endregion

    #region Validation errors
    public string PrincipalError { get => _principalError; private set { _principalError = value; OnPropertyChanged(nameof(PrincipalError)); } }
    public string RateError { get => _rateError; private set { _rateError = value; OnPropertyChanged(nameof(RateError)); } }
    public string TermYearsError { get => _termYearsError; private set { _termYearsError = value; OnPropertyChanged(nameof(TermYearsError)); } }
    public string RecurringExtraError { get => _recurringExtraError; private set { _recurringExtraError = value; OnPropertyChanged(nameof(RecurringExtraError)); } }
    #endregion

    public ObservableCollection<ExtraPaymentRowViewModel> LumpSums { get; }
    public int SelectedLumpSumIndex
    {
        get => _selectedLumpSumIndex;
        set { _selectedLumpSumIndex = value; OnPropertyChanged(nameof(SelectedLumpSumIndex)); CommandManager.InvalidateRequerySuggested(); }
    }

    public DateTime QuickAddDate { get => _quickAddDate; set { _quickAddDate = value; OnPropertyChanged(nameof(QuickAddDate)); } }
    public string QuickAddAmountText { get => _quickAddAmountText; set { _quickAddAmountText = value ?? ""; OnPropertyChanged(nameof(QuickAddAmountText)); } }

    public ObservableCollection<ScheduleRowViewModel> ScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> DisplayScheduleRows { get; }
    public ScheduleSummaryViewModel BaseSummary { get; }
    public ScheduleSummaryViewModel ExtraSummary { get; }
    public decimal InterestSaved { get => _interestSaved; private set { _interestSaved = value; OnPropertyChanged(nameof(InterestSaved)); } }
    public int PaymentsSaved { get => _paymentsSaved; private set { _paymentsSaved = value; OnPropertyChanged(nameof(PaymentsSaved)); } }
    public bool HasExtras { get => _hasExtras; private set { _hasExtras = value; OnPropertyChanged(nameof(HasExtras)); } }
    public DateTime? NewPayoffDate => HasExtras ? ExtraSummary.PayoffDate : (DateTime?)null;

    public DateTime? LastUpdated { get => _lastUpdated; private set { _lastUpdated = value; OnPropertyChanged(nameof(LastUpdated)); OnPropertyChanged(nameof(LastUpdatedText)); } }
    public string LastUpdatedText => _lastUpdated.HasValue ? "Last updated: " + _lastUpdated.Value.ToString("g") : "";

    public bool AutoRecalculate
    {
        get => _autoRecalculate;
        set { _autoRecalculate = value; OnPropertyChanged(nameof(AutoRecalculate)); if (!value) _autoRecalcTimer?.Stop(); }
    }

    public string ExportStatusMessage { get => _exportStatusMessage; private set { _exportStatusMessage = value ?? ""; OnPropertyChanged(nameof(ExportStatusMessage)); } }

    public bool IsYearlyView
    {
        get => _isYearlyView;
        set { _isYearlyView = value; OnPropertyChanged(nameof(IsYearlyView)); UpdateDisplayScheduleRows(); }
    }

    public string JumpToPaymentText { get => _jumpToPaymentText; set { _jumpToPaymentText = value ?? ""; OnPropertyChanged(nameof(JumpToPaymentText)); } }

    public int? ScrollToPaymentNumber
    {
        get => _scrollToPaymentNumber;
        private set { _scrollToPaymentNumber = value; OnPropertyChanged(nameof(ScrollToPaymentNumber)); }
    }

    public bool ShowCumulativeInterestColumn
    {
        get => _showCumulativeInterestColumn;
        set { _showCumulativeInterestColumn = value; OnPropertyChanged(nameof(ShowCumulativeInterestColumn)); }
    }

    public SelectedScenario SelectedScenario
    {
        get => _selectedScenario;
        set { _selectedScenario = value; OnPropertyChanged(nameof(SelectedScenario)); RefreshScheduleDisplay(); }
    }

    public ICommand CalculateCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand AddLumpSumCommand { get; }
    public ICommand RemoveSelectedLumpSumCommand { get; }
    public ICommand AddQuickLumpSumCommand { get; }
    public ICommand AddRecurringPresetCommand { get; }
    public ICommand JumpToPaymentCommand { get; }

    private ScheduleResult? _baseResult;
    private ScheduleResult? _extraResult;

    private void ScheduleAutoRecalc()
    {
        if (!AutoRecalculate) return;
        _autoRecalcTimer ??= new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _autoRecalcTimer.Stop();
        _autoRecalcTimer.Tick -= AutoRecalcTimer_Tick;
        _autoRecalcTimer.Tick += AutoRecalcTimer_Tick;
        _autoRecalcTimer.Start();
    }

    private void AutoRecalcTimer_Tick(object? sender, EventArgs e)
    {
        _autoRecalcTimer?.Stop();
        Calculate();
    }

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
        HasExtras = recurringExtra > 0 || lumpSums.Count > 0;

        var options = CalcOptions.Default;
        _baseResult = _engine.GenerateBaseSchedule(terms, options);
        _extraResult = _engine.GenerateSchedule(terms, extras, options);

        BaseSummary.SetFrom(_baseResult.Summary);
        ExtraSummary.SetFrom(_extraResult.Summary);
        InterestSaved = _baseResult.Summary.TotalInterest - _extraResult.Summary.TotalInterest;
        PaymentsSaved = _baseResult.Summary.TotalPayments - _extraResult.Summary.TotalPayments;
        OnPropertyChanged(nameof(NewPayoffDate));

        LastUpdated = DateTime.Now;
        RefreshScheduleDisplay();
    }

    private void RefreshScheduleDisplay()
    {
        ScheduleRows.Clear();
        var result = SelectedScenario == SelectedScenario.WithExtras ? _extraResult : _baseResult;
        if (result != null)
            foreach (var row in result.Rows)
                ScheduleRows.Add(new ScheduleRowViewModel(row));
        UpdateDisplayScheduleRows();
    }

    private void UpdateDisplayScheduleRows()
    {
        DisplayScheduleRows.Clear();
        if (IsYearlyView && ScheduleRows.Count > 0)
        {
            int year = 1;
            for (int i = 0; i < ScheduleRows.Count; i += 12)
            {
                var chunk = ScheduleRows.Skip(i).Take(12).ToList();
                if (chunk.Count == 0) break;
                var last = chunk[^1];
                decimal sumInterest = chunk.Sum(r => r.Interest);
                decimal sumScheduledPayment = chunk.Sum(r => r.ScheduledPayment);
                decimal sumScheduledPrincipal = chunk.Sum(r => r.ScheduledPrincipal);
                decimal sumExtra = chunk.Sum(r => r.ExtraPrincipal);
                decimal sumTotalPrincipal = chunk.Sum(r => r.TotalPrincipal);
                var yearlyRow = new ScheduleRowViewModel(
                    new ScheduleRow(
                        last.PaymentNumber,
                        last.PaymentDate,
                        sumScheduledPayment,
                        sumInterest,
                        sumScheduledPrincipal,
                        sumExtra,
                        sumTotalPrincipal,
                        last.EndingBalance,
                        last.CumulativeInterest));
                DisplayScheduleRows.Add(yearlyRow);
                year++;
            }
        }
        else
        {
            foreach (var row in ScheduleRows)
                DisplayScheduleRows.Add(row);
        }
    }

    private bool TryParseInputs(out decimal principal, out decimal rate, out int termYears, out decimal recurringExtra)
    {
        principal = 0; rate = 0; termYears = 0; recurringExtra = 0;
        PrincipalError = ""; RateError = ""; TermYearsError = ""; RecurringExtraError = "";

        if (!decimal.TryParse(PrincipalText, out principal) || principal < 0)
        {
            PrincipalError = "Enter a valid non-negative amount.";
            return false;
        }
        if (!decimal.TryParse(RateText, out rate) || rate < 0)
        {
            RateError = "Enter a valid non-negative rate.";
            return false;
        }
        if (!int.TryParse(TermYearsText, out termYears) || termYears <= 0)
        {
            TermYearsError = "Enter a positive number of years.";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(RecurringExtraText) && (!decimal.TryParse(RecurringExtraText, out recurringExtra) || recurringExtra < 0))
        {
            RecurringExtraError = "Enter a valid non-negative amount.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(RecurringExtraText)) recurringExtra = 0;
        return true;
    }

    private void ExportCsv(ExportScope scope)
    {
        var s = scope;
        var path = GetExportFilePath?.Invoke();
        if (string.IsNullOrEmpty(path)) return;

        IEnumerable<ScheduleRow> rowsToExport;
        string scopeLabel;
        if (s == ExportScope.Base && _baseResult != null)
        {
            rowsToExport = _baseResult.Rows;
            scopeLabel = "base schedule";
        }
        else if (s == ExportScope.WithExtras && _extraResult != null)
        {
            rowsToExport = _extraResult.Rows;
            scopeLabel = "with extras";
        }
        else
        {
            var result = SelectedScenario == SelectedScenario.WithExtras ? _extraResult : _baseResult;
            if (result == null || result.Rows.Count == 0)
            {
                ShowMessage?.Invoke("No schedule to export. Run Calculate first.");
                return;
            }
            rowsToExport = result.Rows;
            scopeLabel = "current view";
        }

        if (!rowsToExport.Any())
        {
            ShowMessage?.Invoke("No schedule to export. Run Calculate first.");
            return;
        }

        try
        {
            File.WriteAllText(path, CsvExporter.ExportSchedule(rowsToExport));
            ExportStatusMessage = $"Exported {scopeLabel} to {path}";
            _exportStatusClearTimer?.Stop();
            _exportStatusClearTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _exportStatusClearTimer.Tick += (_, _) => { ExportStatusMessage = ""; _exportStatusClearTimer?.Stop(); };
            _exportStatusClearTimer.Start();
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke($"Export failed: {ex.Message}");
        }
    }

    private void AddLumpSum()
    {
        LumpSums.Add(new ExtraPaymentRowViewModel { Date = DateTime.Today, AmountText = "" });
        ScheduleAutoRecalc();
    }

    private void AddQuickLumpSum()
    {
        if (!decimal.TryParse(QuickAddAmountText, out var amount) || amount <= 0) return;
        LumpSums.Add(new ExtraPaymentRowViewModel { Date = QuickAddDate, AmountText = amount.ToString("F2") });
        QuickAddAmountText = "";
        ScheduleAutoRecalc();
    }

    private void AddRecurringPreset(decimal amount)
    {
        if (amount <= 0) return;
        decimal current = 0;
        decimal.TryParse(RecurringExtraText, out current);
        RecurringExtraText = (current + amount).ToString("F2");
    }

    private void RemoveSelectedLumpSum()
    {
        if (SelectedLumpSumIndex >= 0 && SelectedLumpSumIndex < LumpSums.Count)
        {
            LumpSums.RemoveAt(SelectedLumpSumIndex);
            SelectedLumpSumIndex = -1;
            ScheduleAutoRecalc();
        }
    }

    private void JumpToPayment()
    {
        if (string.IsNullOrWhiteSpace(JumpToPaymentText)) return;
        var source = IsYearlyView ? DisplayScheduleRows : ScheduleRows;
        if (source.Count == 0) return;
        int num;
        if (int.TryParse(JumpToPaymentText, out num))
        {
            if (num >= 1 && num <= source.Count)
            {
                ScrollToPaymentNumber = num;
                ScrollToRow?.Invoke(num - 1);
                ScrollToPaymentNumber = null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
