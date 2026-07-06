# AutoBrowser — Data Flow & URL Matching

## Start-up Flow
1. ViewModel constructor → loads rules/theme, binds commands
2. `_ = CheckForUpdateSilentAsync()` — fire-and-forget update check
3. Silent check: if no release or up-to-date → nothing; if newer → shows Update Available dialog
4. Manual "Check Update" button: full status feedback + same dialog

## Update Flow
```
User clicks "Check Update" / app starts
  → MainViewModel.CheckForUpdateAsync / CheckForUpdateSilentAsync
  → UpdateService.CheckForUpdateAsync() — queries /releases?per_page=10
  → No release → show "No release info" (manual) / silent (auto)
  → Up-to-date → show status (manual) / silent (auto)
  → New version → Wpf.Ui.Controls.MessageBox (Yes/No, 500px)
  → Yes → UpdateService.DownloadAndUpdateAsync()
      1. Download ZIP from GitHub
      2. Extract to temp workspace
      3. Copy AutoUpdater.exe from app dir to runner dir
      4. Launch AutoUpdater.exe (waits for main process to exit)
      5. Main app shuts down
  → AutoUpdater.exe:
      1. Wait for main process exit (2 min)
      2. Back up old files
      3. Copy new files with SHA256 + 5 retries
      4. Verify hashes
      5. On failure → restore backup
      6. Relaunch main app
```

$1
```
URL arrives (autobrowser:// or default click)
  → App.OnStartup (CLI args) or MainWindow.OnLoaded (second instance)
  → UrlInterceptorService.TryRoute(url, fallbackBrowserPath)
      1. StripProtocolPrefix()
      2. LoadRules() from Data/rules.json
      3. Filter IsEnabled=true
      4. Sort by Priority ASC
      5. First regex/substring match → Launch browser
      6. No match → fallback:
           a. Configured fallback browser (Data/settings.json → FallbackBrowserPath) → direct launch
           b. Else → return null (caller shows notification)
```

## URL Pattern Matching (RoutingRule.IsMatch)
1. Try `Regex.IsMatch(url, UrlPattern, IgnoreCase | CultureInvariant)`
2. On `RegexParseException` → `url.Contains(UrlPattern, OrdinalIgnoreCase)`
Patterns can be regex or plain substrings.

## Infinite-Loop Protection
When AutoBrowser is the default browser and a URL doesn't match any rule:
1. Read `Data/default_browser.txt` (saved before self-registration)
2. Launch saved browser **directly by EXE path** (not via shell association)
3. If no saved path → `ShellExecute` fallback

## Single-Instance
- Named `Mutex` (`AutoBrowser-SingleInstance`)
- Second instance → message box + immediate shutdown
- First instance handles URL in `MainWindow.OnLoaded`