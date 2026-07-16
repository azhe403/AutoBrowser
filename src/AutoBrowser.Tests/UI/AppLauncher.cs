using System.Diagnostics;
using System.IO;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System.Threading;
using System.Linq;

namespace AutoBrowser.Tests.UI;

public class AppLauncher : IDisposable
{
    private Process? _process;
    private UIA3Automation? _automation;
    private FlaUI.Core.Application? _app;
    private string? _tempDir;

    public FlaUI.Core.Application App => _app ?? throw new InvalidOperationException("App not launched");
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Automation not initialized");

    public FlaUI.Core.Application Launch()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(2000);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to kill existing process: {ex.Message}");
        }

        foreach (var proc in Process.GetProcessesByName("AutoBrowser"))
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill();
                    proc.WaitForExit(2000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to kill lingering process: {ex.Message}");
            }
        }

        Thread.Sleep(500);

        _tempDir = Path.Combine(Path.GetTempPath(), $"AutoBrowserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        var sourceDir = AppContext.BaseDirectory;
        CopyDirectory(sourceDir, _tempDir);

        var exePath = Path.Combine(_tempDir, "AutoBrowser.exe");
        Console.WriteLine($"Launching test app at {exePath}");

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--no-single-instance --no-update-check --no-re-register-prompt",
            UseShellExecute = false
        });

        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start AutoBrowser process.");
        }

        Thread.Sleep(1000);

        _automation = new UIA3Automation();
        _app = FlaUI.Core.Application.Attach(_process);

        return _app;
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        var dirs = dir.GetDirectories();

        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (var subdirectory in dirs)
        {
            CopyDirectory(subdirectory.FullName, Path.Combine(destDir, subdirectory.Name));
        }
    }

    public void DismissBlockingDialogs(int retries = 3)
    {
        if (_app == null) return;

        for (var attempt = 0; attempt < retries; attempt++)
        {
            try
            {
                var allWindows = _app.GetAllTopLevelWindows(_automation!);
                var dismissed = false;

                foreach (var window in allWindows)
                {
                    foreach (var label in new[] { "No", "Cancel", "Close" })
                    {
                        var button = window.FindFirstDescendant(cf =>
                            cf.ByControlType(ControlType.Button).And(cf.ByText(label)));
                        if (button != null)
                        {
                            button.Click();
                            dismissed = true;
                            Thread.Sleep(500);
                        }
                    }
                }

                if (!dismissed) break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to dismiss dialogs: {ex.Message}");
                break;
            }
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to clean up process during dispose: {ex.Message}");
        }

        foreach (var proc in Process.GetProcessesByName("AutoBrowser"))
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill();
                    proc.WaitForExit(2000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to kill background process: {ex.Message}");
            }
            finally
            {
                try
                {
                    proc.Dispose();
                }
                catch
                {
                    /* Ignore disposal errors */
                }
            }
        }

        _automation?.Dispose();
        _process?.Dispose();

        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete temp dir: {ex.Message}");
            }
        }
    }
}
