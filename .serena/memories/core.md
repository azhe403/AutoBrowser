# AutoBrowser — Core

## Project Type
Windows WPF desktop app (.NET 10) that routes URLs to user-configured browsers via regex rules.

## Repository Layout
```
src/
├── AutoBrowser/          # Main WPF app (net10.0-windows)
│   ├── Models/           # RoutingRule, BrowserDefinition, AppSettings, AppThemeMode
│   ├── Services/         # RuleService, ProtocolService, DefaultBrowserService, SettingsService, UpdateService, UrlInterceptorService, SingleInstanceService
│   ├── ViewModels/       # MainViewModel
│   └── Views/            # MainWindow, RuleEditorView, RuleTesterView
├── AutoBrowser.Tests/    # xUnit + Moq unit tests
└── AutoUpdater/          # Standalone single-file console EXE for file swap + relaunch
```

## Key Invariants
- Portable: all data stored in `Data/` folder next to EXE
- Single-instance via named mutex + named pipe IPC
- Registers `autobrowser://` protocol handler; optionally registers as default browser
- URL routing: pattern match (regex with substring fallback) → launch browser by path
- Infinite-loop protection: unmatched URLs launch previous default browser directly by EXE path
- Auto-update from GitHub releases (throttled to once per hour)
- Theme persistence via `AppSettings.ThemeMode`

## Memories Index
- `mem:tech_stack` — languages, frameworks, dependencies
- `mem:conventions` — code style, naming, patterns
- `mem:suggested_commands` — build, test, run commands
- `mem:task_completion` — verification steps after changes