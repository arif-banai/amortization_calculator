using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Amortization.App.Services;
using Amortization.Core.Domain;
using Amortization.Core.Engine;
using Amortization.Core.Export;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amortization.App.ViewModels;

public enum SelectedScenario { Base, WithExtras }

public enum ExportScope { Current, Base, WithExtras }

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AmortizationEngine _engine = new();
    private readonly INotificationService _notificationService;
    private DispatcherTimer? _autoRecalcTimer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedLumpSumCommand))]
    private int _selectedLumpSumIndex = -1;

    [ObservableProperty]
    private string _principalText = "300000";

    [ObservableProperty]
    private string _rateText = "6.5";

    [ObservableProperty]
    private string _termYearsText = "30";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private string _recurringExtraText = "";

    [ObservableProperty]
    private string _principalError = "";

    [ObservableProperty]
    private string _rateError = "";

    [ObservableProperty]
    private string _termYearsError = "";

    [ObservableProperty]
    private string _recurringExtraError = "";

    [ObservableProperty]
    private DateTime _quickAddDate = DateTime.Today;

    [ObservableProperty]
    private string _quickAddAmountText = "";

    [ObservableProperty]
    private bool _isYearlyView;

    [ObservableProperty]
    private string _jumpToPaymentText = "";

    [ObservableProperty]
    private bool _showCumulativeInterestColumn = true;

    [ObservableProperty]
    private SelectedScenario _selectedScenario = SelectedScenario.Base;

    [ObservableProperty]
    private decimal _interestSaved;

    [ObservableProperty]
    private int _paymentsSaved;

    [ObservableProperty]
    private bool _hasExtras;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastUpdatedText))]
    private DateTime? _lastUpdated;

    [ObservableProperty]
    private bool _autoRecalculate;

    /// <summary>When true and extras exist, table shows base schedule instead of with-extras. Only visible in UI when HasExtras.</summary>
    [ObservableProperty]
    private bool _showBaseInTable;

    /// <summary>Selected tab index when HasExtras: 0 = Base schedule, 1 = With extras. Default 1.</summary>
    [ObservableProperty]
    private int _selectedScheduleTabIndex = 1;

    private int? _scrollToPaymentNumber;

    public MainViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        LumpSums = new ObservableCollection<ExtraPaymentRowViewModel>();
        ScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        DisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        BaseDisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        ExtraDisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        BaseSummary = new ScheduleSummaryViewModel();
        ExtraSummary = new ScheduleSummaryViewModel();
    }

    public Func<string?>? GetExportFilePath { get; set; }
    public Action<int>? ScrollToRow { get; set; }

    public ObservableCollection<ExtraPaymentRowViewModel> LumpSums { get; }
    public ObservableCollection<ScheduleRowViewModel> ScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> DisplayScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> BaseDisplayScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> ExtraDisplayScheduleRows { get; }
    public ScheduleSummaryViewModel BaseSummary { get; }
    public ScheduleSummaryViewModel ExtraSummary { get; }

    public DateTime? NewPayoffDate => HasExtras ? ExtraSummary.PayoffDate : (DateTime?)null;

    public string LastUpdatedText => LastUpdated.HasValue ? "Last updated: " + LastUpdated.Value.ToString("g") : "";

    public int? ScrollToPaymentNumber
    {
        get => _scrollToPaymentNumber;
        private set => SetProperty(ref _scrollToPaymentNumber, value);
    }

    /// <summary>Schedule result shown in the table: with-extras when HasExtras and not ShowBaseInTable, else base.</summary>
    private ScheduleResult? EffectiveTableResult =>
        HasExtras && !ShowBaseInTable ? _extraResult : _baseResult;

    partial void OnPrincipalTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnRateTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnTermYearsTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnStartDateChanged(DateTime value) => ScheduleAutoRecalc();
    partial void OnRecurringExtraTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnSelectedScenarioChanged(SelectedScenario value) => RefreshScheduleDisplay();
    partial void OnShowBaseInTableChanged(bool value) => RefreshScheduleDisplay();
    partial void OnIsYearlyViewChanged(bool value) => UpdateDisplayScheduleRows();
    partial void OnAutoRecalculateChanged(bool value)
    {
        if (!value) _autoRecalcTimer?.Stop();
    }

    [RelayCommand]
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
        var baseResult = _engine.GenerateBaseSchedule(terms, options);
        var extraResult = _engine.GenerateSchedule(terms, extras, options);

        BaseSummary.SetFrom(baseResult.Summary);
        ExtraSummary.SetFrom(extraResult.Summary);
        InterestSaved = baseResult.Summary.TotalInterest - extraResult.Summary.TotalInterest;
        PaymentsSaved = baseResult.Summary.TotalPayments - extraResult.Summary.TotalPayments;
        OnPropertyChanged(nameof(NewPayoffDate));

        LastUpdated = DateTime.Now;
        SelectedScenario = HasExtras ? SelectedScenario.WithExtras : SelectedScenario.Base;
        RefreshScheduleDisplay(baseResult, extraResult);
    }

    [RelayCommand]
    private void ExportCsv(ExportScope scope)
    {
        var path = GetExportFilePath?.Invoke();
        if (string.IsNullOrEmpty(path)) return;

        IEnumerable<ScheduleRow> rowsToExport;
        string scopeLabel;
        if (scope == ExportScope.Base && _baseResult != null)
        {
            rowsToExport = _baseResult.Rows;
            scopeLabel = "base schedule";
        }
        else if (scope == ExportScope.WithExtras && _extraResult != null)
        {
            rowsToExport = _extraResult.Rows;
            scopeLabel = "with extras";
        }
        else
        {
            var result = HasExtras
                ? (SelectedScheduleTabIndex == 0 ? _baseResult : _extraResult)
                : _baseResult;
            if (result == null || result.Rows.Count == 0)
            {
                _notificationService.ShowError("Export", "No schedule to export. Run Calculate first.");
                return;
            }
            rowsToExport = result.Rows;
            scopeLabel = "current view";
        }

        if (!rowsToExport.Any())
        {
            _notificationService.ShowError("Export", "No schedule to export. Run Calculate first.");
            return;
        }

        try
        {
            File.WriteAllText(path, CsvExporter.ExportSchedule(rowsToExport));
            _notificationService.ShowSuccess("CSV exported", $"Exported {scopeLabel} to {path}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Export", $"Export failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddLumpSum()
    {
        LumpSums.Add(new ExtraPaymentRowViewModel { Date = DateTime.Today, AmountText = "" });
        ScheduleAutoRecalc();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveLumpSum))]
    private void RemoveSelectedLumpSum()
    {
        if (SelectedLumpSumIndex >= 0 && SelectedLumpSumIndex < LumpSums.Count)
        {
            LumpSums.RemoveAt(SelectedLumpSumIndex);
            SelectedLumpSumIndex = -1;
            ScheduleAutoRecalc();
        }
    }

    private bool CanRemoveLumpSum() => SelectedLumpSumIndex >= 0;

    [RelayCommand]
    private void AddQuickLumpSum()
    {
        if (!decimal.TryParse(QuickAddAmountText, out var amount) || amount <= 0) return;
        LumpSums.Add(new ExtraPaymentRowViewModel { Date = QuickAddDate, AmountText = amount.ToString("F2") });
        QuickAddAmountText = "";
        ScheduleAutoRecalc();
    }

    [RelayCommand]
    private void AddRecurringPreset(object? parameter)
    {
        decimal amount = 0;
        if (parameter is decimal d) amount = d;
        else if (parameter is int i) amount = i;
        else if (parameter is string s && decimal.TryParse(s, out var parsed)) amount = parsed;
        if (amount <= 0) return;
        decimal current = 0;
        decimal.TryParse(RecurringExtraText, out current);
        RecurringExtraText = (current + amount).ToString("F2");
    }

    [RelayCommand]
    private void JumpToPayment()
    {
        if (string.IsNullOrWhiteSpace(JumpToPaymentText)) return;
        var source = GetCurrentDisplayRowsForJump();
        if (source.Count == 0) return;
        if (int.TryParse(JumpToPaymentText, out int num) && num >= 1 && num <= source.Count)
        {
            ScrollToPaymentNumber = num;
            ScrollToRow?.Invoke(num - 1);
            ScrollToPaymentNumber = null;
        }
    }

    private IReadOnlyList<ScheduleRowViewModel> GetCurrentDisplayRowsForJump()
    {
        if (HasExtras)
        {
            var rows = SelectedScheduleTabIndex == 0 ? BaseDisplayScheduleRows : ExtraDisplayScheduleRows;
            return rows;
        }
        return BaseDisplayScheduleRows;
    }

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

    private void RefreshScheduleDisplay(ScheduleResult? baseResult, ScheduleResult? extraResult)
    {
        _baseResult = baseResult;
        _extraResult = extraResult;
        ScheduleRows.Clear();
        var result = EffectiveTableResult;
        if (result != null)
            foreach (var row in result.Rows)
                ScheduleRows.Add(new ScheduleRowViewModel(row));
        UpdateDisplayScheduleRows();
    }

    private void RefreshScheduleDisplay()
    {
        RefreshScheduleDisplay(_baseResult, _extraResult);
    }

    private void UpdateDisplayScheduleRows()
    {
        DisplayScheduleRows.Clear();
        if (IsYearlyView && ScheduleRows.Count > 0)
        {
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
            }
        }
        else
        {
            foreach (var row in ScheduleRows)
                DisplayScheduleRows.Add(row);
        }
        FillDisplayRowsFromResult(BaseDisplayScheduleRows, _baseResult);
        FillDisplayRowsFromResult(ExtraDisplayScheduleRows, _extraResult);
    }

    private void FillDisplayRowsFromResult(ObservableCollection<ScheduleRowViewModel> target, ScheduleResult? result)
    {
        target.Clear();
        if (result == null || result.Rows.Count == 0) return;
        if (IsYearlyView)
        {
            var rows = result.Rows;
            for (int i = 0; i < rows.Count; i += 12)
            {
                var chunk = rows.Skip(i).Take(12).ToList();
                if (chunk.Count == 0) break;
                var last = chunk[^1];
                decimal sumInterest = chunk.Sum(r => r.Interest);
                decimal sumScheduledPayment = chunk.Sum(r => r.ScheduledPayment);
                decimal sumScheduledPrincipal = chunk.Sum(r => r.ScheduledPrincipal);
                decimal sumExtra = chunk.Sum(r => r.ExtraPrincipal);
                decimal sumTotalPrincipal = chunk.Sum(r => r.TotalPrincipal);
                target.Add(new ScheduleRowViewModel(new ScheduleRow(
                    last.PaymentNumber,
                    last.PaymentDate,
                    sumScheduledPayment,
                    sumInterest,
                    sumScheduledPrincipal,
                    sumExtra,
                    sumTotalPrincipal,
                    last.EndingBalance,
                    last.CumulativeInterest)));
            }
        }
        else
        {
            foreach (var row in result.Rows)
                target.Add(new ScheduleRowViewModel(row));
        }
    }
}
