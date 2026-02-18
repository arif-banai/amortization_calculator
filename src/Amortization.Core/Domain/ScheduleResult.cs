namespace Amortization.Core.Domain;

/// <summary>
/// Full result of schedule generation: rows plus summary.
/// </summary>
public sealed class ScheduleResult
{
    public IReadOnlyList<ScheduleRow> Rows { get; }
    public ScheduleSummary Summary { get; }

    public ScheduleResult(IReadOnlyList<ScheduleRow> rows, ScheduleSummary summary)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }
}
