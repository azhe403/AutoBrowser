using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.ViewModels;
using Serilog;

namespace AutoBrowser;

public partial class App
{
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

        // Apply saved theme AFTER window exists (Gallery pattern)
        var savedTheme = _settingsService.LoadSettings().ThemeMode;
        ApplyTheme(savedTheme);

        var vm = Services.GetRequiredService<MainViewModel>();
        
        var args = Environment.GetCommandLineArgs();
        var forceUpdate = Array.Exists(args, arg => arg.Equals("--force-update-check", StringComparison.OrdinalIgnoreCase));

        if (_mainWindow == null) return;

        // Delay prompt and update check so window is fully rendered and content is completely visible first
        _mainWindow.ContentRendered += async (s, ev) =>
        {
            // Give extra frame time for full visual layout stability
            await Task.Delay(200);

            // Run silent update check first
            await vm.StartSilentUpdateCheckAsync(forceUpdate);
            
            // Then prompt path re-registration
            await CheckAndPromptReRegister();
            
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
        };
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();
        Log.Debug("Window_StateChanged: WindowState={WindowState}, MinimizeToTray={MinimizeToTray}",
            _mainWindow.WindowState, settings.MinimizeToTray);
        if (_mainWindow.WindowState == WindowState.Minimized && settings.MinimizeToTray)
            _mainWindow.Hide();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_mainWindow == null) return;
        var settings = _settingsService.LoadSettings();
        SaveWindowState();
        Log.Debug("MainWindow_Closing: _isExiting={IsExiting}, CloseToTray={CloseToTray}",
            _isExiting, settings.CloseToTray);

        if (!_isExiting && settings.CloseToTray)
        {
            e.Cancel = true;
            _mainWindow.Hide();
        }
        else
        {
            _isExiting = true;
            _trayIcon?.Dispose();
            // Force shutdown to prevent background threads/server keeping app alive
            Application.Current.Shutdown();
        }
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
}
