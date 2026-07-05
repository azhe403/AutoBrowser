using System.Collections.ObjectModel;
using System.Windows;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace AutoBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRuleService _ruleService;
    private readonly IProtocolService _protocolService;
    private readonly IDefaultBrowserService _defaultBrowserService;
    private readonly ISettingsService _settingsService;
    private readonly UpdateService _updateService = new();

    public ObservableCollection<RoutingRule> Rules { get; } = [];
    public List<BrowserDefinition> AvailableBrowsers { get; } = BrowserDefinition.GetKnownBrowsers();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeLabel))]
    private bool _isDarkTheme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdate))]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdate))]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRule))]
    private RoutingRule? _selectedRule;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private string _updateStatus = "";

    [ObservableProperty]
    private BrowserDefinition? _fallbackBrowser;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrayLabel))]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrayLabel))]
    private bool _closeToTray = true;

    public string ThemeLabel => IsDarkTheme ? "Dark" : "Light";
    public string TrayLabel => (MinimizeToTray || CloseToTray) ? "Tray" : "Exit";
    public bool CanCheckForUpdate => !IsCheckingUpdate && !IsDownloadingUpdate;
    public bool HasSelectedRule => SelectedRule is not null;

    public bool IsProtocolRegistered
    {
        get => _protocolService.IsProtocolRegistered();
        set
        {
            if (value)
            {
                _protocolService.RegisterProtocolHandler();
                Status = "autobrowser:// protocol registered";
            }
            else
            {
                _protocolService.UnregisterProtocolHandler();
                Status = "autobrowser:// protocol unregistered";
            }
            OnPropertyChanged();
        }
    }

    public bool IsDefaultBrowser
    {
        get => _defaultBrowserService.IsDefaultBrowser();
        set
        {
            if (value)
            {
                _defaultBrowserService.RegisterAsDefaultBrowser();
                Status = "Registered as default browser — select AutoBrowser in Settings > Default Apps";
            }
            else
            {
                _defaultBrowserService.UnregisterAsDefaultBrowser();
                Status = "Unregistered as default browser";
            }
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        _ruleService = new RuleService();
        _protocolService = new ProtocolService();
        _defaultBrowserService = new DefaultBrowserService();
        _settingsService = new SettingsService();
        LoadRules();

        var app = (App)System.Windows.Application.Current;
        _isDarkTheme = app.CurrentThemeMode == AppThemeMode.Dark;

        var settings = _settingsService.LoadSettings();
        if (!string.IsNullOrEmpty(settings.FallbackBrowserPath))
            _fallbackBrowser = AvailableBrowsers.FirstOrDefault(b =>
                b.ExecutablePath.Equals(settings.FallbackBrowserPath, StringComparison.OrdinalIgnoreCase));

        _minimizeToTray = settings.MinimizeToTray;
        _closeToTray = settings.CloseToTray;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ((App)System.Windows.Application.Current).ApplyTheme(value ? AppThemeMode.Dark : AppThemeMode.Light);
        Status = value ? "Switched to Dark theme" : "Switched to Light theme";
    }

    partial void OnFallbackBrowserChanged(BrowserDefinition? value)
    {
        var settings = _settingsService.LoadSettings();
        settings.FallbackBrowserPath = value?.ExecutablePath ?? string.Empty;
        _settingsService.SaveSettings(settings);
        Status = value is not null ? $"Fallback: {value.DisplayName}" : "Fallback browser cleared";
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        Log.Debug("MinimizeToTray changed to {Value}", value);
        var settings = _settingsService.LoadSettings();
        settings.MinimizeToTray = value;
        _settingsService.SaveSettings(settings);
        Status = value ? "Minimize to tray enabled" : "Minimize to tray disabled";
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        Log.Debug("CloseToTray changed to {Value}", value);
        var settings = _settingsService.LoadSettings();
        settings.CloseToTray = value;
        _settingsService.SaveSettings(settings);
        Status = value ? "Close to tray enabled" : "Close to tray disabled";
    }

    public void StartSilentUpdateCheck()
    {
        _ = CheckForUpdateSilentAsync();
    }

    private async Task CheckForUpdateSilentAsync()
    {
        try
        {
            Log.Information("Silent update check starting");
            var release = await _updateService.CheckForUpdateAsync();
            if (release is null || !release.IsNewer)
            {
                Status = "App is up to date";
                Log.Debug("Silent update check: no update available");
                return;
            }
            Log.Information("Silent update check: v{Version} available, showing dialog", release.Version);
            Status = $"Update available: v{release.Version}";
            await ShowUpdateDialogAsync(release);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Silent update check failed");
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        var dialog = new RuleEditorView();
        if (dialog.ShowDialog() == true)
        {
            Rules.Add(dialog.Rule);
            SelectedRule = dialog.Rule;
            SaveRules();
            Status = $"Rule \"{dialog.Rule.Name}\" added";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void EditRule()
    {
        if (SelectedRule is null) return;

        var index = Rules.IndexOf(SelectedRule);
        var dialog = new RuleEditorView(SelectedRule);
        if (dialog.ShowDialog() == true)
        {
            Rules[index] = dialog.Rule;
            SelectedRule = dialog.Rule;
            SaveRules();
            Status = $"Rule \"{dialog.Rule.Name}\" updated";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void DeleteRule()
    {
        if (SelectedRule is null) return;
        var name = SelectedRule.Name;
        Rules.Remove(SelectedRule);
        SaveRules();
        Status = $"Rule \"{name}\" deleted";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void MoveUp() => MoveRule(-1);

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void MoveDown() => MoveRule(1);

    private void MoveRule(int direction)
    {
        if (SelectedRule is null) return;

        var index = Rules.IndexOf(SelectedRule);
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= Rules.Count) return;

        Rules.Move(index, newIndex);
        SaveRules();
        Status = $"Rule \"{SelectedRule.Name}\" moved";
    }

    [RelayCommand]
    private void LaunchUrl()
    {
        var dialog = new RuleTesterView("Test URL", "Enter URL to test routing:");
        dialog.ShowDialog();
        var url = dialog.Result;
        if (string.IsNullOrWhiteSpace(url)) return;

        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var browser = interceptor.TryRoute(url, FallbackBrowser?.ExecutablePath);
        if (browser is not null)
            Status = $"Routed via {browser}: {url}";
        else
        {
            Status = $"No match: {url}";
            ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        }
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        if (IsCheckingUpdate || IsDownloadingUpdate) return;

        IsCheckingUpdate = true;
        UpdateStatus = "Checking for updates...";
        Status = UpdateStatus;
        Log.Information("Manual update check starting");

        try
        {
            var release = await _updateService.CheckForUpdateAsync();
            if (release is null)
            {
                UpdateStatus = "No release info available (no releases or offline).";
                Status = UpdateStatus;
                Log.Debug("Manual update check: no release info");
                return;
            }

            if (!release.IsNewer)
            {
                UpdateStatus = $"You're up to date (v{release.Version}).";
                Status = UpdateStatus;
                Log.Debug("Manual update check: up to date (v{Version})", release.Version);
                return;
            }

            Log.Debug("Manual update check: v{Version} available", release.Version);
            await ShowUpdateDialogAsync(release);
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update failed: {ex.Message}";
            Status = UpdateStatus;
            Log.Error(ex, "Manual update check failed");
        }
        finally
        {
            IsCheckingUpdate = false;
            IsDownloadingUpdate = false;
        }
    }

    private async Task ShowUpdateDialogAsync(ReleaseInfo release)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Update Available",
            Content = $"Version {release.Version} is available.\n\nCurrent: {typeof(UpdateService).Assembly.GetName().Version}\n\nDownload and install?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            Width = 500,
            MinWidth = 500
        };
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        var result = await dialog.ShowDialogAsync();

        Log.Information("Update dialog result: {Result} for v{Version}", result, release.Version);

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

        IsDownloadingUpdate = true;
        UpdateStatus = "Downloading update...";
        Status = UpdateStatus;

        var progress = new Progress<double>(p =>
        {
            var pct = (int)(p * 100);
            UpdateStatus = $"Downloading... {pct}%";
            Status = UpdateStatus;
        });

        await _updateService.DownloadAndUpdateAsync(release, progress);
    }

    private void LoadRules()
    {
        Rules.Clear();
        foreach (var rule in _ruleService.LoadRules())
            Rules.Add(rule);
    }

    public void SaveRules()
    {
        _ruleService.SaveRules([..Rules]);
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

