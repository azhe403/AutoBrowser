# AutoBrowser — Services & Persistence

## Browser Detection (BrowserDefinition.GetKnownBrowsers)
Order (duplicates skipped by EXE path):
1. **Hardcoded paths** — Edge, Chrome (stable/beta/dev/canary), Firefox (stable/dev/nightly), Opera (stable/GX), Brave (stable/beta/nightly), Vivaldi — in ProgramFiles, ProgramFiles(x86), %LOCALAPPDATA%
2. **%LOCALAPPDATA% scan** — enumerates subdirs for known browser EXEs
3. **App Paths registry** — HKLM + HKCU `\Software\Microsoft\Windows\CurrentVersion\App Paths`
4. **System default browser** — `HKCU\UserChoice` ProgId, inserted at index 0 as "default"
5. **RegisteredApplications** — HKLM + HKCU `\Software\RegisteredApplications`

## Update Check (UpdateService)

- `CheckForUpdateAsync()` queries GitHub releases (`/releases?per_page=10`) — includes pre-releases
- Compares parsed version to entry assembly version
- Returns `ReleaseInfo` with `IsNewer`, `Version`, download URL
- `DownloadAndUpdateAsync()` — downloads ZIP, extracts to temp, finds `AutoUpdater.exe` in app dir, copies to runner folder, launches updater, shuts down main app
- 404 (no releases) handled gracefully
- Asset selection: picks ZIP matching "self-contained" or "framework-dependent" based on `coreclr.dll` presence

## AutoUpdater (src/AutoUpdater/)
- **Single-file** (`PublishSingleFile=true`, `SelfContained=false`) console helper EXE (`AutoUpdater.exe`)
- Waits for main process to exit (2 min timeout)
- Backs up old files, copies new ones with SHA256 validation + 5 retries
- Rollback on failure, relaunches main app on success
- `asInvoker` manifest (no admin)
- Post-build MSBuild target copies `AutoUpdater*` from updater output to main app output

## Single Instance (SingleInstanceService)
- Named pipe IPC (`System.IO.Pipes`) for single-instance signaling
- `SingleInstanceService` manages pipe server in background `Task.Run` loop
- Protocol: `"SHOW"` or `"SHOW|<url>"` — brings existing window to front
- `WindowForegroundHelper` uses Win32 P/Invoke (`SetForegroundWindow`, `ShowWindow`)
- `MainWindow.ActivateFromTray(url)` restores window and processes forwarded URL

## Persistence
### Rules (`Data/rules.json`)
- JSON array of `RoutingRule`, `WriteIndented = true`
- Save triggers: Add, Edit, Delete, Move Up/Down, Toggle Enable/Disable, Window close
- **Auto-merge**: Default rules merged with saved by `Name` (case-insensitive). New defaults not present in saved are appended. Never overwrites user modifications.

### Settings (`Data/settings.json`)
- `AppSettings` object: `ThemeMode`, `LastTestUrl`, `FallbackBrowserPath`, `MinimizeToTray`, `CloseToTray`
- Save triggers: Theme toggle, fallback browser change, tray toggle changes

## Registry Registration
### Protocol Handler (`autobrowser://`)
- Key: `HKCU\Software\Classes\autobrowser`
- (Default) = `URL:AutoBrowser Protocol`, `URL Protocol` = `""`
- Command: `shell\open\command` = `"path\to\AutoBrowser.exe" "%1"`
- Toggle checkbox in toolbar, no admin needed
- **Path check**: `GetRegisteredPath()` reads current registered exe path for re-register prompt

### Default Browser
- `HKCU\Software\RegisteredApplications` → `AutoBrowser` = `Software\AutoBrowser\Capabilities`
- Creates ProgId `AutoBrowserLink` with shell/open/command
- Before registering: saves current default EXE to `Data/default_browser.txt`
- On unregister: deletes that file
- Silent toggle — no message box shown (user selects AutoBrowser in Settings > Default Apps if they want)
- **Path check**: `GetRegisteredPath()` reads current registered exe path for re-register prompt

## Re-Register Prompt
- On startup, `CheckAndPromptReRegister()` compares registered paths with `Environment.ProcessPath`
- If path differs (app was moved), shows MessageBox with old/new paths and Yes/No to re-register
- On Yes: unregisters and re-registers both protocol and default browser
- On No: logs decline, continues normally