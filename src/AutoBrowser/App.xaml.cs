using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using AutoBrowser.Helpers;
using Serilog;
using Wpf.Ui.Appearance;

namespace AutoBrowser;

public partial class App : System.Windows.Application
{
    private const string MutexName = "AutoBrowser-SingleInstance";

    public static IServiceProvider Services { get; private set; } = null!;

    private ISettingsService _settingsService = null!;
    private IProtocolService _protocolService = null!;
    private IDefaultBrowserService _defaultBrowserService = null!;
    private IRuleService _ruleService = null!;

    private SingleInstanceService? _singleInstanceService;
    private MainWindow? _mainWindow;
    private System.Threading.Mutex? _mutex;
    public AppThemeMode CurrentThemeMode { get; private set; }

    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _isExiting;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
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
        services.AddTransient<MainWindow>();

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
        _mutex = new System.Threading.Mutex(true, MutexName, out var isNewInstance);
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

        ApplyTheme(_settingsService.LoadSettings().ThemeMode);
        ShowMainWindow();
        StartPipeServer();
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        Log.Debug("Applying theme: {Theme}", mode);
        var theme = mode == AppThemeMode.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(theme);
        CurrentThemeMode = mode;

        var settings = _settingsService.LoadSettings();
        settings.ThemeMode = mode;
        _settingsService.SaveSettings(settings);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Information("OnExit (exit code: {ExitCode})", e.ApplicationExitCode);
        _singleInstanceService?.Dispose();
        _mutex?.Dispose();
        _trayIcon?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    public void ProcessUrl(string url)
    {
        Log.Debug("ProcessUrl called: {Url}", url);

        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var vm = Services.GetRequiredService<MainViewModel>();
        var fallbackPath = vm.FallbackBrowser?.ExecutablePath;
        var browser = interceptor.TryRoute(url, fallbackPath);
        if (browser is not null)
        {
            Log.Information("URL routed via {Browser}: {Url}", browser, url);
            ShowNotification("AutoBrowser", $"Routed via {browser}:\n{url}");
            if (_mainWindow != null && _mainWindow.IsLoaded)
                vm.Status = $"Routed via {browser}: {url}";
            return;
        }

        Log.Warning("No rule matched for URL: {Url}", url);
        vm.Status = $"No match: {url}";
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
    }

    public void ActivateFromTray(string? url = null)
    {
        if (_mainWindow == null) return;
        Log.Information("ActivateFromTray called, Url={Url}", url ?? "(none)");

        WindowForegroundHelper.BringToFront(_mainWindow);

        if (!string.IsNullOrEmpty(url))
        {
            Log.Information("Processing forwarded URL: {Url}", url);
            ProcessUrl(url);
        }

        Log.Debug("ActivateFromTray complete");
    }

    // ── Private helpers ──────────────────────────────────────────────

    private void ConfigureLogging()
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

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };
    }

    private static bool IsUrl(string? value) =>
        value is not null
        && (value.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private bool TryRouteUrl(string url)
    {
        Log.Information("Routing URL via UrlInterceptorService");
        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var fallbackPath = _settingsService.LoadSettings().FallbackBrowserPath;
        var browser = interceptor.TryRoute(url, fallbackPath);

        if (browser is not null)
        {
            Log.Debug("URL routed via {Browser}, shutting down", browser);
            ShowNotification("AutoBrowser", $"Routed via {browser}:\n{url}");
            return true;
        }

        Log.Debug("No match for URL, showing notification and continuing to main window");
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        return false;
    }

    private void ShowMainWindow()
    {
        Log.Information("Creating MainWindow");
        _mainWindow = Services.GetRequiredService<MainWindow>();

        _mainWindow.Loaded += MainWindow_Loaded;
        _mainWindow.Closing += MainWindow_Closing;
        _mainWindow.StateChanged += MainWindow_StateChanged;

        RestoreWindowState();

        _mainWindow.Show();
        Log.Information("MainWindow shown");
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("MainWindow loaded, setting up tray icon");
        SetupTrayIcon();

        var vm = Services.GetRequiredService<MainViewModel>();
        await CheckAndPromptReRegister();

        var args = Environment.GetCommandLineArgs();
        var forceUpdate = Array.Exists(args, arg => arg.Equals("--force-update-check", StringComparison.OrdinalIgnoreCase));
        await vm.StartSilentUpdateCheckAsync(forceUpdate);
        if (args.Length > 1)
        {
            var url = args[1];
            Log.Debug("Command-line URL argument: {Url}", url);
            if (IsUrl(url))
            {
                ProcessUrl(url);
            }
            else
            {
                Log.Debug("Ignoring non-URL argument: {Url}", url);
            }
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow == null) return;
        var vm = Services.GetRequiredService<MainViewModel>();
        Log.Debug("Window_StateChanged: WindowState={WindowState}, MinimizeToTray={MinimizeToTray}",
            _mainWindow.WindowState, vm.MinimizeToTray);
        if (_mainWindow.WindowState == WindowState.Minimized && vm.MinimizeToTray)
            _mainWindow.Hide();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_mainWindow == null) return;
        var vm = Services.GetRequiredService<MainViewModel>();
        SaveWindowState();
        vm.SaveRules();
        Log.Debug("MainWindow_Closing: _isExiting={IsExiting}, CloseToTray={CloseToTray}",
            _isExiting, vm.CloseToTray);

        if (!_isExiting && vm.CloseToTray)
        {
            e.Cancel = true;
            _mainWindow.Hide();
        }
        else
        {
            _isExiting = true;
            _trayIcon?.Dispose();
        }
    }

    private void StartPipeServer()
    {
        _singleInstanceService = new SingleInstanceService();
        _singleInstanceService.StartServer(
            url =>
            {
                Log.Information("Second instance requested activation, Url={Url}", url ?? "(none)");
                ActivateFromTray(url);
            },
            Dispatcher);
    }

    private static void ShowNotification(string title, string message)
    {
        try
        {
            using var icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
                Visible = true
            };
            icon.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Warning);
        }
        catch { }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
            Text = "AutoBrowser - URL Router",
            Visible = true
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Show Window", null, (_, _) => ShowWindow());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        Log.Debug("Tray icon created and visible");
    }

    private void ShowWindow()
    {
        if (_mainWindow == null) return;
        Log.Debug("ShowWindow called from tray");
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        Log.Debug("Window restored from tray");
    }

    private void ExitApp()
    {
        Log.Information("ExitApp called from tray context menu");
        SaveWindowState();
        _isExiting = true;
        _trayIcon?.Dispose();
        Shutdown();
    }

    private void RestoreWindowState()
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();

        _mainWindow.Width = settings.WindowWidth;
        _mainWindow.Height = settings.WindowHeight;

        if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
        {
            _mainWindow.Left = settings.WindowLeft;
            _mainWindow.Top = settings.WindowTop;
            _mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        if (settings.IsMaximized)
            _mainWindow.WindowState = WindowState.Maximized;

        Log.Debug("Window state restored: {Width}x{Height} at ({Left},{Top}), Maximized={Maximized}",
            _mainWindow.Width, _mainWindow.Height, _mainWindow.Left, _mainWindow.Top, settings.IsMaximized);
    }

    private void SaveWindowState()
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();

        if (_mainWindow.WindowState == WindowState.Maximized)
        {
            settings.IsMaximized = true;
            settings.WindowLeft = _mainWindow.RestoreBounds.Left;
            settings.WindowTop = _mainWindow.RestoreBounds.Top;
            settings.WindowWidth = _mainWindow.RestoreBounds.Width;
            settings.WindowHeight = _mainWindow.RestoreBounds.Height;
        }
        else
        {
            settings.IsMaximized = false;
            settings.WindowLeft = _mainWindow.Left;
            settings.WindowTop = _mainWindow.Top;
            settings.WindowWidth = _mainWindow.Width;
            settings.WindowHeight = _mainWindow.Height;
        }

        _settingsService.SaveSettings(settings);
        Log.Debug("Window state saved: {Width}x{Height} at ({Left},{Top}), Maximized={Maximized}",
            settings.WindowWidth, settings.WindowHeight, settings.WindowLeft, settings.WindowTop, settings.IsMaximized);
    }

    private async Task CheckAndPromptReRegister()
    {
        Log.Information("CheckAndPromptReRegister called");

        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            Log.Debug("Cannot determine current process path, skipping re-register check");
            return;
        }

        var needsReRegister = false;
        var registrationType = string.Empty;

        // Check autobrowser:// protocol registration
        if (_protocolService.IsProtocolRegistered())
        {
            var registeredPath = _protocolService.GetRegisteredPath();
            Log.Debug("Protocol registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredPath, currentPath);

            if (!string.IsNullOrEmpty(registeredPath)
                && !registeredPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                needsReRegister = true;
                registrationType = "autobrowser:// protocol handler";
            }
        }

        // Check default browser registration
        if (_defaultBrowserService.IsDefaultBrowser())
        {
            var registeredPath = _defaultBrowserService.GetRegisteredPath();
            Log.Debug("Default browser registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredPath, currentPath);

            if (!string.IsNullOrEmpty(registeredPath)
                && !registeredPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                needsReRegister = true;
                registrationType = string.IsNullOrEmpty(registrationType)
                    ? "system default browser"
                    : registrationType + " and system default browser";
            }
        }

        if (needsReRegister)
        {
            Log.Information("App path has changed, prompting user to re-register: {Type}", registrationType);

            var oldProtocolPath = _protocolService.IsProtocolRegistered() ? _protocolService.GetRegisteredPath() : null;
            var oldDefaultPath = _defaultBrowserService.IsDefaultBrowser() ? _defaultBrowserService.GetRegisteredPath() : null;
            var oldPath = oldProtocolPath ?? oldDefaultPath ?? "(unknown)";

            var contentPanel = new System.Windows.Controls.StackPanel();

            var descText = new System.Windows.Controls.TextBlock
            {
                Text = $"AutoBrowser has been moved to a new location, but the {registrationType} still points to the old path.",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 0, 0, 16),
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorPrimaryBrush")
            };
            contentPanel.Children.Add(descText);

            var cardBorder = new System.Windows.Controls.Border
            {
                Background = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("ControlFillColorDefaultBrush"),
                BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("ControlElevationBorderBrush"),
                BorderThickness = new System.Windows.Thickness(1),
                CornerRadius = new System.Windows.CornerRadius(6),
                Padding = new System.Windows.Thickness(12),
                Margin = new System.Windows.Thickness(0, 0, 0, 16)
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(8) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var oldLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Old Path:",
                FontWeight = System.Windows.FontWeights.SemiBold,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorSecondaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(oldLabel, 0);
            System.Windows.Controls.Grid.SetRow(oldLabel, 0);
            grid.Children.Add(oldLabel);

            var oldPathText = new System.Windows.Controls.TextBlock
            {
                Text = oldPath,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorTertiaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(oldPathText, 1);
            System.Windows.Controls.Grid.SetRow(oldPathText, 0);
            grid.Children.Add(oldPathText);

            var newLabel = new System.Windows.Controls.TextBlock
            {
                Text = "New Path:",
                FontWeight = System.Windows.FontWeights.SemiBold,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorSecondaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(newLabel, 0);
            System.Windows.Controls.Grid.SetRow(newLabel, 2);
            grid.Children.Add(newLabel);

            var newPathText = new System.Windows.Controls.TextBlock
            {
                Text = currentPath,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorTertiaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(newPathText, 1);
            System.Windows.Controls.Grid.SetRow(newPathText, 2);
            grid.Children.Add(newPathText);

            cardBorder.Child = grid;
            contentPanel.Children.Add(cardBorder);

            var questionText = new System.Windows.Controls.TextBlock
            {
                Text = "Would you like to re-register now?",
                FontWeight = System.Windows.FontWeights.SemiBold,
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorPrimaryBrush")
            };
            contentPanel.Children.Add(questionText);

            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = "AutoBrowser — Path Changed",
                Content = contentPanel,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                IsCloseButtonEnabled = false,
                Width = 550,
                MinWidth = 550
            };
            dialog.Owner = _mainWindow;
            var result = await dialog.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                var vm = Services.GetRequiredService<MainViewModel>();
                if (registrationType.Contains("protocol"))
                {
                    _protocolService.UnregisterProtocolHandler();
                    _protocolService.RegisterProtocolHandler();
                    Log.Information("Protocol handler re-registered");
                }
                if (registrationType.Contains("default browser"))
                {
                    _defaultBrowserService.UnregisterAsDefaultBrowser();
                    _defaultBrowserService.RegisterAsDefaultBrowser();
                    Log.Information("Default browser registration updated");
                }

                ShowNotification("AutoBrowser", "Registration updated successfully.");
                vm.Status = "Registration updated to new path.";
            }
            else
            {
                Log.Debug("User declined re-registration");
            }
        }
        else
        {
            Log.Debug("Registration paths are current, no re-registration needed");
        }
    }
}
