# AutoBrowser — UI & Behavior

## Theme Toggle
- **Control**: `ui:ToggleSwitch` in toolbar, bound to `MainViewModel.IsDarkTheme` (bool)
- **Enum**: `AppThemeMode` — `Light` (0), `Dark` (1) — `System` removed
- **Default**: `Light`
- **Flow**: `App.OnStartup` → loads settings → `ApplyTheme(mode)` → MainViewModel reads `App.CurrentThemeMode` to init toggle state
- **Toggle setter** → `App.ApplyTheme(Dark|Light)` → `ApplicationThemeManager.Apply()` + loads existing settings → sets `ThemeMode` → persists to `Data/settings.json`
- **`App.CurrentThemeMode`** property prevents desync from stale settings values

## System Tray (Managed by App.xaml.cs)
- `NotifyIcon` with app icon + context menu (Show Window, Exit)
- Minimize → hides to tray (`MainWindow_StateChanged`) — when `MinimizeToTray` enabled
- Close → minimizes to tray (cancel `MainWindow_Closing` event) — when `CloseToTray` enabled
- Only Exit menu item truly terminates
- `SaveRules()` and `SaveWindowState()` called on close
- Both options persist to `Data/settings.json`

## Update Check
- **Button**: "Check Update" in toolbar, bound to `CheckForUpdateCommand`, disabled while checking/downloading
- **Auto-check**: `_ = CheckForUpdateSilentAsync()` runs on startup, silently ignores no-update/offline
- **Throttled**: Checks once per hour via `LastUpdateCheckTime` in settings
- **Dialog**: `Wpf.Ui.Controls.MessageBox` (500px wide) with Yes/No — no third Cancel button
- **Silent flow**: fires `ShowUpdateDialogAsync` only when newer version found
- **Manual flow**: shows status for checking/up-to-date/failed, then delegates to `ShowUpdateDialogAsync`

## Single Instance (Managed by App.xaml.cs)
- Named pipe IPC (`System.IO.Pipes`) for single-instance signaling
- `SingleInstanceService` manages pipe server in background `Task.Run` loop
- Protocol: `"SHOW"` or `"SHOW|<url>"` — brings existing window to front
- `WindowForegroundHelper` uses Win32 P/Invoke (`SetForegroundWindow`, `ShowWindow`)
- `App.ActivateFromTray(url)` restores window and processes forwarded URL

## Re-Register Prompt (Managed by App.xaml.cs)
- On startup, compares registered protocol/default browser paths with `Environment.ProcessPath`
- If path differs (app was moved), shows WPF UI `MessageBox` with old/new paths
- Uses `ShowDialogAsync()` (async) with owner set to MainWindow
- Yes: unregisters and re-registers both handlers, shows notification
- No: logs decline, continues normally

### Testing Re-Register Prompt
- Save current path: `(Get-ItemProperty -Path $regPath -Name "(default)")."(default)"`
- Fake old path: `Set-ItemProperty ... "C:\OldLocation\AutoBrowser.exe"`
- Launch app → dialog should appear
- Restore original path
- Verify log shows `"App path has changed"`

## Build & Run
- **Verify compilation (app running)**: `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging` — does NOT kill the running app
- **Full build (kills app)**: `dotnet build src\AutoBrowser\AutoBrowser.csproj` — only when intentionally overwriting the running binary
- **Run**: `dotnet run --project src\AutoBrowser\AutoBrowser.csproj` or run EXE directly
- **Post-change ritual**: verify with `bin\staging` first (no kill), then exit app and run fresh copy
- **Tests**: `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj` (38 tests)