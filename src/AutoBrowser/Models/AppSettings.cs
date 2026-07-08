namespace AutoBrowser.Models;

public class AppSettings
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;
    public string LastTestUrl { get; set; } = "https://";
    public string FallbackBrowserPath { get; set; } = string.Empty;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public double WindowWidth { get; set; } = 950;
    public double WindowHeight { get; set; } = 650;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public bool IsMaximized { get; set; }
    public DateTime LastUpdateCheckTime { get; set; } = DateTime.MinValue;
}
