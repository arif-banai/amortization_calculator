using System.Globalization;
using System.Text;
using Amortization.Core.Domain;

namespace Amortization.Core.Export;

/// <summary>
/// Exports schedule rows to CSV string. No file I/O; caller writes the string to disk.
/// </summary>
public static class CsvExporter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string ExportSchedule(IEnumerable<ScheduleRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PaymentNumber,PaymentDate,ScheduledPayment,Interest,ScheduledPrincipal,ExtraPrincipal,TotalPrincipal,EndingBalance,CumulativeInterest,CumulativeTotalPaid,CumulativePrincipal,PercentPaidOff,InterestPercentOfPayment");
        foreach (var row in rows)
        {
            sb.Append(row.PaymentNumber).Append(',')
                .Append(row.PaymentDate.ToString("yyyy-MM-dd", Invariant)).Append(',')
                .Append(row.ScheduledPayment.ToString("F2", Invariant)).Append(',')
                .Append(row.Interest.ToString("F2", Invariant)).Append(',')
                .Append(row.ScheduledPrincipal.ToString("F2", Invariant)).Append(',')
                .Append(row.ExtraPrincipal.ToString("F2", Invariant)).Append(',')
                .Append(row.TotalPrincipal.ToString("F2", Invariant)).Append(',')
                .Append(row.EndingBalance.ToString("F2", Invariant)).Append(',')
                .Append(row.CumulativeInterest.ToString("F2", Invariant)).Append(',')
                .Append(row.CumulativeTotalPaid.ToString("F2", Invariant)).Append(',')
                .Append(row.CumulativePrincipal.ToString("F2", Invariant)).Append(',')
                .Append(row.PercentPaidOff.ToString("F2", Invariant)).Append(',')
                .Append(row.InterestPercentOfPayment.ToString("F2", Invariant)).AppendLine();
        }
        return sb.ToString();
    }
}
