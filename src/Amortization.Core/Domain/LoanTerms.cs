namespace Amortization.Core.Domain;

/// <summary>
/// Defines the core terms of a fixed-rate loan.
/// StartDate is the first payment date (payment #1).
/// </summary>
public sealed class LoanTerms
{
    public decimal Principal { get; }
    public decimal AnnualInterestRatePercent { get; }
    public int TermMonths { get; }
    public DateTime StartDate { get; }

    public LoanTerms(decimal principal, decimal annualInterestRatePercent, int termMonths, DateTime startDate)
    {
        if (principal < 0) throw new ArgumentOutOfRangeException(nameof(principal));
        if (annualInterestRatePercent < 0) throw new ArgumentOutOfRangeException(nameof(annualInterestRatePercent));
        if (termMonths <= 0) throw new ArgumentOutOfRangeException(nameof(termMonths));

        Principal = principal;
        AnnualInterestRatePercent = annualInterestRatePercent;
        TermMonths = termMonths;
        StartDate = startDate.Date;
    }
}
