using System.IO;
using System.Text.Json;
using AutoBrowser.Models;

namespace AutoBrowser.Services;

public class SettingsService : ISettingsService
{
    private static readonly string DataDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string SettingsPath = Path.Combine(DataDir, "settings.json");

    public AppSettings LoadSettings()
    {
        try
        {
            EnsureDataDir();

            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        EnsureDataDir();

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    private static void EnsureDataDir()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);
    }
}
