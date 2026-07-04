using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AutoUpdater;

internal static class Program
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(2);
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 500;

    private static readonly string BackupDir = Path.Combine(
        Path.GetTempPath(), "AutoBrowserUpdate", "backup");

    private static string? _appDir;

    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: AutoUpdater <appPid> <updateWorkspace> <appExePath>");
                return 1;
            }

            if (!int.TryParse(args[0], out var appPid))
            {
                Console.Error.WriteLine($"Invalid PID: {args[0]}");
                return 1;
            }

            var updateWorkspace = args[1];
            var appExePath = args[2];
            _appDir = Path.GetDirectoryName(appExePath);

            if (string.IsNullOrEmpty(_appDir) || !Directory.Exists(_appDir))
            {
                Console.Error.WriteLine($"App directory not found: {_appDir}");
                return 1;
            }

            if (!Directory.Exists(updateWorkspace))
            {
                Console.Error.WriteLine($"Update workspace not found: {updateWorkspace}");
                return 1;
            }

            Console.WriteLine($"Waiting for process {appPid} to exit...");
            try
            {
                var process = Process.GetProcessById(appPid);
                if (!process.WaitForExit((int)WaitTimeout.TotalMilliseconds))
                {
                    Console.Error.WriteLine("Timed out waiting for main process to exit.");
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (ArgumentException)
            {
                // Process already exited
            }

            Console.WriteLine("Backing up current files...");
            CleanBackup();
            Directory.CreateDirectory(BackupDir);
            BackupFiles();

            Console.WriteLine("Copying update files...");
            if (!TryCopyWithRetry(updateWorkspace, _appDir))
            {
                Console.WriteLine("Update failed. Rolling back...");
                RestoreBackup();
                return 1;
            }

            Console.WriteLine("Verifying update...");
            if (!VerifyUpdate(updateWorkspace))
            {
                Console.WriteLine("Verification failed. Rolling back...");
                RestoreBackup();
                return 1;
            }

            CleanBackup();

            Console.WriteLine("Update successful. Restarting app...");
            StartApp(appExePath);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            RestoreBackup();
            return 1;
        }
    }

    private static void BackupFiles()
    {
        foreach (var file in Directory.EnumerateFiles(_appDir!, "*", SearchOption.TopDirectoryOnly))
        {
            var dest = Path.Combine(BackupDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static void RestoreBackup()
    {
        if (!Directory.Exists(BackupDir)) return;

        foreach (var file in Directory.EnumerateFiles(BackupDir, "*", SearchOption.TopDirectoryOnly))
        {
            var dest = Path.Combine(_appDir!, Path.GetFileName(file));
            try
            {
                File.Copy(file, dest, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Rollback error for {file}: {ex.Message}");
            }
        }
    }

    private static void CleanBackup()
    {
        try
        {
            if (Directory.Exists(BackupDir))
                Directory.Delete(BackupDir, recursive: true);
        }
        catch
        {
            // Best effort
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
                }
                return true;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                Console.WriteLine($"Copy attempt {attempt} failed: {ex.Message}. Retrying...");
                Thread.Sleep(RetryDelayMs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Copy failed after {MaxRetries} attempts: {ex.Message}");
                return false;
            }
        }
        return false;
    }

    private static bool VerifyUpdate(string updateWorkspace)
    {
        foreach (var updateFile in Directory.EnumerateFiles(updateWorkspace, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(updateFile);
            var destFile = Path.Combine(_appDir!, fileName);
            if (!File.Exists(destFile)) return false;

            var updateHash = ComputeSha256(updateFile);
            var destHash = ComputeSha256(destFile);
            if (!updateHash.SequenceEqual(destHash)) return false;
        }
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to restart app: {ex.Message}");
        }
    }
}
