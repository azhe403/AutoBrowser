using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;
using Serilog;

namespace AutoUpdater;

internal static class Program
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(2);
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 500;

    private static string? _appDir;
    private static string? _backupDir;

    static async Task<int> Main(string[] args)
    {
        var pidOption = new Option<int>("--pid", "Process ID of the app to wait for") { IsRequired = true };
        var sourceOption = new Option<string>("--source", "Path to update workspace directory") { IsRequired = true };
        var exeOption = new Option<string>("--exe", "Path to app executable") { IsRequired = true };

        var rootCommand = new RootCommand("AutoUpdater - applies updates to AutoBrowser")
        {
            pidOption,
            sourceOption,
            exeOption
        };

        rootCommand.SetHandler(async (int appPid, string updateWorkspace, string appExePath) =>
        {
            Environment.ExitCode = await RunUpdate(appPid, updateWorkspace, appExePath, args);
        }, pidOption, sourceOption, exeOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> RunUpdate(int appPid, string updateWorkspace, string appExePath, string[] args)
    {
        var appDirForLog = Path.GetDirectoryName(appExePath) ?? ".";

        var logDir = Path.Combine(appDirForLog, "Logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "Updater-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("=== AutoUpdater Started ===");
            Log.Information("Args: {Args}", string.Join(" ", args));

            _appDir = Path.GetDirectoryName(appExePath) ?? ".";
            _backupDir = Path.Combine(_appDir, "UpdateBackup");

            if (!Directory.Exists(_appDir))
            {
                Log.Error("App directory not found: {AppDir}", _appDir);
                return 1;
            }

            if (!Directory.Exists(updateWorkspace))
            {
                Log.Error("Update workspace not found: {Workspace}", updateWorkspace);
                return 1;
            }

            Log.Information("Waiting for process {Pid} to exit...", appPid);
            try
            {
                var process = Process.GetProcessById(appPid);
                if (!process.WaitForExit((int)WaitTimeout.TotalMilliseconds))
                {
                    Log.Warning("Timed out waiting for main process to exit, killing...");
                    process.Kill(entireProcessTree: true);
                }
                Log.Information("Main process exited");
            }
            catch (ArgumentException)
            {
                Log.Information("Main process already exited");
            }

            Log.Information("Backing up current files...");
            CleanBackup();
            Directory.CreateDirectory(_backupDir);
            BackupFiles();

            Log.Information("Copying update files from {Source} to {Dest}...", updateWorkspace, _appDir);
            if (!TryCopyWithRetry(updateWorkspace, _appDir))
            {
                Log.Error("Update copy failed, rolling back...");
                RestoreBackup();
                return 1;
            }

            Log.Information("Verifying update...");
            if (!VerifyUpdate(updateWorkspace))
            {
                Log.Error("Verification failed, rolling back...");
                RestoreBackup();
                return 1;
            }

            CleanBackup();

            Log.Information("Update successful. Restarting app: {Exe}", appExePath);
            StartApp(appExePath);

            Log.Information("=== AutoUpdater Finished ===");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error");
            RestoreBackup();
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void BackupFiles()
    {
        if (_appDir is null || _backupDir is null) return;

        foreach (var file in Directory.EnumerateFiles(_appDir, "*", SearchOption.TopDirectoryOnly))
        {
            var dest = Path.Combine(_backupDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
            Log.Verbose("Backed up: {File}", Path.GetFileName(file));
        }
        Log.Debug("Backed up {Count} files", Directory.GetFiles(_backupDir).Length);
    }

    private static void RestoreBackup()
    {
        if (!Directory.Exists(_backupDir)) return;

        Log.Information("Restoring backup...");
        foreach (var file in Directory.EnumerateFiles(_backupDir, "*", SearchOption.TopDirectoryOnly))
        {
            var dest = Path.Combine(_appDir!, Path.GetFileName(file));
            try
            {
                File.Copy(file, dest, overwrite: true);
                Log.Verbose("Restored: {File}", Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Rollback error for {File}", Path.GetFileName(file));
            }
        }
        Log.Debug("Backup restore completed");
    }

    private static void CleanBackup()
    {
        try
        {
            if (Directory.Exists(_backupDir))
            {
                Directory.Delete(_backupDir, recursive: true);
                Log.Verbose("Cleaned backup directory");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to clean backup directory");
        }
    }

    private static bool TryCopyWithRetry(string sourceDir, string destDir)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
                {
                    var dest = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, dest, overwrite: true);
                    Log.Verbose("Copied: {File}", Path.GetFileName(file));
                }
                Log.Debug("Copy completed on attempt {Attempt}", attempt);
                return true;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                Log.Warning(ex, "Copy attempt {Attempt} failed, retrying...", attempt);
                Thread.Sleep(RetryDelayMs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Copy failed after {MaxRetries} attempts", MaxRetries);
                return false;
            }
        }
        return false;
    }

    private static bool VerifyUpdate(string updateWorkspace)
    {
        var verified = 0;
        foreach (var updateFile in Directory.EnumerateFiles(updateWorkspace, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(updateFile);
            var destFile = Path.Combine(_appDir!, fileName);
            if (!File.Exists(destFile))
            {
                Log.Error("Verification failed: {File} not found after copy", fileName);
                return false;
            }

            var updateHash = ComputeSha256(updateFile);
            var destHash = ComputeSha256(destFile);
            if (!updateHash.SequenceEqual(destHash))
            {
                Log.Error("Verification failed: {File} hash mismatch", fileName);
                return false;
            }
            verified++;
        }
        Log.Debug("Verified {Count} files", verified);
        return true;
    }

    private static byte[] ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return sha256.ComputeHash(stream);
    }

    private static void StartApp(string appExePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = appExePath,
                UseShellExecute = true,
                WorkingDirectory = _appDir
            };
            Process.Start(psi);
            Log.Information("App restarted successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restart app");
        }
    }
}
