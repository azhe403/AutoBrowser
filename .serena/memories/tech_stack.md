# Tech Stack

## Core
- **Language**: C# (.NET 10)
- **Framework**: WPF (Windows Presentation Foundation)
- **UI Toolkit**: WPF UI (`xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`)

## Architecture
- **Pattern**: MVVM (Model-View-ViewModel)
- **State Management**: `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand)
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection`

## Libraries
- **Logging**: Serilog (structured logging to file)

## Build
- **Project Format**: SDK-style (`net10.0-windows`)
- **Updater**: Standalone single-file console app (`PublishSingleFile=true`) built via MSBuild task.
