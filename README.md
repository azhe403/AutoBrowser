# AutoBrowser

A Windows desktop app that routes URLs to user-configured browsers based on regex rules. Registers as the `autobrowser://` protocol handler and optionally as the system default browser.

## Features

- **URL Routing** — Define rules to route URLs to specific browsers (e.g., work links → Chrome, personal → Firefox)
- **Regex/Substring Patterns** — Patterns support full regex, with automatic fallback to substring matching
- **Protocol Handler** — Registers `autobrowser://` protocol so links open directly in AutoBrowser
- **Default Browser Mode** — Optionally register as the system default browser to intercept all HTTP(S) links
- **Infinite-Loop Protection** — Unmatched URLs launch the previous default browser directly by EXE path, not via shell association
- **Fallback Browser** — Configurable fallback browser for unmatched URLs
- **Auto-Update** — Check for updates from GitHub releases, download and apply with automatic backup/restore
- **System Tray** — Minimize to tray and/or close to tray (independently configurable)
- **Dark Mode** — Toggle Dark/Light theme
- **Portable** — All data stored in `Data/` folder next to the executable

## Requirements

- Windows 10+
- .NET 10 Runtime (for framework-dependent build)

## Installation

Download from [Releases](https://github.com/azhe403/AutoBrowser/releases):

| Package | Description |
|---------|-------------|
| `AutoBrowser-*-framework-dependent.zip` | Requires .NET 10 runtime |
| `AutoBrowser-*-self-contained.zip` | Standalone, no runtime needed |

Extract and run `AutoBrowser.exe`.

> **Portable**: If you move the folder to another location, just re-tick both checkboxes in the toolbar (`autobrowser:// protocol` and `Registered as default browser`) to update the registry with the new path. Old entries are automatically cleaned up.

## Usage

1. Launch AutoBrowser
2. Add routing rules (URL pattern → browser)
3. Enable `autobrowser://` protocol registration to handle custom links
4. Optionally register as default browser to intercept all HTTP(S) links
5. Use **Test URL** to verify routing

## Build from Source

```bash
dotnet build src\AutoBrowser\AutoBrowser.csproj
```

Verify compilation without killing a running instance:
```bash
dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging
```

## Tech Stack

- **.NET 10** (Windows, WPF)
- **WPF UI** — Fluent Design controls and theming
- **CommunityToolkit.Mvvm** — MVVM with source generators (`ObservableObject`, `RelayCommand`)
- **Serilog** — Structured logging
- **AutoUpdater** — Standalone AOT console EXE for file swap + relaunch

## License

MIT
