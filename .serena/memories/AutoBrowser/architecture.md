# AutoBrowser — Architecture

## Overview
WPF desktop app for Windows. Registers as `autobrowser://` protocol handler and optional default browser, then routes URLs to user-configured browsers by regex rules. Lives in system tray, minimizes on close (minimize-to-tray and close-to-tray independently configurable).

## Dependency Injection (DI) & MVVM
- Migrated to a Dependency Injection container using `Microsoft.Extensions.DependencyInjection`.
- Core services, `MainWindow`, and `MainViewModel` are registered on startup in `App.xaml.cs`.
- `MainWindow` has zero code-behind, resolving the `MainViewModel` from the global `ServiceProvider`.
- `RoutingRule` properties use CommunityToolkit.Mvvm source generators and auto-saves to rule service on property change events.

## AutoUpdater (src/AutoUpdater/)
- Standalone single-file console EXE for file swap + relaunch (no runtime dependency)
- Build dependency via `ReferenceOutputAssembly="false"` in main csproj
- Post-build MSBuild target copies all `AutoUpdater*` files into main app output

## Project Structure
```
AutoBrowser/
├── app.ico                      # Multi-res icon
├── App.xaml / App.xaml.cs       # Entry: Configure DI services, Single-instance mutex, CLI URL dispatch, ApplyTheme(), Window lifecycle events (Loaded, Closing, StateChanged), Tray Icon management
├── MainWindow.xaml / .cs        # UI: Layout shell with sliced components. Zero code-behind (boilerplate constructor only)
├── AutoBrowser.csproj           # SDK-style, net10.0-windows, WPF + WinForms + WPF-UI + Microsoft.Extensions.DependencyInjection
├── Models/
│   ├── AppSettings.cs           # ThemeMode (Light/Dark)
│   ├── AppThemeMode.cs          # Light=0, Dark=1 (System removed)
│   ├── RoutingRule.cs           # Name, Pattern, BrowserPath, Sequence, IsEnabled, IsMatch (inherits from ObservableObject)
│   └── BrowserDefinition.cs     # Browser detection logic
├── Services/
│   ├── IRuleService.cs / RuleService.cs           # Rules JSON persistence + auto-merge
│   ├── ISettingsService.cs / SettingsService.cs   # Settings JSON persistence
│   ├── IProtocolService.cs / ProtocolService.cs   # autobrowser:// registry ops
│   ├── IDefaultBrowserService.cs / DefaultBrowserService.cs  # Default browser reg
│   ├── SingleInstanceService.cs                   # Named pipe IPC for single-instance
│   ├── UrlInterceptorService.cs                   # URL matching + browser launch
│   └── UpdateService.cs + ReleaseInfo record      # GitHub release check, download, update install
├── Views/
│   ├── ToolbarView.xaml / .cs      # Sliced control: Add/Edit/Delete, Up/Down, Theme switch, Update check, URL test
│   ├── RulesListView.xaml / .cs    # Sliced control: ListView of Rules with double-click edit, protocol checkboxes
│   ├── FooterView.xaml / .cs       # Sliced control: Tray settings toggles and fallback browser combo box
│   ├── StatusControl.xaml / .cs    # Sliced control: Status bar info display
│   ├── RuleEditorView.xaml / .cs   # Add/Edit rule with browser dropdown
│   └── RuleTesterView.xaml / .cs   # Test URL input dialog
└── ViewModels/
    └── MainViewModel.cs         # ViewModel containing commands and state properties for views.
```

## Key Design Decisions
- **Dependency Injection**: Services and views resolved from centralized `ServiceProvider` in `App.xaml.cs`.
- **Portable**: All data in `Data/` folder next to EXE, not %APPDATA%
- **Per-user registry**: No admin elevation needed
- **Default browser via HKCU**: `RegisteredApplications` approach (user confirms in Settings)
- **Auto-merge rules**: Default rules merged by `Name`, never overwrites user edits
- **Infinite-loop protection**: Always launch unmatched URLs via saved-default-browser EXE path, not shell association