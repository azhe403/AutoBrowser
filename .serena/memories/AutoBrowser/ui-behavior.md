# AutoBrowser — UI & Behavior

## Theme Toggle
- **Control**: `ui:ToggleSwitch` in toolbar, bound to `MainViewModel.IsDarkTheme` (bool)
- **Enum**: `AppThemeMode` — `Light` (0), `Dark` (1) — `System` removed
- **Default**: `Light`
- **Flow**: `App.OnStartup` → loads settings → `ApplyTheme(mode)` → MainViewModel reads `App.CurrentThemeMode` to init toggle state
- **Toggle setter** → `App.ApplyTheme(Dark|Light)` → `ApplicationThemeManager.Apply()` + loads existing settings → sets `ThemeMode` → persists to `Data/settings.json`
- **`App.CurrentThemeMode`** property prevents desync from stale settings values

## System Tray
- `NotifyIcon` with app icon + context menu (Show Window, Exit)
- Minimize → hides to tray (`Window_StateChanged`) — when `MinimizeToTray` enabled
- Close → minimizes to tray (cancel `Closing` event) — when `CloseToTray` enabled
- Only Exit menu item truly terminates
- `SaveRules()` called on close
- Both options persist to `Data/settings.json`

## Update Check
- **Button**: "Check Update" in toolbar, bound to `CheckForUpdateCommand`, disabled while checking/downloading
- **Auto-check**: `_ = CheckForUpdateSilentAsync()` runs on startup, silently ignores no-update/offline
- **Dialog**: `Wpf.Ui.Controls.MessageBox` (500px wide) with Yes/No — no third Cancel button
- **Silent flow**: fires `ShowUpdateDialogAsync` only when newer version found
- **Manual flow**: shows status for checking/up-to-date/failed, then delegates to `ShowUpdateDialogAsync`

$1
- **Verify compilation (app running)**: `dotnet build AutoBrowser\AutoBrowser.csproj -o AutoBrowser\bin\Debug\net10.0-windows_staging` — does NOT kill the running app, output goes to `_staging` folder
- **Full build (kills app)**: `dotnet build` — only when intentionally overwriting the running binary
- **Run**: `dotnet run` or run exe directly
- **Post-change ritual**: verify with `_staging` first (no kill), then exit app and run fresh copy