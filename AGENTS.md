# AGENTS.md — Instructions for AI Agents

## Project Overview

AutoBrowser is a WPF desktop app for Windows that routes URLs to user-configured browsers. It uses .NET 10, WPF UI library, and follows MVVM pattern.

## Key Conventions

- **Language**: C# (.NET 10), XAML
- **Pattern**: MVVM with `INotifyPropertyChanged`
- **UI Library**: WPF UI (`xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`)
- **Data Storage**: JSON files in `Data/` folder next to EXE (portable)
- **Registry**: HKCU only, no admin elevation needed
- **Logging**: Serilog with structured logging using message templates
- **Code Style**: Use `using` directives, not fully qualified names; aliases only in `GlobalUsings.cs`

### Logging Level Convention

When adding logs to methods, follow this level hierarchy:

1. **Information** - Method entry and exit points (first and last logs)
   - Method calls with key parameters
   - Completion status with results
   - Example: `Log.Information("TryRoute called with URL: {Url}", url)`

2. **Debug** - Middle steps with braces/parameters
   - Variable values with placeholders
   - Conditional checks
   - Example: `Log.Debug("Loaded {Count} enabled rules", rules.Count)`

3. **Verbose** - Detailed internal steps (finest level)
   - Fine-grained operations
   - Individual loop iterations
   - Example: `Log.Verbose("Checking rule '{RuleName}'", rule.Name)`

4. **Error** - Exception handling
   - Example: `Log.Error(ex, "LaunchBrowser failed")`

**Note**: Serilog uses `Verbose` instead of `Trace` (equivalent level below Debug).

## Project Structure

```
src/AutoBrowser/
├── App.xaml / .cs           # Entry point, theme apply, single-instance mutex, pipe server
├── MainWindow.xaml / .cs    # Main UI, tray icon, re-register prompt
├── AssemblyInfo.cs          # Assembly metadata
├── Helpers/
│   └── WindowForegroundHelper.cs  # Win32 P/Invoke for foreground window
├── Models/
│   ├── AppSettings.cs       # Persisted settings (ThemeMode, LastUpdateCheckTime)
│   ├── AppThemeMode.cs      # Light/Dark enum
│   ├── RoutingRule.cs       # Rule model
│   └── BrowserDefinition.cs # Browser detection from filesystem/registry
├── Services/
│   ├── IRuleService.cs      # Rule service interface
│   ├── RuleService.cs       # Rule JSON persistence + auto-merge
│   ├── ISettingsService.cs  # Settings service interface
│   ├── SettingsService.cs   # Settings JSON persistence
│   ├── IProtocolService.cs  # Protocol service interface
│   ├── ProtocolService.cs   # autobrowser:// registry ops + path check
│   ├── IDefaultBrowserService.cs  # Default browser interface
│   ├── DefaultBrowserService.cs   # Default browser registration + path check
│   ├── IUpdateService.cs    # (if present)
│   ├── UpdateService.cs     # Auto-update from GitHub releases
│   └── UrlInterceptorService.cs  # URL matching + browser launch
├── ViewModels/
│   └── MainViewModel.cs     # Commands, IsDarkTheme, Status, update throttling
└── Views/
    ├── RuleEditorView.xaml / .cs    # Add/Edit rule dialog
    └── RuleTesterView.xaml / .cs    # Test URL input dialog
```

## Serena Memories

Relevant memories:
- `AutoBrowser/architecture` — high-level design and layout
- `AutoBrowser/flow` — URL routing flow and pattern matching
- `AutoBrowser/services` — browser detection, persistence, registry
- `AutoBrowser/ui-behavior` — theme toggle, tray, build commands
- `git/commit-strategy` — split large changes, 3-5min delay between commits
- `workflow/sync-memory` — sync project state to memory after changes

## First Step — ALWAYS

Before doing ANY task, read the relevant Serena memories to understand current project state:
- `AutoBrowser/architecture` — high-level design and layout
- `AutoBrowser/flow` — URL routing flow and pattern matching
- `AutoBrowser/services` — browser detection, persistence, registry
- `AutoBrowser/ui-behavior` — theme toggle, tray, build commands
- `AutoBrowser/changes/YYYY-MM/YYYY-MM-DD` — recent changes (check latest date)

This prevents stale assumptions and keeps context fresh.

## Memory Sync — ALWAYS

After every change (code, config, files), immediately update the relevant Serena memory. This keeps context fresh for fast future action.

See `workflow/sync-memory` for details. Use `AutoBrowser/changes/YYYY-MM/YYYY-MM-DD` for daily changelog.

## Build & Run

- Verify while app runs: `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging` (no kill)
- Full build: `dotnet build src\AutoBrowser\AutoBrowser.csproj`
- Run: `dotnet run --project src\AutoBrowser\AutoBrowser.csproj` or run EXE directly

## Post-Change Verification — ALWAYS

After **any** code or XAML change, run this sequence to confirm the app starts without crash:

0. Tests: `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj`
1. Build: `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging`
2. Launch, wait 15s, close:
   ```powershell
   $proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 20; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
   ```
3. Check the log at `bin\staging\Logs/` for any `[ERR]` entries.
4. **Test re-register prompt** — simulate moved app by changing registry, launch, verify prompt, restore:
   ```powershell
   # Read current path
   $regPath = "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command"
   $original = (Get-ItemProperty -Path $regPath -Name "(default)")."(default)"

   # Fake old path to trigger prompt
   Set-ItemProperty -Path $regPath -Name "(default)" -Value '"C:\OldLocation\AutoBrowser.exe" "%1"'

   # Launch and capture — user should see the dialog
   $proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 20; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue

   # Restore original path
   Set-ItemProperty -Path $regPath -Name "(default)" -Value $original

   # Verify log shows re-register was triggered
   Get-ChildItem "bin\staging\Logs\" -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { Get-Content $_.FullName | Select-String "App path has changed" }
   ```

If tests fail or the app fails to launch or throws an error, fix it before proceeding.

**Note**: Use WPF UI `MessageBox` with `ShowDialogAsync()` instead of `System.Windows.MessageBox` to prevent title bar movement issues.

## Git Rules — CRITICAL

- **NEVER commit or push without explicit user permission** — this is the #1 rule.
  - Under no circumstances should you run `git commit`, `git push`, or `git add` unless the user has explicitly requested it in their message. If you need to check state, only use read-only commands like `git status` or `git diff`.
- Split large changes into multiple logical commits
- Add 3-5 minute random delay between commits
