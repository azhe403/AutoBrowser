using System.IO;
using System.Text.Json;
using AutoBrowser.Models;
using Serilog;

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
            {
                Log.Debug("Settings file not found, returning defaults");
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            Log.Debug("Settings loaded: {Settings}", json.ReplaceLineEndings(" "));
            return settings;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load settings, returning defaults");
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        EnsureDataDir();

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
        Log.Debug("Settings saved: {Settings}", json.ReplaceLineEndings(" "));
    }

    private static void EnsureDataDir()
    {
        if (!Directory.Exists(DataDir))
        {
            Log.Debug("Creating data directory: {Path}", DataDir);
            Directory.CreateDirectory(DataDir);
        }
    }
}
