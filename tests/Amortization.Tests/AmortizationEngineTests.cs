using Amortization.Core.Domain;
using Amortization.Core.Engine;
using Xunit;

namespace Amortization.Tests;

public class AmortizationEngineTests
{
    private static readonly LoanTerms StandardLoan = new(
        principal: 300_000m,
        annualInterestRatePercent: 6.5m,
        termMonths: 360,
        startDate: new DateTime(2026, 3, 1));

    private static readonly CalcOptions DefaultOptions = CalcOptions.Default;

    [Fact]
    public void MonthlyPayment_MatchesExpected_WithinOneCent()
    {
        // 300k @ 6.5% for 360 months: typical calculators ~1895.82–1896.20 depending on rounding
        decimal payment = AmortizationEngine.ComputeMonthlyPayment(
            300_000m,
            (6.5m / 100m) / 12m,
            360);
        Assert.True(payment >= 1895m && payment <= 1897m, $"Expected ~1896, got {payment}");
    }

    [Fact]
    public void BaseSchedule_LastRowEndingBalance_IsZero()
    {
        var engine = new AmortizationEngine();
        var result = engine.GenerateBaseSchedule(StandardLoan, DefaultOptions);
        Assert.NotEmpty(result.Rows);
        var last = result.Rows[^1];
        Assert.Equal(0.00m, last.EndingBalance);
    }

    [Fact]
    public void BaseSchedule_RowCount_DoesNotExceedTerm()
    {
        var engine = new AmortizationEngine();
        var result = engine.GenerateBaseSchedule(StandardLoan, DefaultOptions);
        Assert.True(result.Rows.Count <= 360, $"Expected <= 360 payments, got {result.Rows.Count}");
    }

    [Fact]
    public void BaseSchedule_Summary_ConsistentWithRows()
    {
        var engine = new AmortizationEngine();
        var result = engine.GenerateBaseSchedule(StandardLoan, DefaultOptions);
        Assert.Equal(result.Rows.Count, result.Summary.TotalPayments);
        var last = result.Rows[^1];
        Assert.Equal(last.CumulativeInterest, result.Summary.TotalInterest);
        Assert.Equal(last.PaymentDate, result.Summary.PayoffDate);
    }

    [Fact]
    public void WithRecurringExtra_PayoffEarlier_AndLessInterest()
    {
        var engine = new AmortizationEngine();
        var baseResult = engine.GenerateBaseSchedule(StandardLoan, DefaultOptions);
        var extras = new ExtraPaymentPlan(recurringExtraPrincipal: 200m);
        var extraResult = engine.GenerateSchedule(StandardLoan, extras, DefaultOptions);

        Assert.True(extraResult.Summary.TotalPayments < baseResult.Summary.TotalPayments,
            "With $200/mo extra, payoff should be in fewer than 360 payments.");
        Assert.True(extraResult.Summary.TotalInterest < baseResult.Summary.TotalInterest,
            "With extra payments, total interest should be lower than base.");
        Assert.Equal(0.00m, extraResult.Rows[^1].EndingBalance);
    }

    [Fact]
    public void WithRecurringExtra_LastRowEndingBalance_IsZero()
    {
        var engine = new AmortizationEngine();
        var extras = new ExtraPaymentPlan(recurringExtraPrincipal: 200m);
        var result = engine.GenerateSchedule(StandardLoan, extras, DefaultOptions);
        Assert.Equal(0.00m, result.Rows[^1].EndingBalance);
    }

    [Fact]
    public void LumpSumOnDate_AppearsAsExtraPrincipal_OnThatPayment()
    {
        var start = new DateTime(2026, 3, 1);
        var loan = new LoanTerms(300_000m, 6.5m, 360, start);
        var lumpDate = start.AddMonths(5); // payment #6
        var extras = new ExtraPaymentPlan(0m, new[] { new ExtraPayment(lumpDate, 10_000m) });
        var engine = new AmortizationEngine();
        var result = engine.GenerateSchedule(loan, extras, DefaultOptions);

        var row6 = result.Rows.FirstOrDefault(r => r.PaymentNumber == 6);
        Assert.NotNull(row6);
        Assert.Equal(10_000m, row6.ExtraPrincipal);
        Assert.Equal(0.00m, result.Rows[^1].EndingBalance);
    }
}
