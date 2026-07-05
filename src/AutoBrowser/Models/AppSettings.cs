namespace AutoBrowser.Models;

public class AppSettings
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;
    public string LastTestUrl { get; set; } = "https://";
    public string FallbackBrowserPath { get; set; } = string.Empty;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
}
