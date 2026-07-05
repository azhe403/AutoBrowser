using System.Windows;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _isExiting;

    public MainWindow()
    {
        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;

        Log.Debug("MainWindow initialized: MinimizeToTray={Minimize}, CloseToTray={Close}",
            _viewModel.MinimizeToTray, _viewModel.CloseToTray);
    }

    public void ProcessUrl(string url)
    {
        Log.Debug("ProcessUrl called: {Url}", url);

        var interceptor = new UrlInterceptorService(
            new RuleService(), new DefaultBrowserService());
        var fallbackPath = _viewModel.FallbackBrowser?.ExecutablePath;
        var browser = interceptor.TryRoute(url, fallbackPath);
        if (browser is not null)
        {
            Log.Information("URL routed via {Browser}: {Url}", browser, url);
            if (IsLoaded)
                _viewModel.Status = $"Routed via {browser}: {url}";
            return;
        }

        Log.Warning("No rule matched for URL: {Url}", url);
        _viewModel.Status = $"No match: {url}";
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
    }

    private static void ShowNotification(string title, string message)
    {
        Log.Debug("ShowNotification: {Title} - {Message}", title, message);
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
        catch (Exception ex)
        {
            Log.Warning(ex, "ShowNotification failed");
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("MainWindow loaded, setting up tray icon");
        SetupTrayIcon();
        _viewModel.StartSilentUpdateCheck();

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var url = args[1];
            Log.Debug("Command-line URL argument: {Url}", url);
            if (url.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ProcessUrl(url);
            }
            else
            {
                Log.Debug("Ignoring non-URL argument: {Url}", url);
            }
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                Environment.ProcessPath ?? ""),
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
        Log.Debug("ShowWindow called from tray");
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Log.Debug("Window restored from tray");
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        Log.Debug("Window_StateChanged: WindowState={WindowState}, MinimizeToTray={MinimizeToTray}",
            WindowState, _viewModel.MinimizeToTray);
        if (WindowState == WindowState.Minimized && _viewModel.MinimizeToTray)
            Hide();
    }

    private void CheckBox_Toggled(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.SaveRules();
    }

    private void RuleListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _viewModel.EditRuleCommand.Execute(null);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.SaveRules();
        Log.Debug("OnClosing: _isExiting={IsExiting}, CloseToTray={CloseToTray}",
            _isExiting, _viewModel.CloseToTray);

        if (!_isExiting && _viewModel.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _isExiting = true;
            _trayIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void ExitApp()
    {
        Log.Information("ExitApp called from tray context menu");
        _isExiting = true;
        _trayIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
