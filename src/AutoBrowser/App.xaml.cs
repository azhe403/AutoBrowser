using System.IO;
using AutoBrowser.Models;
using AutoBrowser.Services;
using Serilog;
using Wpf.Ui.Appearance;

namespace AutoBrowser;

public partial class App : System.Windows.Application
{
    private static readonly string MutexName = "AutoBrowser-SingleInstance";
    private ISettingsService? _settingsService;
    public AppThemeMode CurrentThemeMode { get; private set; }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(logDir, "AutoBrowser-.log"),
                rollingInterval: RollingInterval.Day,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 14)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Error(ex, "AppDomain.UnhandledException");
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "DispatcherUnhandledException");
            args.Handled = true;
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };

        Log.Information("=== AutoBrowser Started ===");
        Log.Debug("OS: {OS}", Environment.OSVersion);
        Log.Debug("CLR: {CLR}", Environment.Version);
        Log.Debug("64-bit: {Is64Bit}", Environment.Is64BitProcess);
        Log.Debug("Path: {Path}", Environment.ProcessPath);
        Log.Debug("Args: {Args}", string.Join(" ", e.Args));

        base.OnStartup(e);

        _settingsService = new SettingsService();

        if (e.Args.Length > 0)
        {
            var url = e.Args[0];
            Log.Debug("URL argument received: {Url}", url);
            if (url.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Routing URL via UrlInterceptorService");
                var interceptor = new UrlInterceptorService(
                    new RuleService(), new DefaultBrowserService());
                var fallbackPath = _settingsService?.LoadSettings()?.FallbackBrowserPath;
                var browser = interceptor.TryRoute(url, fallbackPath);
                if (browser is not null)
                {
                    Log.Debug("URL routed via {Browser}, shutting down", browser);
                    Shutdown();
                    return;
                }
                Log.Debug("No match for URL, showing notification and continuing to main window");
                ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
            }
        }

        using var mutex = new System.Threading.Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            Log.Debug("Another instance already running, shutting down");
            System.Windows.MessageBox.Show("AutoBrowser is already running in the system tray.",
                "AutoBrowser", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Shutdown();
            return;
        }
        Log.Information("Single instance mutex acquired");

        var settings = _settingsService?.LoadSettings() ?? new AppSettings();
        Log.Debug("Settings loaded, theme: {Theme}", settings.ThemeMode);
        ApplyTheme(settings.ThemeMode);

        Log.Information("Creating MainWindow");
        var mainWindow = new MainWindow();
        mainWindow.Show();
        Log.Information("MainWindow shown");
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        Log.Debug("Applying theme: {Theme}", mode);
        var theme = mode == AppThemeMode.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

        ApplicationThemeManager.Apply(theme);
        CurrentThemeMode = mode;
        if (_settingsService is not null)
        {
            var settings = _settingsService.LoadSettings();
            settings.ThemeMode = mode;
            _settingsService.SaveSettings(settings);
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Information("OnExit (exit code: {ExitCode})", e.ApplicationExitCode);
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ShowNotification(string title, string message)
    {
        try
        {
            using var icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    Environment.ProcessPath ?? ""),
                Visible = true
            };
            icon.ShowBalloonTip(3000, title, message,
                System.Windows.Forms.ToolTipIcon.Warning);
        }
        catch { }
    }
}
