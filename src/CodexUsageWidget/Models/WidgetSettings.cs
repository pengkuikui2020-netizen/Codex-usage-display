namespace CodexUsageWidget.Models;

public sealed class WidgetSettings
{
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public int RefreshIntervalSeconds { get; set; } = 120;
}
