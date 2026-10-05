using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public sealed class CodexUsageService : IAsyncDisposable
{
    public const string UsageUrl = "https://chatgpt.com/settings/usage?tab=overview";

    private CoreWebView2Environment? _environment;
    private HiddenBrowserWindow? _host;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task InitializeAsync()
    {
        if (_environment is not null && _host is not null) return;

        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexUsageWidget", "WebView2");
        Directory.CreateDirectory(dataRoot);

        _environment = await CoreWebView2Environment.CreateAsync(null, dataRoot);
        _host = new HiddenBrowserWindow();
        _host.Show();
        await _host.Browser.EnsureCoreWebView2Async(_environment);

        _host.Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _host.Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _host.Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
    }

    public async Task<CodexUsageData> RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            await InitializeAsync();
            if (_host?.Browser.CoreWebView2 is null)
                return new CodexUsageData { ErrorMessage = "WebView2 初始化失败。" };

            var completion = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object? _, CoreWebView2NavigationCompletedEventArgs args) => completion.TrySetResult(args);

            _host.Browser.NavigationCompleted += Handler;
            try
            {
                if (_host.Browser.Source?.AbsoluteUri == UsageUrl)
                    _host.Browser.CoreWebView2.Reload();
                else
                    _host.Browser.CoreWebView2.Navigate(UsageUrl);

                if (await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(25))) != completion.Task)
                {
                    _host.Browser.CoreWebView2.Stop();
                    return new CodexUsageData { ErrorMessage = "页面加载超时，请刷新重试。" };
                }
                var navigation = await completion.Task;
                if (!navigation.IsSuccess)
                    return new CodexUsageData { ErrorMessage = $"页面加载失败：{navigation.WebErrorStatus}" };
            }
            finally
            {
                _host.Browser.NavigationCompleted -= Handler;
            }

            string pageText = string.Empty;
            string currentUrl = string.Empty;
            for (var i = 0; i < 24; i++)
            {
                await Task.Delay(i == 0 ? 800 : 500);
                pageText = await ReadBodyTextAsync();
                currentUrl = _host.Browser.Source?.ToString() ?? string.Empty;

                if (CodexUsageParser.LooksLikeLoginPage(currentUrl, pageText))
                    return new CodexUsageData { LoginRequired = true, ErrorMessage = "需要登录 ChatGPT" };

                if (CodexUsageParser.LooksReady(pageText))
                    break;
            }

            return CodexUsageParser.Parse(pageText, currentUrl);
        }
        catch (Exception ex)
        {
            return new CodexUsageData { ErrorMessage = $"读取失败：{ex.Message}" };
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string> ReadBodyTextAsync()
    {
        if (_host?.Browser.CoreWebView2 is null) return string.Empty;
        var textJson = await _host.Browser.CoreWebView2.ExecuteScriptAsync(
            "document.body ? document.body.innerText : ''");
        return JsonSerializer.Deserialize<string>(textJson) ?? string.Empty;
    }

    public async Task<bool> ShowLoginAsync(Window owner)
    {
        await InitializeAsync();
        if (_environment is null) return false;

        var login = new LoginWindow(_environment) { Owner = owner };
        var result = login.ShowDialog();
        return result == true;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        _host?.Close();
        _host = null;
        _refreshLock.Dispose();
    }

    private sealed class HiddenBrowserWindow : Window
    {
        public WebView2 Browser { get; } = new();

        public HiddenBrowserWindow()
        {
            Width = 20;
            Height = 20;
            Left = -5000;
            Top = -5000;
            ShowInTaskbar = false;
            ShowActivated = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Opacity = 0.01;
            Content = Browser;
        }
    }
}
