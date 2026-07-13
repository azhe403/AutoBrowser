# Core

## Source Map
- `src/AutoBrowser/` - Main WPF application.
  - `App.xaml / App.xaml.cs` - Entry point, DI container setup, single-instance mutex, tray icon.
  - `MainWindow.xaml / .cs` - Main window, UI host (NavigationView).
  - `Models/` - Data models (`RoutingRule`, `AppSettings`, `BrowserDefinition`, `AppThemeMode`).
  - `Services/` - Core logic (`RuleService`, `SettingsService`, `ProtocolService`, `UpdateService`, `UrlInterceptorService`).
  - `ViewModels/` - UI state (`MainViewModel`, `SettingsViewModel`).
  - `Views/` - Pages (`HomePage`, `SettingsPage`, `AboutPage`) and controls (`RuleEditorView`, `RuleTesterView`).
- `src/AutoUpdater/` - Standalone updater executable.
- `src/AutoBrowser.Tests/` - Unit tests.
- `Data/` - Persistent storage (created at runtime next to EXE).

## Architecture Invariants
- **Dependency Injection**: Services and ViewModels are resolved from a central `ServiceProvider` initialized in `App.xaml.cs`.
- **Portability**: All settings and rules are stored in JSON files within the `Data/` directory next to the executable. Do not use `%APPDATA%`.
- **Registry Access**: Modifications are strictly limited to `HKCU` to avoid requiring admin privileges.
- **Window Layout**: `MainWindow` uses `NavigationView` as its root. Pages handle their own scrolling (`ScrollViewer.CanContentScroll="False"`).

## References
- `mem:tech_stack`
- `mem:suggested_commands`
- `mem:conventions`
- `mem:task_completion`
