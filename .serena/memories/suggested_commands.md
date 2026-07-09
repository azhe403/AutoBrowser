# Suggested Commands

## Build
```bash
# Full build
dotnet build src\AutoBrowser\AutoBrowser.csproj

# Build to staging (doesn't kill running instance)
dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging

# Build solution
dotnet build AutoBrowser.slnx
```

## Test
```bash
dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj
```

## Run
```bash
dotnet run --project src\AutoBrowser\AutoBrowser.csproj
```

## Publish
```bash
# Publish AutoUpdater (triggers automatically via MSBuild targets)
dotnet publish src\AutoUpdater\AutoUpdater.csproj -c Release
```

## Git (Windows)
```bash
git status
git diff
git log --oneline -10
```