using System.IO;
using Microsoft.Win32;

namespace AutoBrowser.Models;

public class BrowserDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string ArgumentsTemplate { get; set; } = "{url}";

    public static List<BrowserDefinition> GetKnownBrowsers()
    {
        var browsers = new List<BrowserDefinition>();
        var selfPath = Environment.ProcessPath ?? "";
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        void TryAdd(string name, string displayName, string path, string args = "{url}")
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path) &&
                !path.Equals(selfPath, StringComparison.OrdinalIgnoreCase) &&
                browsers.All(b => !b.ExecutablePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                browsers.Add(new BrowserDefinition
                {
                    Name = name,
                    DisplayName = displayName,
                    ExecutablePath = path,
                    ArgumentsTemplate = args
                });
            }
        }

        TryAdd("edge", "Microsoft Edge",
            Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"), "--new-tab {url}");
        TryAdd("edge_x86", "Microsoft Edge",
            Path.Combine(programFilesX86 ?? "", "Microsoft", "Edge", "Application", "msedge.exe"), "--new-tab {url}");
        TryAdd("edge_user", "Microsoft Edge",
            Path.Combine(localAppData, "Microsoft", "Edge", "Application", "msedge.exe"), "--new-tab {url}");
        TryAdd("chrome", "Google Chrome",
            Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"));
        TryAdd("chrome_x86", "Google Chrome",
            Path.Combine(programFilesX86 ?? "", "Google", "Chrome", "Application", "chrome.exe"));
        TryAdd("chrome_user", "Google Chrome",
            Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe"));
        TryAdd("chrome_beta", "Google Chrome Beta",
            Path.Combine(localAppData, "Google", "Chrome Beta", "Application", "chrome.exe"));
        TryAdd("chrome_dev", "Google Chrome Dev",
            Path.Combine(localAppData, "Google", "Chrome Dev", "Application", "chrome.exe"));
        TryAdd("chrome_canary", "Google Chrome Canary",
            Path.Combine(localAppData, "Google", "Chrome SxS", "Application", "chrome.exe"));
        TryAdd("firefox", "Mozilla Firefox",
            Path.Combine(programFiles, "Mozilla Firefox", "firefox.exe"));
        TryAdd("firefox_x86", "Mozilla Firefox",
            Path.Combine(programFilesX86 ?? "", "Mozilla Firefox", "firefox.exe"));
        TryAdd("firefox_dev", "Firefox Developer Edition",
            Path.Combine(localAppData, "Firefox Developer Edition", "firefox.exe"));
        TryAdd("firefox_nightly", "Firefox Nightly",
            Path.Combine(localAppData, "Firefox Nightly", "firefox.exe"));
        TryAdd("opera", "Opera",
            Path.Combine(programFiles, "Opera", "launcher.exe"));
        TryAdd("opera_x86", "Opera",
            Path.Combine(programFilesX86 ?? "", "Opera", "launcher.exe"));
        TryAdd("opera_gx", "Opera GX",
            Path.Combine(localAppData, "Programs", "Opera GX", "launcher.exe"));
        TryAdd("brave", "Brave",
            Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
        TryAdd("brave_x86", "Brave",
            Path.Combine(programFilesX86 ?? "", "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
        TryAdd("brave_user", "Brave",
            Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
        TryAdd("brave_beta", "Brave Beta",
            Path.Combine(localAppData, "BraveSoftware", "Brave-Browser-Beta", "Application", "brave.exe"));
        TryAdd("brave_nightly", "Brave Nightly",
            Path.Combine(localAppData, "BraveSoftware", "Brave-Browser-Nightly", "Application", "brave.exe"));
        TryAdd("vivaldi", "Vivaldi",
            Path.Combine(programFiles, "Vivaldi", "Application", "vivaldi.exe"));

        ScanLocalAppDataForBrowsers(browsers, localAppData);

        ScanAppPathsRegistry(browsers, RegistryView.Registry64);
        ScanAppPathsRegistry(browsers, RegistryView.Registry32);
        ScanAppPathsRegistryCurrentUser(browsers);
        ScanUrlAssociationsRegistry(browsers);
        ScanRegisteredApplications(browsers, RegistryHive.LocalMachine);
        ScanRegisteredApplications(browsers, RegistryHive.CurrentUser);

        return browsers;
    }

    private static void ScanAppPathsRegistry(List<BrowserDefinition> browsers, RegistryView view)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var appPaths = baseKey.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
            if (appPaths == null) return;

            ScanAppPathsSubKey(appPaths, browsers);
        }
        catch { }
    }

    private static void ScanAppPathsRegistryCurrentUser(List<BrowserDefinition> browsers)
    {
        try
        {
            using var appPaths = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
            if (appPaths == null) return;

            ScanAppPathsSubKey(appPaths, browsers);
        }
        catch { }
    }

    private static void ScanAppPathsSubKey(RegistryKey appPaths, List<BrowserDefinition> browsers)
    {
        foreach (var subKeyName in appPaths.GetSubKeyNames())
        {
            try
            {
                using var subKey = appPaths.OpenSubKey(subKeyName);
                var path = subKey?.GetValue("") as string;
                if (string.IsNullOrEmpty(path)) continue;

                var exePath = ParseExePath(path);
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) continue;
                if (browsers.Any(b => b.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var name = Path.GetFileNameWithoutExtension(subKeyName);
                var known = GetKnownBrowserName(name);
                if (known == null) continue;

                browsers.Add(new BrowserDefinition
                {
                    Name = name,
                    DisplayName = known,
                    ExecutablePath = exePath,
                    ArgumentsTemplate = "{url}"
                });
            }
            catch { }
        }
    }

    private static void ScanUrlAssociationsRegistry(List<BrowserDefinition> browsers)
    {
        try
        {
            var userChoicePath = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
            using var userChoice = Registry.CurrentUser.OpenSubKey(userChoicePath);
            var progId = userChoice?.GetValue("Progid") as string;
            if (string.IsNullOrEmpty(progId)) return;

            using var commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
            var cmd = commandKey?.GetValue("") as string;
            if (string.IsNullOrEmpty(cmd)) return;

            var path = ParseExePath(cmd);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            if (path.Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) return;

            if (browsers.All(b => !b.ExecutablePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                browsers.Insert(0, new BrowserDefinition
                {
                    Name = "default",
                    DisplayName = "System Default Browser",
                    ExecutablePath = path,
                    ArgumentsTemplate = "{url}"
                });
            }
        }
        catch { }
    }

    private static void ScanRegisteredApplications(List<BrowserDefinition> browsers, RegistryHive hive)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var registeredApps = baseKey.OpenSubKey(@"SOFTWARE\RegisteredApplications");
            if (registeredApps == null) return;

            foreach (var appName in registeredApps.GetValueNames())
            {
                try
                {
                    var appPath = registeredApps.GetValue(appName) as string;
                    if (string.IsNullOrEmpty(appPath)) continue;

                    using var appKey = Registry.LocalMachine.OpenSubKey(appPath);
                    var capabilitiesUrl = appKey?.OpenSubKey("URLAssociations")?.GetValue("http") as string
                                          ?? appKey?.OpenSubKey("URLAssociations")?.GetValue("https") as string;
                    if (string.IsNullOrEmpty(capabilitiesUrl)) continue;

                    using var commandKey = Registry.ClassesRoot.OpenSubKey(
                        $@"{capabilitiesUrl}\shell\open\command");
                    var cmd = commandKey?.GetValue("") as string;
                    if (string.IsNullOrEmpty(cmd)) continue;

                    var exePath = ParseExePath(cmd);
                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) continue;
                    if (exePath.Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (browsers.Any(b => b.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    browsers.Add(new BrowserDefinition
                    {
                        Name = appName,
                        DisplayName = appName,
                        ExecutablePath = exePath,
                        ArgumentsTemplate = "{url}"
                    });
                }
                catch { }
            }
        }
        catch { }
    }

    private static void ScanLocalAppDataForBrowsers(List<BrowserDefinition> browsers, string localAppData)
    {
        if (!Directory.Exists(localAppData)) return;

        var browserExes = new[] { "chrome.exe", "msedge.exe", "firefox.exe", "brave.exe",
            "vivaldi.exe", "opera.exe", "launcher.exe", "tor.exe", "waterfox.exe",
            "librewolf.exe", "palemoon.exe", "iridium.exe", "epic.exe" };

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(localAppData))
            {
                foreach (var exe in browserExes)
                {
                    var path = Path.Combine(dir, exe);
                    if (!File.Exists(path)) continue;
                    if (browsers.Any(b => b.ExecutablePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                        break;

                    var dirName = Path.GetFileName(dir);
                    browsers.Add(new BrowserDefinition
                    {
                        Name = exe.Replace(".exe", ""),
                        DisplayName = dirName,
                        ExecutablePath = path,
                        ArgumentsTemplate = "{url}"
                    });
                    break;
                }
            }
        }
        catch { }
    }

    private static string? GetKnownBrowserName(string exeName)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["msedge"] = "Microsoft Edge",
            ["chrome"] = "Google Chrome",
            ["firefox"] = "Mozilla Firefox",
            ["opera"] = "Opera",
            ["brave"] = "Brave",
            ["vivaldi"] = "Vivaldi",
            ["msedge.exe"] = "Microsoft Edge",
            ["chrome.exe"] = "Google Chrome",
            ["firefox.exe"] = "Mozilla Firefox",
            ["opera.exe"] = "Opera",
            ["brave.exe"] = "Brave",
            ["vivaldi.exe"] = "Vivaldi",
        };

        return map.TryGetValue(exeName, out var name) ? name : null;
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
}
