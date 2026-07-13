# Conventions

## Code Style
- Use standard `using` directives at top of file, not fully qualified names in code.
- Namespace aliases and global imports belong in `GlobalUsings.cs`.

## MVVM Pattern
- Properties in Models and ViewModels inherit from `ObservableObject` or use `CommunityToolkit.Mvvm` source generators.
- UI elements use data binding to ViewModel commands instead of code-behind events.

## WPF UI Conventions
- **Typography Rule**: `ui:TextBlock` with `FontTypography` MUST specify an explicit `Foreground` brush binding (e.g. `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`), otherwise it defaults to black in dark mode.
- **Page Layout**: Set `ScrollViewer.CanContentScroll="False"` on pages with their own `ScrollViewer` to disable NavigationView's built-in scroll.
- **Dialogs**: Use WPF UI `MessageBox` with `ShowDialogAsync()` instead of `System.Windows.MessageBox`.

## Logging Hierarchy
- `Information` - Method entry/exit points, key parameters, completion status.
- `Debug` - Intermediate steps, variable values, branch conditions.
- `Verbose` - Iterations, fine-grained details.
- `Error` - Exceptions.
- Serilog uses `Verbose` (not `Trace`).
