using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using AutoBrowser.Views;
using Serilog;
using Wpf.Ui.Appearance;

namespace AutoBrowser;

public partial class App : Application
{
    private const string MutexName = "AutoBrowser-SingleInstance";

    public static IServiceProvider Services { get; private set; } = null!;

    private ISettingsService _settingsService = null!;
    private IProtocolService _protocolService = null!;
    private IDefaultBrowserService _defaultBrowserService = null!;
    private IRuleService _ruleService = null!;

    private SingleInstanceService? _singleInstanceService;
    private MainWindow? _mainWindow;
    private Mutex? _mutex;
    public AppThemeMode CurrentThemeMode { get; private set; }

    private NotifyIcon? _trayIcon;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureLogging();
        RegisterExceptionHandlers();

        Log.Information("=== AutoBrowser Started ===");
        Log.Debug("OS: {OS}", Environment.OSVersion);
        Log.Debug("CLR: {CLR}", Environment.Version);
        Log.Debug("64-bit: {Is64Bit}", Environment.Is64BitProcess);
        Log.Debug("Path: {Path}", Environment.ProcessPath);
        Log.Debug("Args: {Args}", string.Join(" ", e.Args));

        var services = new ServiceCollection();
        services.AddSingleton<IRuleService, RuleService>();
        services.AddSingleton<IProtocolService, ProtocolService>();
        services.AddSingleton<IDefaultBrowserService, DefaultBrowserService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<MainWindow>();
        services.AddSingleton<Wpf.Ui.Abstractions.INavigationViewPageProvider, NavigationViewPageProvider>();
        services.AddTransient<HomePage>();
        services.AddTransient<AboutPage>();
        services.AddTransient<SettingsPage>();

        Services = services.BuildServiceProvider();

        _settingsService = Services.GetRequiredService<ISettingsService>();
        _protocolService = Services.GetRequiredService<IProtocolService>();
        _defaultBrowserService = Services.GetRequiredService<IDefaultBrowserService>();
        _ruleService = Services.GetRequiredService<IRuleService>();

        base.OnStartup(e);

        // Extract URL arg once — used for early routing and later pipe signaling
        var urlArg = e.Args.Length > 0 ? e.Args[0] : null;
        if (IsUrl(urlArg))
        {
            Log.Debug("URL argument received: {Url}", urlArg);
            if (TryRouteUrl(urlArg!))
            {
                Shutdown();
                return;
            }
        }

        // Single-instance guard: if already running, signal it and exit
        _mutex = new Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            Log.Information("Another instance detected, signaling via pipe");
            SingleInstanceService.SignalExistingInstance(urlArg);
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }
        Log.Information("Single instance mutex acquired");

        ShowMainWindow();
        StartPipeServer();
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        Log.Debug("Applying theme: {Theme}", mode);
        var theme = mode == AppThemeMode.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(theme);
        CurrentThemeMode = mode;
    }

    public void SaveTheme(AppThemeMode mode)
    {
        CurrentThemeMode = mode;
        var settings = _settingsService.LoadSettings();
        settings.ThemeMode = mode;
        _settingsService.SaveSettings(settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("OnExit (exit code: {ExitCode})", e.ApplicationExitCode);
        _singleInstanceService?.Dispose();
        _mutex?.Dispose();
        _trayIcon?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
        Environment.Exit(e.ApplicationExitCode); // forcefully kill any rogue background threads
    }


    // ── Private helpers ──────────────────────────────────────────────

    private void ConfigureLogging()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{ThreadId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(logDir, "AutoBrowser-.log"),
                rollingInterval: RollingInterval.Day,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{ThreadId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 14)
            .CreateLogger();
    }

    private void RegisterExceptionHandlers()
    {
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

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };
    }


}
