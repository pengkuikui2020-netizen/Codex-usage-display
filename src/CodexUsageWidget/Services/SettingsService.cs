using System.IO;
using System.Text.Json;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsService()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CodexUsageWidget");
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "settings.json");
    }

    public WidgetSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new WidgetSettings();
            return JsonSerializer.Deserialize<WidgetSettings>(File.ReadAllText(_settingsPath)) ?? new WidgetSettings();
        }
        catch
        {
            return new WidgetSettings();
        }
    }

    public void Save(WidgetSettings settings)
    {
        try
        {
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, _jsonOptions));
        }
        catch
        {
            // Settings persistence should never crash the widget.
        }
    }
}
