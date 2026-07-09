# Task Completion — Verification Steps

After any code change, run these commands in order:

## 1. Run Tests
```bash
dotnet test src\AutoBrowser.Tests\AutoBrowser.Tests.csproj
```

## 2. Build (Staging)
```bash
dotnet build src\AutoBrowser\AutoBrowser.csproj -o bin\staging
```

## 3. Launch and Verify
```powershell
$proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 15; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
```

## 4. Check Logs
```powershell
Get-ChildItem "bin\staging\Logs\" -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { Get-Content $_.FullName | Select-String "\[ERR\]" }
```

## 5. Test Re-Register Prompt (if protocol/default browser changes)
```powershell
# Read current path
$regPath = "HKCU:\Software\Classes\AutoBrowserLink\shell\open\command"
$original = (Get-ItemProperty -Path $regPath -Name "(default)")."(default)"

# Fake old path to trigger prompt
Set-ItemProperty -Path $regPath -Name "(default)" -Value '"C:\OldLocation\AutoBrowser.exe" "%1"'

# Launch — user should see the dialog
$proc = Start-Process -FilePath "bin\staging\AutoBrowser.exe" -PassThru; Start-Sleep -Seconds 15; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue

# Restore original path
Set-ItemProperty -Path $regPath -Name "(default)" -Value $original
```