namespace CodexUsageWidget.Models;

public sealed class CodexUsageData
{
    public int? FiveHourRemainingPercent { get; init; }
    public string? FiveHourResetText { get; init; }
    public int? WeeklyRemainingPercent { get; init; }
    public string? WeeklyResetText { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    public bool LoginRequired { get; init; }
    public string? ErrorMessage { get; init; }

    public bool HasUsage => FiveHourRemainingPercent.HasValue || WeeklyRemainingPercent.HasValue;
}
