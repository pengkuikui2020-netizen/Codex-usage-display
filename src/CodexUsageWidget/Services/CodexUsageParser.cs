using System.Text.RegularExpressions;
using System.Globalization;
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

        if (url.Contains("tab=analytics", StringComparison.OrdinalIgnoreCase) ||
            five.Percent is null || week.Percent is null)
        {
            return new CodexUsageData
            {
                ErrorMessage = "未读到完整的当前额度；请刷新或重新登录。"
            };
        }

        return new CodexUsageData
        {
            FiveHourRemainingPercent = five.Percent,
            FiveHourResetText = five.Reset,
            WeeklyRemainingPercent = week.Percent,
            WeeklyResetText = week.Reset,
            UpdatedAt = DateTime.Now
        };
    }

    public static bool LooksReady(string pageText)
    {
        var text = Normalize(pageText);
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return ExtractSection(lines, FiveHourKeys).Percent.HasValue &&
               ExtractSection(lines, WeekKeys).Percent.HasValue;
    }

    public static bool LooksLikeLoginPage(string currentUrl, string pageText) =>
        LooksLikeLogin(currentUrl ?? string.Empty, Normalize(pageText));

    private static (int? Percent, string? Reset) ExtractSection(string[] lines, string[] keys)
    {
        var indices = Enumerable.Range(0, lines.Length)
            .Where(i => IsHeading(lines[i], keys))
            .ToArray();

        foreach (var idx in indices)
        {
            var start = idx;
            var end = idx;
            while (end + 1 < lines.Length && end < idx + 5)
            {
                var next = lines[end + 1];
                if (IsHeading(next, FiveHourKeys) || IsHeading(next, WeekKeys) ||
                    SectionBoundaryRegex().IsMatch(next)) break;
                end++;
            }
            var window = lines[start..(end + 1)];
            var joined = string.Join("\n", window);

            var percent = ParseRemainingPercent(joined);
            var reset = ParseReset(window);
            if (percent.HasValue)
                return (percent, reset);
        }

        return (null, null);
    }

    private static bool IsHeading(string line, string[] keys) => keys.Any(key =>
        line.Equals(key, StringComparison.OrdinalIgnoreCase) ||
        (line.StartsWith(key, StringComparison.OrdinalIgnoreCase) &&
         line.Length > key.Length &&
         (char.IsWhiteSpace(line[key.Length]) || line[key.Length] is ':' or '：') &&
         (RemainingPercentRegex().IsMatch(line[key.Length..]) || UsedPercentRegex().IsMatch(line[key.Length..]))));

    private static int? ParseRemainingPercent(string text)
    {
        foreach (Match m in RemainingPercentRegex().Matches(text))
        {
            var raw = m.Groups["pct"].Success ? m.Groups["pct"].Value : m.Groups["pct2"].Value;
            return ToRemainingPercent(raw, used: false);
        }

        foreach (Match m in UsedPercentRegex().Matches(text))
        {
            var raw = m.Groups["pct"].Success ? m.Groups["pct"].Value : m.Groups["pct2"].Value;
            return ToRemainingPercent(raw, used: true);
        }

        return null;
    }

    private static int? ToRemainingPercent(string raw, bool used)
    {
        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var percent) ||
            percent < 0 || percent > 100) return null;
        return (int)decimal.Round(used ? 100 - percent : percent, 0, MidpointRounding.AwayFromZero);
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

    [GeneratedRegex(@"(?:(?<![\d.,<>+\-])(?<pct>\d{1,3}(?:\.\d+)?)\s*%\s*(?:remaining|left|available|剩余|可用))|(?:(?:remaining|left|available|剩余|可用)\s*[:：]?\s*(?<pct2>\d{1,3}(?:\.\d+)?)\s*%)", RegexOptions.IgnoreCase)]
    private static partial Regex RemainingPercentRegex();

    [GeneratedRegex(@"(?:(?<![\d.,<>+\-])(?<pct>\d{1,3}(?:\.\d+)?)\s*%\s*(?:used|consumed|已使用|已用))|(?:(?:used|consumed|已使用|已用)\s*[:：]?\s*(?<pct2>\d{1,3}(?:\.\d+)?)\s*%)", RegexOptions.IgnoreCase)]
    private static partial Regex UsedPercentRegex();

    [GeneratedRegex(@"^(?:credits?|daily usage|usage history|plan usage history|code review.*|额度|剩余额度.*|每日用量|用量历史记录|套餐用量历史|代码审查.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SectionBoundaryRegex();
}
