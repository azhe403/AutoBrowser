# AutoBrowser — Architecture

## Overview
WPF desktop app for Windows. Registers as `autobrowser://` protocol handler and optional default browser, then routes URLs to user-configured browsers by regex rules. Lives in system tray, minimizes on close.

## Project Layout
```
AutoBrowser/
├── app.ico                      # Multi-res icon
├── App.xaml / App.xaml.cs       # Entry: single-instance mutex, CLI URL dispatch, ApplyTheme(), CurrentThemeMode
├── MainWindow.xaml / .cs        # UI: toolbar (ToggleSwitch dark mode), rule ListView, status bar, tray icon
├── AutoBrowser.csproj           # SDK-style, net10.0-windows, WPF + WinForms + WPF-UI
├── Models/
│   ├── AppSettings.cs           # ThemeMode (Light/Dark)
│   ├── AppThemeMode.cs          # Light=0, Dark=1 (System removed)
│   ├── RoutingRule.cs           # Name, Pattern, BrowserPath, Priority, IsEnabled, IsMatch
│   └── BrowserDefinition.cs     # Browser detection logic
├── Services/
│   ├── IRuleService.cs / RuleService.cs           # Rules JSON persistence + auto-merge
│   ├── ISettingsService.cs / SettingsService.cs   # Settings JSON persistence
│   ├── IProtocolService.cs / ProtocolService.cs   # autobrowser:// registry ops
│   ├── IDefaultBrowserService.cs / DefaultBrowserService.cs  # Default browser reg
│   └── UrlInterceptorService.cs                   # URL matching + browser launch
└── ViewModels/
    ├── MainViewModel.cs         # CRUD, reorder, toggle, reg checkboxes, test URL, IsDarkTheme
    ├── RelayCommand.cs          # ICommand impl
    ├── RuleDialog.xaml / .cs    # Add/Edit rule with browser dropdown
    └── InputDialog.xaml / .cs   # Test URL input dialog
```

## Key Design Decisions
- **Portable**: All data in `Data/` folder next to EXE, not %APPDATA%
- **Per-user registry**: No admin elevation needed
- **Default browser via HKCU**: `RegisteredApplications` approach (user confirms in Settings)
- **Auto-merge rules**: Default rules merged by `Name`, never overwrites user edits
- **Infinite-loop protection**: Always launch unmatched URLs via saved-default-browser EXE path, not shell association