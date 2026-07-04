# AutoBrowser — UI & Behavior

## Theme Toggle
- **Control**: `ui:ToggleSwitch` in toolbar, bound to `MainViewModel.IsDarkTheme` (bool)
- **Enum**: `AppThemeMode` — `Light` (0), `Dark` (1) — `System` removed
- **Default**: `Light`
- **Flow**: `App.OnStartup` → loads settings → `ApplyTheme(mode)` → MainViewModel reads `App.CurrentThemeMode` to init toggle state
- **Toggle setter** → `App.ApplyTheme(Dark|Light)` → `ApplicationThemeManager.Apply()` + persists to `Data/settings.json`
- **`App.CurrentThemeMode`** property prevents desync from stale settings values

## System Tray
- `NotifyIcon` with app icon + context menu (Show Window, Exit)
- Minimize → hides to tray (`Window_StateChanged`)
- Close → minimizes to tray (cancel `Closing` event)
- Only Exit menu item truly terminates
- `SaveRules()` called on close

## Important Build/Run Commands
- **Verify compilation (app running)**: `dotnet build AutoBrowser\AutoBrowser.csproj -o AutoBrowser\bin\Debug\net10.0-windows_staging` — does NOT kill the running app, output goes to `_staging` folder
- **Full build (kills app)**: `dotnet build` — only when intentionally overwriting the running binary
- **Run**: `dotnet run` or run exe directly
- **Post-change ritual**: verify with `_staging` first (no kill), then exit app and run fresh copy