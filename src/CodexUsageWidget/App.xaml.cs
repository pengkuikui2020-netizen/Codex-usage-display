using System.Windows;

namespace CodexUsageWidget;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Codex Usage Widget 遇到错误：\n\n{args.Exception.Message}",
                "Codex Usage Widget", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var main = new MainWindow();
        MainWindow = main;
        main.Show();
    }
}
