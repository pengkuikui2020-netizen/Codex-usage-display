using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace CodexUsageWidget;

public partial class LoginWindow : Window
{
    private readonly CoreWebView2Environment _environment;

    public LoginWindow(CoreWebView2Environment environment)
    {
        InitializeComponent();
        _environment = environment;
        Loaded += LoginWindow_Loaded;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoginBrowser.EnsureCoreWebView2Async(_environment);
            LoginBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            LoginBrowser.CoreWebView2.Navigate(Services.CodexUsageService.AnalyticsUrl);
        }
        catch (Exception ex)
        {
            HintText.Text = $"WebView2 启动失败：{ex.Message}";
        }
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
