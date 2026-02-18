using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using Amortization.App.Services;
using Amortization.Core.Domain;
using Amortization.Core.Engine;
using Amortization.Core.Export;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amortization.App.ViewModels;

/// <summary>Controls which schedule is shown in the table (grid).</summary>
public enum TableScheduleMode { Base, WithExtras }

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
    [NotifyPropertyChangedFor(nameof(HasPropertyValue), nameof(ShowLtvColumnInTable))]
    private string _propertyValueText = "";

    [ObservableProperty]
    private string _propertyValueError = "";

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
    [NotifyPropertyChangedFor(nameof(ShowExtraColumns), nameof(ShowExtraPrincipalColumnInTable), nameof(ShowTotalPrincipalColumnInTable), nameof(IsBaseTableMode), nameof(IsExtrasTableMode), nameof(GridViewingLabel), nameof(GridViewingHint))]
    private TableScheduleMode _tableScheduleMode = TableScheduleMode.Base;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScheduleRowHeight))]
    private bool _isCompactDensity;

    public double ScheduleRowHeight => IsCompactDensity ? 26 : 36;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExtraPrincipalColumnInTable))]
    private bool _showExtraPrincipalColumn = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTotalPrincipalColumnInTable))]
    private bool _showTotalPrincipalColumn = true;

    [ObservableProperty]
    private bool _showBalanceColumn = true;

    [ObservableProperty]
    private bool _showCumulativeTotalPaidColumn;

    [ObservableProperty]
    private bool _showCumulativePrincipalColumn;

    [ObservableProperty]
    private bool _showPercentPaidOffColumn;

    [ObservableProperty]
    private bool _showInterestPercentColumn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLtvColumnInTable))]
    private bool _showLtvColumn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterestSavedPercent))]
    private decimal _interestSaved;

    [ObservableProperty]
    private int _paymentsSaved;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridViewingHint))]
    private bool _hasExtras;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastUpdatedText))]
    private DateTime? _lastUpdated;

    [ObservableProperty]
    private bool _autoRecalculate;

    private int? _scrollToPaymentNumber;

    public MainViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        LumpSums = new ObservableCollection<ExtraPaymentRowViewModel>();
        ScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        DisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        BaseDisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        ExtraDisplayScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        ActiveScheduleRows = new ObservableCollection<ScheduleRowViewModel>();
        BaseSummary = new ScheduleSummaryViewModel();
        ExtraSummary = new ScheduleSummaryViewModel();
        LumpSums.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ExtrasSummaryText));
            OnPropertyChanged(nameof(TotalLumpSumAmount));
        };
    }

    public Func<string?>? GetExportFilePath { get; set; }
    public Action<int>? ScrollToRow { get; set; }

    public ObservableCollection<ExtraPaymentRowViewModel> LumpSums { get; }
    public ObservableCollection<ScheduleRowViewModel> ScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> DisplayScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> BaseDisplayScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> ExtraDisplayScheduleRows { get; }
    public ObservableCollection<ScheduleRowViewModel> ActiveScheduleRows { get; }
    public ScheduleSummaryViewModel BaseSummary { get; }
    public ScheduleSummaryViewModel ExtraSummary { get; }

    public bool ShowExtraColumns => TableScheduleMode == TableScheduleMode.WithExtras;
    public bool ShowExtraPrincipalColumnInTable => ShowExtraColumns && ShowExtraPrincipalColumn;
    public bool ShowTotalPrincipalColumnInTable => ShowExtraColumns && ShowTotalPrincipalColumn;
    public bool HasPropertyValue => decimal.TryParse(PropertyValueText, out var v) && v > 0;
    public bool ShowLtvColumnInTable => ShowLtvColumn && HasPropertyValue;
    public bool IsBaseTableMode => TableScheduleMode == TableScheduleMode.Base;
    public bool IsExtrasTableMode => TableScheduleMode == TableScheduleMode.WithExtras;
    public string GridViewingLabel => TableScheduleMode == TableScheduleMode.Base ? "Viewing: Base schedule" : "Viewing: With extras schedule";
    public string GridViewingHint => HasExtras && TableScheduleMode == TableScheduleMode.Base ? "Extras configured but not applied to this table." : "";
    public string PayoffDateDelta => FormatPayoffDateDelta(BaseSummary.PayoffDate, ExtraSummary.PayoffDate);

    public string InterestSavedPercent
    {
        get
        {
            if (BaseSummary.TotalInterest <= 0 || InterestSaved <= 0) return "";
            var pct = InterestSaved / BaseSummary.TotalInterest * 100;
            return $"({pct:F1}%)";
        }
    }

    public string ExtrasSummaryText => BuildExtrasSummaryText();
    public decimal TotalLumpSumAmount => LumpSums.Where(x => decimal.TryParse(x.AmountText, out var a) && a > 0).Sum(x => decimal.Parse(x.AmountText));

    public DateTime? NewPayoffDate => HasExtras ? ExtraSummary.PayoffDate : (DateTime?)null;

    public string LastUpdatedText => LastUpdated.HasValue ? "Last updated: " + LastUpdated.Value.ToString("g") : "";

    public int? ScrollToPaymentNumber
    {
        get => _scrollToPaymentNumber;
        private set => SetProperty(ref _scrollToPaymentNumber, value);
    }

    /// <summary>Schedule result shown in the table (for legacy ScheduleRows/DisplayScheduleRows).</summary>
    private ScheduleResult? EffectiveTableResult =>
        TableScheduleMode == TableScheduleMode.WithExtras ? _extraResult : _baseResult;

    partial void OnPrincipalTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnRateTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnTermYearsTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnStartDateChanged(DateTime value) => ScheduleAutoRecalc();
    partial void OnRecurringExtraTextChanged(string value)
    {
        ScheduleAutoRecalc();
        OnPropertyChanged(nameof(ExtrasSummaryText));
    }
    partial void OnPropertyValueTextChanged(string value) => ScheduleAutoRecalc();
    partial void OnTableScheduleModeChanged(TableScheduleMode value)
    {
        SyncActiveScheduleRows();
    }
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
        OnPropertyChanged(nameof(PayoffDateDelta));
        OnPropertyChanged(nameof(InterestSavedPercent));

        if (!string.IsNullOrWhiteSpace(PropertyValueText))
        {
            if (!decimal.TryParse(PropertyValueText, out var pv) || pv <= 0)
                PropertyValueError = "Enter a valid positive amount.";
            else
                PropertyValueError = "";
        }
        else
            PropertyValueError = "";

        LastUpdated = DateTime.Now;
        RefreshScheduleDisplay(baseResult, extraResult);
        TableScheduleMode = HasExtras ? TableScheduleMode.WithExtras : TableScheduleMode.Base;
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
            var result = TableScheduleMode == TableScheduleMode.Base ? _baseResult : _extraResult;
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
        LumpSums.Add(CreateExtraPaymentRow(DateTime.Today, ""));
        ScheduleAutoRecalc();
    }

    private ExtraPaymentRowViewModel CreateExtraPaymentRow(DateTime date, string amountText)
    {
        var row = new ExtraPaymentRowViewModel { Date = date, AmountText = amountText };
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExtraPaymentRowViewModel.AmountText))
            {
                OnPropertyChanged(nameof(ExtrasSummaryText));
                OnPropertyChanged(nameof(TotalLumpSumAmount));
            }
        };
        return row;
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
        LumpSums.Add(CreateExtraPaymentRow(QuickAddDate, amount.ToString("F2")));
        QuickAddAmountText = "";
        ScheduleAutoRecalc();
    }

    [RelayCommand]
    private void ClearRecurring()
    {
        RecurringExtraText = "";
    }

    [RelayCommand]
    private void RemoveLumpSum(ExtraPaymentRowViewModel? item)
    {
        if (item != null && LumpSums.Remove(item))
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

    private IReadOnlyList<ScheduleRowViewModel> GetCurrentDisplayRowsForJump() => ActiveScheduleRows;

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
                decimal sumPaymentAndExtra = sumScheduledPayment + sumExtra;
                decimal interestPercentYearly = sumPaymentAndExtra > 0 ? sumInterest / sumPaymentAndExtra * 100 : 0;
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
                        last.CumulativeInterest,
                        last.CumulativeTotalPaid,
                        last.CumulativePrincipal,
                        last.PercentPaidOff,
                        interestPercentYearly));
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
        ApplyLtvToRows(BaseDisplayScheduleRows);
        ApplyLtvToRows(ExtraDisplayScheduleRows);
        SyncActiveScheduleRows();
    }

    private void ApplyLtvToRows(ObservableCollection<ScheduleRowViewModel> rows)
    {
        if (!HasPropertyValue || !decimal.TryParse(PropertyValueText, out var propertyValue) || propertyValue <= 0) return;
        foreach (var row in rows)
            row.LoanToValue = row.EndingBalance / propertyValue * 100;
    }

    private void SyncActiveScheduleRows()
    {
        var source = TableScheduleMode == TableScheduleMode.Base ? BaseDisplayScheduleRows : ExtraDisplayScheduleRows;
        ActiveScheduleRows.Clear();
        foreach (var row in source)
            ActiveScheduleRows.Add(row);
    }

    private static string FormatPayoffDateDelta(DateTime basePayoff, DateTime extraPayoff)
    {
        if (extraPayoff >= basePayoff) return "—";
        var span = basePayoff - extraPayoff;
        var years = span.Days / 365;
        var months = (span.Days % 365) / 30;
        if (years > 0 && months > 0) return $"{years} yr {months} mo sooner";
        if (years > 0) return $"{years} yr sooner";
        if (months > 0) return $"{months} mo sooner";
        return "Sooner";
    }

    private string BuildExtrasSummaryText()
    {
        var parts = new List<string>();
        if (decimal.TryParse(RecurringExtraText, out var rec) && rec > 0)
            parts.Add($"{rec:C0}/mo");
        var count = LumpSums.Count(x => decimal.TryParse(x.AmountText, out var a) && a > 0);
        if (count > 0)
            parts.Add(count == 1 ? "1 lump sum" : $"{count} lump sums");
        return parts.Count == 0 ? "None" : string.Join(" + ", parts);
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
                decimal sumPaymentAndExtra = sumScheduledPayment + sumExtra;
                decimal interestPercentYearly = sumPaymentAndExtra > 0 ? sumInterest / sumPaymentAndExtra * 100 : 0;
                target.Add(new ScheduleRowViewModel(new ScheduleRow(
                    last.PaymentNumber,
                    last.PaymentDate,
                    sumScheduledPayment,
                    sumInterest,
                    sumScheduledPrincipal,
                    sumExtra,
                    sumTotalPrincipal,
                    last.EndingBalance,
                    last.CumulativeInterest,
                    last.CumulativeTotalPaid,
                    last.CumulativePrincipal,
                    last.PercentPaidOff,
                    interestPercentYearly)));
            }
        }
        else
        {
            foreach (var row in result.Rows)
                target.Add(new ScheduleRowViewModel(row));
        }
    }
}
