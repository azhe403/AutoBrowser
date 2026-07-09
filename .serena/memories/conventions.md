# Conventions

## Code Style
- C# with nullable enabled, implicit usings enabled
- File-scoped namespaces (e.g., `namespace AutoBrowser;`)
- CommunityToolkit.Mvvm source generators: `[ObservableProperty]`, `[RelayCommand]`, `partial void On<Property>Changed()`
- Services exposed via interfaces (e.g., `IRuleService`, `IProtocolService`) — concrete classes used directly in constructors (no DI container)
- Serilog structured logging with message templates: `Log.Information("Message {Param}", value)`

## Naming
- Private fields: `_camelCase` (e.g., `_ruleService`, `_isDarkTheme`)
- Properties: `PascalCase` (e.g., `SelectedRule`, `Status`)
- Commands: verb-noun via `[RelayCommand]` (e.g., `AddRuleCommand`, `CheckForUpdateCommand`)
- View files: `<Name>View.xaml` + `<Name>View.xaml.cs`
- Model files: plain names (e.g., `RoutingRule.cs`, `AppSettings.cs`)

## Architecture
- MVVM: View → ViewModel → Service → Model
- Views instantiate ViewModels directly (no DI)
- Services instantiate their dependencies directly
- Data persistence: JSON files in `Data/` folder (portable)
- Registry operations: HKCU only (no admin elevation)

## XAML
- WPF UI controls: `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`
- Theme: `ui:FluentWindow` with `Appearance:ThemeManager`
- Dialogs: `Wpf.Ui.Controls.MessageBox` with `ShowDialogAsync()` (not `System.Windows.MessageBox`)