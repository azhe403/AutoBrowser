using System.IO;
using System.Text.Json;
using AutoBrowser.Models;
using Microsoft.Win32;

namespace AutoBrowser.Services;

public class ConfigurationService : IConfigurationService
{
    private static readonly string DataDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Data");

    private static readonly string ConfigPath = Path.Combine(DataDir, "rules.json");
    private static readonly string DefaultBrowserPath = Path.Combine(DataDir, "default_browser.txt");

    private static readonly string AppName = "AutoBrowser";
    private static readonly string ProtocolName = "autobrowser";

    public List<RoutingRule> LoadRules()
    {
        try
        {
            EnsureDataDir();

            if (!File.Exists(ConfigPath))
                return GetDefaultRules();

            var json = File.ReadAllText(ConfigPath);
            var savedRules = JsonSerializer.Deserialize<List<RoutingRule>>(json);
            if (savedRules == null || savedRules.Count == 0)
                return GetDefaultRules();

            var defaults = GetDefaultRules();
            var merged = false;

            foreach (var defaultRule in defaults)
            {
                if (savedRules.Any(r => r.Name.Equals(defaultRule.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                savedRules.Add(defaultRule);
                merged = true;
            }

            if (merged)
                SaveRules(savedRules);

            return savedRules;
        }
        catch
        {
            return GetDefaultRules();
        }
    }

    public void SaveRules(List<RoutingRule> rules)
    {
        EnsureDataDir();

        var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public bool RegisterProtocolHandler()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            using var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{ProtocolName}");
            key.SetValue("", $"URL:{AppName} Protocol");
            key.SetValue("URL Protocol", "");

            using var commandKey = key.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UnregisterProtocolHandler()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProtocolName}", false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsProtocolRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Classes\{ProtocolName}");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    public bool RegisterAsDefaultBrowser()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            SaveCurrentDefaultBrowser();

            var progId = "AutoBrowserLink";

            using var classesKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{progId}");
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
            urlAssoc.SetValue("http", progId);
            urlAssoc.SetValue("https", progId);

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
            try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\AutoBrowserLink", false); } catch { }

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

    private static List<RoutingRule> GetDefaultRules()
    {
        var browsers = BrowserDefinition.GetKnownBrowsers();
        var edge = browsers.FirstOrDefault(b => b.Name.Contains("edge"));
        var chrome = browsers.FirstOrDefault(b => b.Name.Contains("chrome"));

        var rules = new List<RoutingRule>();

        if (edge != null)
        {
            rules.Add(new RoutingRule
            {
                Name = "Work sites",
                UrlPattern = @"(teams|office|sharepoint|outlook|microsoft)\.com",
                BrowserPath = edge.ExecutablePath,
                BrowserArguments = edge.ArgumentsTemplate,
                Priority = 1
            });
        }

        if (chrome != null)
        {
            rules.Add(new RoutingRule
            {
                Name = "Social & Entertainment",
                UrlPattern = @"(youtube|reddit|twitter|x\.com|instagram|facebook)\.com",
                BrowserPath = chrome.ExecutablePath,
                BrowserArguments = chrome.ArgumentsTemplate,
                Priority = 2
            });
        }

        rules.Add(new RoutingRule
        {
            Name = "Development",
            UrlPattern = @"(github|gitlab|stackoverflow|npmjs|docker)\.(com|io)",
            BrowserPath = chrome?.ExecutablePath ?? edge?.ExecutablePath ?? "",
            BrowserArguments = chrome?.ArgumentsTemplate ?? edge?.ArgumentsTemplate ?? "{url}",
            Priority = 3
        });

        return rules;
    }
}
