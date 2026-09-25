using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CodexUsageWidget.Models;
using CodexUsageWidget.Services;

namespace CodexUsageWidget;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly CodexUsageService _usageService = new();
    private WidgetSettings _settings = new();
    private DispatcherTimer? _timer;
    private bool _refreshing;
    private const double BarMaxWidth = 250;
    private const double ExpandedHeight = 164;
    private const double CollapsedHeight = 52;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsService.Load();
        RestorePosition();
        ApplyCollapsedState(_settings.IsCollapsed);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Clamp(_settings.RefreshIntervalSeconds, 30, 3600))
        };
        _timer.Tick += async (_, _) => await RefreshUsageAsync(false);
        _timer.Start();

        await RefreshUsageAsync(true);
    }

    private void RestorePosition()
    {
        var work = SystemParameters.WorkArea;
        if (_settings.WindowLeft.HasValue && _settings.WindowTop.HasValue &&
            _settings.WindowLeft >= work.Left - 50 && _settings.WindowLeft <= work.Right - 30 &&
            _settings.WindowTop >= work.Top - 50 && _settings.WindowTop <= work.Bottom - 30)
        {
            Left = _settings.WindowLeft.Value;
            Top = _settings.WindowTop.Value;
        }
        else
        {
            Left = work.Right - Width - 18;
            Top = work.Top + 18;
        }
    }

    private async Task RefreshUsageAsync(bool allowLoginPrompt)
    {
        if (_refreshing) return;
        _refreshing = true;
        RefreshButton.IsEnabled = false;
        if (!_settings.IsCollapsed)
            StatusText.Text = "正在读取…";

        try
        {
            var data = await _usageService.RefreshAsync();
            if (data.LoginRequired && allowLoginPrompt)
            {
                if (!_settings.IsCollapsed)
                    StatusText.Text = "需要登录";
                var logged = await _usageService.ShowLoginAsync(this);
                if (logged)
                    data = await _usageService.RefreshAsync();
            }
            Apply(data);
        }
        finally
        {
            _refreshing = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private void Apply(CodexUsageData data)
    {
        if (data.HasUsage)
        {
            SetUsage(data.FiveHourRemainingPercent, FiveValue, FiveBar);
            SetUsage(data.WeeklyRemainingPercent, WeekValue, WeekBar);
            FiveReset.Text = ShortReset(data.FiveHourResetText);
            WeekReset.Text = ShortReset(data.WeeklyResetText);
            UpdatedText.Text = data.UpdatedAt.ToString("HH:mm 更新");
            StatusText.Text = "";
            CompactSummary.Text = FormatCompactSummary(data.FiveHourRemainingPercent, data.WeeklyRemainingPercent);
            return;
        }

        FiveValue.Text = "--% 剩余";
        WeekValue.Text = "--% 剩余";
        FiveBar.Width = 0;
        WeekBar.Width = 0;
        UpdatedText.Text = "";
        StatusText.Text = data.LoginRequired ? "需要登录 · 点击 ●" : (data.ErrorMessage ?? "读取失败");
        CompactSummary.Text = data.LoginRequired ? "需登录" : "--";
    }

    private static void SetUsage(int? remaining, System.Windows.Controls.TextBlock label, FrameworkElement bar)
    {
        if (!remaining.HasValue)
        {
            label.Text = "--% 剩余";
            bar.Width = 0;
            return;
        }

        label.Text = $"{remaining.Value}% 剩余";
        bar.Width = BarMaxWidth * remaining.Value / 100.0;
    }

    private static string ShortReset(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var t = text.Trim();
        if (t.StartsWith("Resets", StringComparison.OrdinalIgnoreCase)) return t;
        return "Resets " + t;
    }

    private static string FormatCompactSummary(int? fiveHour, int? weekly)
    {
        var five = fiveHour.HasValue ? $"5H {fiveHour.Value}%" : "5H --";
        var week = weekly.HasValue ? $"W {weekly.Value}%" : "W --";
        return $"{five} · {week}";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshUsageAsync(false);

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        var logged = await _usageService.ShowLoginAsync(this);
        if (logged) await RefreshUsageAsync(false);
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        _settings.IsCollapsed = !_settings.IsCollapsed;
        ApplyCollapsedState(_settings.IsCollapsed);
        _settingsService.Save(_settings);
    }

    private void ApplyCollapsedState(bool collapsed)
    {
        FiveSection.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        WeekSection.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        FooterSection.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        FiveRow.Height = new GridLength(collapsed ? 0 : 50);
        WeekRow.Height = new GridLength(collapsed ? 0 : 50);
        FooterRow.Height = new GridLength(collapsed ? 0 : 15);

        CompactSummary.Visibility = collapsed ? Visibility.Visible : Visibility.Collapsed;
        LoginButton.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        RefreshButton.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        CloseButton.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        CollapseButton.Content = collapsed ? "▸" : "▾";
        CollapseButton.ToolTip = collapsed ? "展开" : "收起";
        Height = collapsed ? CollapsedHeight : ExpandedHeight;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        try { DragMove(); } catch { }
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settingsService.Save(_settings);
        _timer?.Stop();
        await _usageService.DisposeAsync();
    }
}
