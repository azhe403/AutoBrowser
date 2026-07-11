using System;
using System.Collections.Generic;
using System.Linq;
using AutoBrowser.Models;
using AutoBrowser.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace AutoBrowser.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IProtocolService _protocolService;
    private readonly IDefaultBrowserService _defaultBrowserService;
    private readonly ISettingsService _settingsService;

    public List<BrowserDefinition> AvailableBrowsers { get; } = BrowserDefinition.GetKnownBrowsers();

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private BrowserDefinition? _fallbackBrowser;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _closeToTray = true;

    [ObservableProperty]
    private string _status = "Ready";

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

    public SettingsViewModel(
        IProtocolService protocolService,
        IDefaultBrowserService defaultBrowserService,
        ISettingsService settingsService)
    {
        _protocolService = protocolService;
        _defaultBrowserService = defaultBrowserService;
        _settingsService = settingsService;

        var app = System.Windows.Application.Current as App;
        _isDarkTheme = app?.CurrentThemeMode == AppThemeMode.Dark;

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
}