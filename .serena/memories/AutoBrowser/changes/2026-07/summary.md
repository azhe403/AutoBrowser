# Changes 2026-07

## Auto-Update System & AutoUpdater
- Created separate `AutoUpdater` project (net10.0 console application, published as single-file, uses Serilog).
- Implemented `UpdateService` to query GitHub releases, download and trigger the updater.
- Added path arguments parsing and backup/rollback mechanisms.

## Single-Instance & System Tray
- Integrated mutex-based single-instance verification.
- Implemented IPC using Named Pipes to pass launch URLs to the running instance.
- Added System Tray integration with independent minimize/close behaviors, notifications, and restoring from tray.

## UI Modernization & MVVM Refactoring
- Split monolithic `MainWindow` into modular UserControls under `Views/` (`ToolbarView`, `RulesListView`, `FooterView`, `StatusControl`).
- Implemented Dependency Injection (`Microsoft.Extensions.DependencyInjection`).
- Migrated to `ui:NavigationView` with `HomePage`, `SettingsPage`, and `AboutPage`.
- Extracted and cleaned up C# code-behind files into focused partial classes (`App.Tray.cs`, `App.Routing.cs`, `App.Registration.cs`, `App.Window.cs`).
- Integrated WPF UI `InfoBar` inside `StatusControl` for non-intrusive status updates.

## Bug Fixes & Refactoring
- Fixed path change detection / re-register prompt logic so it ignores CLI wrapper (`dotnet.exe`) and checks actual paths.
- Fixed theme styling issues (applied theme on Window loaded; fixed missing TextBlock dynamic foregrounds under dark mode).
- Cleaned up fully-qualified names globally, centralizing aliases in `GlobalUsings.cs`.
- Resolved double-click edit bubble issues and dynamic command enabling (`CanExecute`).
- Improved startup sequence to avoid overlapping dialog boxes.

## Tests & CI
- Added FlaUI integration UI test for deleting rules (`MainWindow_DeleteRule`).
- Formulated CI revision calculation based on UTC seconds.
