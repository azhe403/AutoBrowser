using System.IO;
using Microsoft.Win32;

namespace AutoBrowser.Services;

public class DefaultBrowserService : IDefaultBrowserService
{
    private const string AppName = "AutoBrowser";
    private const string ProgId = "AutoBrowserLink";

    private static readonly string DataDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string DefaultBrowserPath = Path.Combine(DataDir, "default_browser.txt");

    public bool RegisterAsDefaultBrowser()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            SaveCurrentDefaultBrowser();

            using var classesKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{ProgId}");
            classesKey.SetValue("", $"HTTP:{AppName}");
            classesKey.SetValue("FriendlyTypeName", AppName);
            using var dqKey = classesKey.CreateSubKey(@"DefaultIcon");
            dqKey.SetValue("", $"\"{exePath}\",0");
            using var cmdKey = classesKey.CreateSubKey(@"shell\open\command");
            cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");

            using var capabilities = Registry.CurrentUser.CreateSubKey(
                $@"Software\{AppName}\Capabilities");
            capabilities.SetValue("ApplicationName", AppName);
            capabilities.SetValue("ApplicationDescription", "Smart URL router");

            using var urlAssoc = capabilities.CreateSubKey("URLAssociations");
            urlAssoc.SetValue("http", ProgId);
            urlAssoc.SetValue("https", ProgId);

            using var registeredApp = Registry.CurrentUser.CreateSubKey(
                @"Software\RegisteredApplications");
            registeredApp.SetValue(AppName, $@"Software\{AppName}\Capabilities");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UnregisterAsDefaultBrowser()
    {
        try
        {
            using var registeredApp = Registry.CurrentUser.CreateSubKey(
                @"Software\RegisteredApplications");
            registeredApp.DeleteValue(AppName, false);

            try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\{AppName}", false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false); } catch { }

            RemoveSavedDefaultBrowser();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsDefaultBrowser()
    {
        try
        {
            using var registeredApp = Registry.CurrentUser.OpenSubKey(
                @"Software\RegisteredApplications");
            return registeredApp?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public string? GetSavedDefaultBrowser()
    {
        try
        {
            return File.Exists(DefaultBrowserPath)
                ? File.ReadAllText(DefaultBrowserPath).Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private void SaveCurrentDefaultBrowser()
    {
        try
        {
            var currentPath = GetCurrentDefaultBrowserPath();
            if (!string.IsNullOrEmpty(currentPath))
            {
                EnsureDataDir();
                File.WriteAllText(DefaultBrowserPath, currentPath);
            }
        }
        catch { }
    }

    private void RemoveSavedDefaultBrowser()
    {
        try
        {
            if (File.Exists(DefaultBrowserPath))
                File.Delete(DefaultBrowserPath);
        }
        catch { }
    }

    public string? GetRegisteredPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgId}\shell\open\command");
            var cmd = key?.GetValue("") as string;
            if (string.IsNullOrEmpty(cmd)) return null;
            return ParseExePath(cmd);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCurrentDefaultBrowserPath()
    {
        try
        {
            using var userChoice = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
            var progId = userChoice?.GetValue("Progid") as string;
            if (string.IsNullOrEmpty(progId)) return null;

            using var commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
            var cmd = commandKey?.GetValue("") as string;
            if (string.IsNullOrEmpty(cmd)) return null;

            return ParseExePath(cmd);
        }
        catch
        {
            return null;
        }
    }

    private static string? ParseExePath(string commandLine)
    {
        commandLine = commandLine.Trim();
        if (commandLine.StartsWith("\""))
        {
            var end = commandLine.IndexOf('"', 1);
            return end > 0 ? commandLine[1..end] : null;
        }

        var firstSpace = commandLine.IndexOf(' ');
        return firstSpace > 0 ? commandLine[..firstSpace] : commandLine;
    }

    private static void EnsureDataDir()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);
    }
}
