# AutoBrowser — Data Flow & URL Matching

## URL Flow
```
URL arrives (autobrowser:// or default click)
  → App.OnStartup (CLI args) or MainWindow.OnLoaded (second instance)
  → UrlInterceptorService.TryRoute(url)
      1. StripProtocolPrefix()
      2. LoadRules() from Data/rules.json
      3. Filter IsEnabled=true
      4. Sort by Priority ASC
      5. First regex/substring match → Launch browser
      6. No match → fallback:
           a. Saved default browser EXE (Data/default_browser.txt) → direct launch
           b. Else → ShellExecute
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