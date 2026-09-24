using System.Text.RegularExpressions;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public static partial class CodexUsageParser
{
    private static readonly string[] FiveHourKeys =
    [
        "5 hour usage limit", "5-hour usage limit", "5 hour limit", "5-hour limit",
        "five hour usage limit", "five-hour usage limit", "five hour limit", "five-hour limit",
        "5 hour", "5-hour", "five hour", "five-hour",
        "5 小时使用限制", "5小时使用限制", "5 小时限额", "5小时限额", "5 小时", "5小时"
    ];

    private static readonly string[] WeekKeys =
    [
        "weekly usage limit", "week usage limit", "weekly limit", "week limit", "weekly", "week",
        "每周使用限制", "周使用限制", "每周限额", "周限额", "每周额度", "周额度", "每周"
    ];

    public static CodexUsageData Parse(string pageText, string currentUrl)
    {
        var text = Normalize(pageText);
        var url = currentUrl ?? string.Empty;

        if (LooksLikeLogin(url, text))
        {
            return new CodexUsageData
            {
                LoginRequired = true,
                ErrorMessage = "需要登录 ChatGPT"
            };
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var five = ExtractSection(lines, FiveHourKeys);
        var week = ExtractSection(lines, WeekKeys);

        if (five.Percent is null && ContainsAny(text, FiveHourKeys))
            five.Percent = ExtractNearestPercent(text, FiveHourKeys);
        if (week.Percent is null && ContainsAny(text, WeekKeys))
            week.Percent = ExtractNearestPercent(text, WeekKeys);

        if (five.Percent is null && week.Percent is null)
        {
            return new CodexUsageData
            {
                ErrorMessage = "页面已打开，但暂未识别到额度；请点 ↻ 再试或点 ● 重新登录。"
            };
        }

        return new CodexUsageData
        {
            FiveHourRemainingPercent = Clamp(five.Percent),
            FiveHourResetText = five.Reset,
            WeeklyRemainingPercent = Clamp(week.Percent),
            WeeklyResetText = week.Reset,
            UpdatedAt = DateTime.Now
        };
    }

    public static bool LooksReady(string pageText)
    {
        var text = Normalize(pageText);
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (ContainsAny(text, FiveHourKeys) && AnyPercentRegex().IsMatch(text)) return true;
        if (ContainsAny(text, WeekKeys) && AnyPercentRegex().IsMatch(text)) return true;
        return false;
    }

    public static bool LooksLikeLoginPage(string currentUrl, string pageText) =>
        LooksLikeLogin(currentUrl ?? string.Empty, Normalize(pageText));

    private static (int? Percent, string? Reset) ExtractSection(string[] lines, string[] keys)
    {
        var indices = Enumerable.Range(0, lines.Length)
            .Where(i => keys.Any(k => lines[i].Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var idx in indices)
        {
            var start = idx;
            var end = Math.Min(lines.Length - 1, idx + 10);
            var window = lines[start..(end + 1)];
            var joined = string.Join(" | ", window);

            var percent = ParseRemainingPercent(joined);
            var reset = ParseReset(window);
            if (percent.HasValue || reset is not null)
                return (percent, reset);
        }

        return (null, null);
    }

    private static int? ExtractNearestPercent(string text, string[] keys)
    {
        var lower = text.ToLowerInvariant();
        foreach (var key in keys)
        {
            var pos = lower.IndexOf(key.ToLowerInvariant(), StringComparison.Ordinal);
            if (pos < 0) continue;
            var len = Math.Min(800, lower.Length - pos);
            var slice = text.Substring(pos, len);
            var p = ParseRemainingPercent(slice);
            if (p.HasValue) return p;
        }
        return null;
    }

    private static int? ParseRemainingPercent(string text)
    {
        foreach (Match m in RemainingPercentRegex().Matches(text))
        {
            var raw = m.Groups["pct"].Success ? m.Groups["pct"].Value : m.Groups["pct2"].Value;
            if (int.TryParse(raw, out var pct)) return pct;
        }

        foreach (Match m in UsedPercentRegex().Matches(text))
        {
            var raw = m.Groups["pct"].Success ? m.Groups["pct"].Value : m.Groups["pct2"].Value;
            if (int.TryParse(raw, out var pct)) return 100 - pct;
        }

        var any = AnyPercentRegex().Match(text);
        if (any.Success && int.TryParse(any.Groups["pct"].Value, out var apct))
            return apct;

        return null;
    }

    private static string? ParseReset(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.Contains("reset", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("重置", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("恢复", StringComparison.OrdinalIgnoreCase))
            {
                var clean = Regex.Replace(line, @"\s+", " ").Trim();
                return clean.Length > 64 ? clean[..64] + "…" : clean;
            }
        }
        return null;
    }

    private static bool LooksLikeLogin(string url, string text)
    {
        if (url.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("login", StringComparison.OrdinalIgnoreCase)) return true;

        var hasLogin = text.Contains("log in", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("sign in", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("登录", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("登入", StringComparison.OrdinalIgnoreCase);
        var hasUsage = ContainsAny(text, FiveHourKeys) || ContainsAny(text, WeekKeys);
        return hasLogin && !hasUsage;
    }

    private static bool ContainsAny(string text, IEnumerable<string> keys) =>
        keys.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string text) =>
        (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

    private static int? Clamp(int? value) => value.HasValue ? Math.Clamp(value.Value, 0, 100) : null;

    [GeneratedRegex(@"(?:(?<pct>\d{1,3})\s*%\s*(?:remaining|left|available|剩余|可用))|(?:(?:remaining|left|available|剩余|可用)\s*[:：]?\s*(?<pct2>\d{1,3})\s*%)", RegexOptions.IgnoreCase)]
    private static partial Regex RemainingPercentRegex();

    [GeneratedRegex(@"(?:(?<pct>\d{1,3})\s*%\s*(?:used|consumed|已使用|已用))|(?:(?:used|consumed|已使用|已用)\s*[:：]?\s*(?<pct2>\d{1,3})\s*%)", RegexOptions.IgnoreCase)]
    private static partial Regex UsedPercentRegex();

    [GeneratedRegex(@"(?<pct>\d{1,3})\s*%", RegexOptions.IgnoreCase)]
    private static partial Regex AnyPercentRegex();
}
