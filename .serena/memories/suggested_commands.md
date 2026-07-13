# Suggested Commands

## Codebase Commands
- **Verify compilation (non-destructive)**:
  `dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging`
- **Full build (destroys running app)**:
  `dotnet build src\AutoBrowser\AutoBrowser.csproj`
- **Run project**:
  `dotnet run --project src\AutoBrowser\AutoBrowser.csproj`
- **Run tests**:
  `dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj`

## Windows Registry Checks
- **Read protocol command**:
  `Get-ItemProperty -Path "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command" -Name "(default)"`
- **Write fake registry path (testing prompt)**:
  `Set-ItemProperty -Path "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command" -Name "(default)" -Value '"C:\OldLocation\AutoBrowser.exe" "%1"'`
