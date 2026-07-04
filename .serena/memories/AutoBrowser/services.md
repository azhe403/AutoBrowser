# AutoBrowser — Services & Persistence

## Browser Detection (BrowserDefinition.GetKnownBrowsers)
Order (duplicates skipped by EXE path):
1. **Hardcoded paths** — Edge, Chrome (stable/beta/dev/canary), Firefox (stable/dev/nightly), Opera (stable/GX), Brave (stable/beta/nightly), Vivaldi — in ProgramFiles, ProgramFiles(x86), %LOCALAPPDATA%
2. **%LOCALAPPDATA% scan** — enumerates subdirs for known browser EXEs
3. **App Paths registry** — HKLM + HKCU `\Software\Microsoft\Windows\CurrentVersion\App Paths`
4. **System default browser** — `HKCU\UserChoice` ProgId, inserted at index 0 as "default"
5. **RegisteredApplications** — HKLM + HKCU `\Software\RegisteredApplications`

## JSON Persistence
### Rules (`Data/rules.json`)
- JSON array of `RoutingRule`, `WriteIndented = true`
- Save triggers: Add, Edit, Delete, Move Up/Down, Toggle Enable/Disable, Window close
- **Auto-merge**: Default rules merged with saved by `Name` (case-insensitive). New defaults not present in saved are appended. Never overwrites user modifications.

### Settings (`Data/settings.json`)
- Single `AppSettings` object (`ThemeMode`)
- Save triggers: Theme toggle change

## Registry Registration
### Protocol Handler (`autobrowser://`)
- Key: `HKCU\Software\Classes\autobrowser`
- (Default) = `URL:AutoBrowser Protocol`, `URL Protocol` = `""`
- Command: `shell\open\command` = `"path\to\AutoBrowser.exe" "%1"`
- Toggle checkbox in toolbar, no admin needed

### Default Browser
- `HKCU\Software\RegisteredApplications` → `AutoBrowser` = `Software\AutoBrowser\Capabilities`
- Creates ProgId `AutoBrowserLink` with shell/open/command
- Before registering: saves current default EXE to `Data/default_browser.txt`
- On unregister: deletes that file
- Silent toggle — no message box shown (user selects AutoBrowser in Settings > Default Apps if they want)