# AutoBrowser - URL Router App

## Overview
WPF .NET 10 app for routing URLs to different browsers based on configurable rules.

## Project Path
`C:\Projexts\Space\AutoBrowser`

## Structure
```
src/
├── AutoBrowser/                     — Main WPF app
│   ├── Models/                      — RoutingRule, BrowserDefinition, AppSettings
│   ├── Services/                    — RuleService, SettingsService, ProtocolService,
│   │                                  DefaultBrowserService, UrlInterceptorService, UpdateService + ReleaseInfo
│   ├── Views/                       — RuleEditorView, RuleTesterView
│   ├── ViewModels/                  — MainViewModel, RelayCommand
│   ├── MainWindow.xaml/.cs          — UI + tray icon
│   ├── App.xaml/.cs                 — Single-instance, theme, CLI URL dispatch
│   └── app.ico
├── AutoUpdater/                     — AOT console EXE for update file swap + relaunch
│   └── Program.cs
├── AutoBrowser.slnx                 — Solution file (SLNX format)
└── .github/workflows/release.yml    — Build + publish + GitHub release
```

## Key Features
- URL routing rules with regex pattern matching, ordered by priority
- Detects browsers via common install paths, App Paths registry, RegisteredApplications
- System tray with show/exit
- `autobrowser://` protocol handler registration
- Default browser registration (appears in Windows Default Apps)
- Falls back to saved default browser when no rule matches (avoids infinite loops)
- Portable: all data in `Data/` folder next to exe
- Auto-update: checks GitHub releases on startup + manual button
- CI: pre-release on branch push, stable on tag push; revision from UTC time-of-day

## Developer Tools
- **Serena MCP** — semantic code analysis (symbol search, references, refactoring) via `opencode.json`
- **AGENTS.md** — instructions for AI agents working on this project

## Important Build/Run Commands
- Restore: `dotnet restore AutoBrowser.slnx`
- Build (solution): `dotnet build AutoBrowser.slnx`
- Verify while app runs (staging, no kill): `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging`
- Full build: `dotnet build src\AutoBrowser\AutoBrowser.csproj`
- Run: `dotnet run --project src\AutoBrowser\AutoBrowser.csproj` or run EXE directly

## Registry Registration
- Protocol handler: `HKCU\Software\Classes\autobrowser`
- Default browser: `HKCU\Software\RegisteredApplications` + capabilities
- Default icon: exe icon index 0
