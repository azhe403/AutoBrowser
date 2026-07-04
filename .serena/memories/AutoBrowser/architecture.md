# AutoBrowser Architecture & Flow

## Overview

AutoBrowser is a WPF desktop app for Windows that registers as a URL protocol handler (`autobrowser://`) and optionally as the system default browser, then routes URLs to user-configured browsers based on regex rules. It lives in the system tray and minimizes on close.

## Project Layout

```
AutoBrowser/
├── app.ico                      # Multi-res icon (16-256px, routing arrow on blue circle)
├── App.xaml / App.xaml.cs       # App entry: single-instance via Mutex, CLI URL dispatch
├── MainWindow.xaml / .cs        # Main UI: toolbar, rule ListView, status bar, tray icon
├── AutoBrowser.csproj           # SDK-style, net10.0-windows, WPF + WinForms
├── Models/
│   ├── RoutingRule.cs           # Rule model (Name, Pattern, BrowserPath, Priority, IsEnabled, IsMatch)
│   └── BrowserDefinition.cs     # Browser detection: common paths, App Paths registry, RegisteredApplications, %LOCALAPPDATA% scan
├── Services/
│   ├── IConfigurationService.cs # Interface for config/protocol/registration operations
│   ├── ConfigurationService.cs  # JSON persistence, protocol & default-browser registry ops
│   └── UrlInterceptorService.cs # URL matching engine + browser launch
└── ViewModels/
    ├── MainViewModel.cs         # MVVM commands for CRUD, reorder, toggle, registration checkboxes, test URL
    ├── RelayCommand.cs          # ICommand implementation
    ├── RuleDialog.xaml / .cs    # Add/Edit rule dialog with detected-browsers dropdown
    └── InputDialog.xaml / .cs   # Simple text input dialog (for test URL)
```

## Data Flow

```
1. URL arrives (autobrowser:// or system default click):
   ┌──────────────────────────────────────────────────┐
   │  App.OnStartup (command-line args)               │
   │  or MainWindow.OnLoaded (second instance args)   │
   └──────────────┬───────────────────────────────────┘
                  │ url
                  ▼
   ┌──────────────────────────────┐
   │  UrlInterceptorService       │
   │  .TryRoute(url)              │
   │  1. StripProtocolPrefix()    │
   │  2. LoadRules() from JSON    │
   │  3. Filter IsEnabled=true    │
   │  4. Sort by Priority ASC     │
   │  5. First match → Launch()   │
   │  6. No match → fallback:     │
   │     a. Saved default browser │
   │        (from default_browser.txt) → Launch()
   │     b. Else → ShellExecute   │
   └──────────────────────────────┘
```

## Browser Detection Strategy (BrowserDefinition.GetKnownBrowsers)

Detection runs in this order (duplicates skipped by EXE path):

1. **Hardcoded known paths** — common install locations for Edge, Chrome (stable/beta/dev/canary), Firefox (stable/dev/nightly), Opera (stable/GX), Brave (stable/beta/nightly), Vivaldi — checking both `ProgramFiles`, `ProgramFiles(x86)`, and `%LOCALAPPDATA%` for per-user installs.

2. **%LOCALAPPDATA% directory scan** — enumerates all subdirectories looking for known browser EXEs (chrome.exe, msedge.exe, firefox.exe, brave.exe, vivaldi.exe, opera.exe, launcher.exe, tor.exe, waterfox.exe, librewolf.exe, palemoon.exe, iridium.exe, epic.exe). This catches non-standard/channel installs.

3. **App Paths registry** — reads `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths` (both 64-bit and 32-bit views) and `HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths`. Filters to known browser names.

4. **System default browser** — reads `HKCU\UserChoice` ProgId to discover the current default browser. Inserted at index 0 with name "default".

5. **RegisteredApplications** — reads `HKLM\SOFTWARE\RegisteredApplications` and `HKCU\Software\RegisteredApplications`, follows each app's capability URL associations to find browser EXEs.

## JSON Persistence

- **File**: `Data/rules.json` (next to AutoBrowser.exe)
- **Format**: JSON array of `RoutingRule` objects, `WriteIndented = true`
- **Save triggers**: Add, Edit, Delete, Move Up/Down, Toggle Enable/Disable, and Window close
- **Load auto-merge**: default rules are merged with saved rules by matching on `Name` (case-insensitive). New defaults not already present in saved rules are appended. This allows new default rules to appear after app updates without overwriting user modifications.

## Registry Registration

### Protocol Handler (`autobrowser://`)
- **Key**: `HKCU\Software\Classes\autobrowser`
- Toggle checkbox in toolbar — no admin needed, per-user only
- Values: (Default)=`URL:AutoBrowser Protocol`, `URL Protocol`=`""`
- Command: `shell\open\command` = `"path\to\AutoBrowser.exe" "%1"`

### Default Browser Registration
- **Key**: `HKCU\Software\RegisteredApplications` → `AutoBrowser` = `Software\AutoBrowser\Capabilities`
- Creates `HKCU\Software\AutoBrowser\Capabilities` with URLAssociations for http/https
- Creates `HKCU\Software\Classes\AutoBrowserLink` as the ProgId with shell/open/command
- **Before** registering, saves the current default browser path to `Data/default_browser.txt`
- **On unregister**, deletes the saved path
- Toggle checkbox in toolbar — user must also manually select AutoBrowser in Settings > Default Apps

## Infinite-Loop Protection

When AutoBrowser is the default browser and receives a URL that doesn't match any rule:
1. It reads `Data/default_browser.txt` (saved before self-registration)
2. Launches the saved browser **directly by EXE path** (not via shell association)
3. This avoids the OS calling back to AutoBrowser

If no saved browser path exists, falls back to `ShellExecute` (the OS default behavior).

## URL Pattern Matching (RoutingRule.IsMatch)

1. Tries `Regex.IsMatch(url, UrlPattern, IgnoreCase | CultureInvariant)`
2. On `RegexParseException`, falls back to `url.Contains(UrlPattern, OrdinalIgnoreCase)`

This means patterns can be either regex or plain substrings.

## Single-Instance Enforcement (App.xaml.cs)

- Uses a named `Mutex` (`AutoBrowser-SingleInstance`)
- Second instance shows a message box and immediately shuts down
- The first instance handles the URL from its command line in `MainWindow.OnLoaded`

## System Tray Behavior

- `NotifyIcon` with extracted app icon + context menu (Show Window, Exit)
- Minimizing the window hides it to the tray (Window_StateChanged)
- Closing the window minimizes to tray (cancel Closing event)
- Only the Exit menu item truly terminates the app
- SaveRules is called on close to ensure latest state is persisted

## Important Build/Run Commands

- Verify compilation while app is running: `dotnet build AutoBrowser\AutoBrowser.csproj -o AutoBrowser\bin\Debug\net10.0-windows_staging`
- Full build (kills running app): `dotnet build`
- Run: `dotnet run` or run exe directly
- Must run and close after every change to verify runtime behavior

## Key Design Decisions

- **Portable**: All data stored in `Data/` folder next to EXE, not %APPDATA%
- **Per-user registry**: No admin elevation required for registration
- **No admin** for default browser: Uses `HKCU\RegisteredApplications` approach (modern Windows allows this, user confirms in Settings)
- **Auto-merge defaults**: New default rules (by `Name`) are merged in on load, never overwrite existing user rules
- **Fallback safety**: Always prefer saved-default-browser direct launch over shell association to avoid loops
