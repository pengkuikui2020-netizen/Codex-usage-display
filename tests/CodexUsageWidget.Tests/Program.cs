using System.Reflection;
using System.Windows.Controls;
using CodexUsageWidget;
using CodexUsageWidget.Models;
using CodexUsageWidget.Services;

internal static class Program
{
    private const string OverviewUrl = "https://chatgpt.com/settings/usage?tab=overview";
    private static int _passed;

    [STAThread]
    private static void Main()
    {
        try
        {
            Check("current Chinese overview", "5 小时限额\n4 小时 20 分钟后重置\n剩余 87%\n每周限额\n4 天 11 小时后重置\n剩余 21%", 87, 21);
            Check("English remaining", "5 hour usage limit\n87% remaining\nResets in 4 hours\nWeekly usage limit\n21% remaining\nResets in 4 days", 87, 21);
            Check("used conversion", "5-hour limit\n13% used\nWeekly limit\n已使用 79%", 87, 21);
            Check("split labels", "5 小时\n剩余\n87%\n每周\n21%\n剩余", 87, 21);
            Check("inline headings", "5 hour usage limit: 87% remaining\nWeekly usage limit: 21% remaining", 87, 21);
            Check("decimal used", "5 小时限额\n17.8% used\n每周限额\n50.1% used", 82, 50);
            Check("decimal remaining", "5 小时限额\n剩余 87.5%\n每周限额\n21.4% remaining", 88, 21);
            Check("zero and full", "5 小时限额\n剩余 0%\n每周限额\n剩余 100%", 0, 100);
            Check("reversed cards", "每周限额\n剩余 21%\n5 小时限额\n剩余 87%", 87, 21);
            Check("historical analytics decimals", "套餐用量历史\n5 小时限额\n周期\t已使用限额百分比\n9/30 – 10/1\n17.8%\n每周限额\n周期\t已使用限额百分比\n9/30 – 10/7\n50.1%", null, null);
            Check("bare percentages rejected", "5 小时限额\n17.8%\n每周限额\n50.1%", null, null);
            Check("loading card cannot borrow weekly", "5 小时限额\n正在加载…\n每周限额\n剩余 21%", null, null);
            Check("weekly cannot borrow credits", "5 小时限额\n剩余 87%\n每周限额\n额度\n剩余 42%", null, null);
            Check("weekly cannot borrow daily history", "5 小时限额\n剩余 87%\n每周限额\n每日用量\n21% remaining", null, null);
            Check("code review excluded", "5 小时限额\n剩余 87%\nWeekly code review limit\n21% remaining", null, null);
            foreach (var malformed in new[] { "101.2% remaining", "117.8% remaining", "1000% remaining", "-17.8% remaining", "<0.1% remaining", "剩余 -1%", "剩余 101%" })
                Check("invalid percentage " + malformed, "5 小时限额\n" + malformed + "\n每周限额\n剩余 21%", null, null);
            Check("empty page", "", null, null);
            Check("fresh values replace previous read", "5 小时限额\n剩余 86%\n每周限额\n剩余 20%", 86, 20);

            var login = CodexUsageParser.Parse("Sign in", "https://auth.openai.com/log-in");
            Assert(login.LoginRequired && !login.HasUsage, "login state");
            var redirected = CodexUsageParser.Parse("5 小时限额\n剩余 87%\n每周限额\n剩余 21%", "https://chatgpt.com/settings/usage?tab=analytics");
            Assert(!redirected.HasUsage, "analytics URL rejected");
            Assert(CodexUsageService.UsageUrl == OverviewUrl, "service targets current overview");

            var main = new MainWindow();
            var apply = typeof(MainWindow).GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance)!;
            apply.Invoke(main, [CodexUsageParser.Parse("5 小时限额\n4 小时后重置\n剩余 87%\n每周限额\n4 天后重置\n剩余 21%", OverviewUrl)]);
            Assert(Text(main, "FiveValue") == "87% 剩余" && Text(main, "WeekValue") == "21% 剩余", "expanded display");
            Assert(Text(main, "CompactSummary") == "5H 87% · W 21%", "compact display");
            Assert(Text(main, "FiveReset") == "4 小时后重置", "Chinese reset preserved");
            apply.Invoke(main, [new CodexUsageData { ErrorMessage = "读取失败" }]);
            Assert(Text(main, "FiveValue") == "--% 剩余" && Text(main, "WeekValue") == "--% 剩余" &&
                   Text(main, "FiveReset") == "" && Text(main, "WeekReset") == "" && Text(main, "UpdatedText") == "", "failed refresh clears stale display");
            Console.WriteLine($"PASS: {_passed} regression checks");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Environment.ExitCode = 1;
        }
    }

    private static string Text(MainWindow window, string name) => ((TextBlock)window.FindName(name)).Text;

    private static void Check(string name, string text, int? five, int? week)
    {
        var data = CodexUsageParser.Parse(text, OverviewUrl);
        if (data.FiveHourRemainingPercent != five || data.WeeklyRemainingPercent != week)
            throw new Exception($"{name}: expected {five}/{week}, got {data.FiveHourRemainingPercent}/{data.WeeklyRemainingPercent}");
        if (CodexUsageParser.LooksReady(text) != (five.HasValue && week.HasValue))
            throw new Exception(name + ": incorrect readiness");
        _passed++;
    }

    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        _passed++;
    }
}
