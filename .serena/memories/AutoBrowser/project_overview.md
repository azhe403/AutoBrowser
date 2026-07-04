# AutoBrowser - URL Router App

## Overview
WPF .NET 10 app for routing URLs to different browsers based on configurable rules.

## Project Path
`C:\Projexts\Space\AutoBrowser`

## Structure
```
AutoBrowser/
├── Models/
│   ├── RoutingRule.cs          — Rule: Name, UrlPattern (regex), BrowserPath, BrowserArguments, IsEnabled, Priority
│   └── BrowserDefinition.cs    — Browser detection (paths + registry), multi-resolution scanning
├── Services/
│   ├── IConfigurationService.cs / ConfigurationService.cs — JSON config in `Data/rules.json`, protocol/default browser registration
│   └── UrlInterceptorService.cs — URL matching, browser launching, fallback to saved default browser
├── ViewModels/
│   ├── MainViewModel.cs        — Rule CRUD, toggle protocol/default browser registration, test URL
│   ├── RelayCommand.cs
│   ├── RuleDialog.xaml/.cs     — Add/edit rule with detected browsers dropdown + manual path
│   └── InputDialog.xaml/.cs    — Simple input prompt
├── MainWindow.xaml/.cs         — Rule list UI, system tray icon
├── App.xaml/.cs                — Single-instance, protocol URL handling at startup
└── app.ico                     — Multi-res icon (16-256px, routing arrow)
```

## Key Features
- URL routing rules with regex pattern matching, ordered by priority
- Detects browsers via common install paths, App Paths registry, RegisteredApplications (both HKLM and HKCU)
- Checks per-user installs (`%LOCALAPPDATA%`) for browsers and release channels (Chrome Beta/Canary, Firefox Dev/Nightly, etc.)
- System tray with show/exit
- `autobrowser://` protocol handler registration
- Default browser registration (appears in Windows Default Apps)
- Falls back to saved default browser when no rule matches (avoids infinite loops)
- Portable: all data in `Data/` folder next to exe

## Important Build/Run Commands
- Verify compilation while app is running: `dotnet build AutoBrowser\AutoBrowser.csproj -o AutoBrowser\bin\Debug\net10.0-windows_staging`
- Full build (kills running app): `dotnet build`
- Run: `dotnet run` or run exe directly
- Must run and close after every change to verify runtime behavior


- Build: `dotnet build` (auto-kills any running AutoBrowser.exe before compile)
- Run: `dotnet run` or run exe directly
- Must run and close after every change to verify runtime behavior


- Build: `dotnet build` (slnx at root)
- Run: `dotnet run` or run exe directly
- Must run and close after every change to verify runtime behavior

## Registry Registration
- Protocol handler: `HKCU\Software\Classes\autobrowser`
- Default browser: `HKCU\Software\RegisteredApplications` + capabilities
- Default icon: exe icon index 0
