using System.Diagnostics;
using System.IO;
using AutoBrowser.Models;
using Serilog;

namespace AutoBrowser.Services;

public class UrlInterceptorService
{
    private readonly IRuleService _ruleService;
    private readonly IDefaultBrowserService _defaultBrowserService;

    public UrlInterceptorService(IRuleService ruleService, IDefaultBrowserService defaultBrowserService)
    {
        _ruleService = ruleService;
        _defaultBrowserService = defaultBrowserService;
    }

    public bool TryRoute(string url)
    {
        Log.Information("TryRoute called with URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Verbose("URL is null or whitespace, returning false");
            Log.Information("TryRoute completed: false (null/whitespace URL)");
            return false;
        }

        url = url.Trim();
        url = StripProtocolPrefix(url);

        var rules = _ruleService.LoadRules()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ToList();

        Log.Debug("Loaded {Count} enabled rules", rules.Count);

        foreach (var rule in rules)
        {
            Log.Verbose("Checking rule '{RuleName}' (Priority: {Priority}, Pattern: {Pattern})", rule.Name, rule.Priority, rule.UrlPattern);
            if (!rule.IsMatch(url))
            {
                Log.Verbose("Rule '{RuleName}' does not match", rule.Name);
                continue;
            }

            Log.Verbose("Rule '{RuleName}' matched, launching browser: {BrowserPath}", rule.Name, rule.BrowserPath);
            LaunchBrowser(rule.BrowserPath, rule.BrowserArguments, url);
            Log.Information("TryRoute completed: true (matched rule: {RuleName})", rule.Name);
            return true;
        }

        Log.Verbose("No rules matched, checking default browser");
        var savedDefault = _defaultBrowserService.GetSavedDefaultBrowser();
        if (savedDefault is not null && File.Exists(savedDefault))
        {
            Log.Verbose("Using saved default browser: {BrowserPath}", savedDefault);
            LaunchBrowser(savedDefault, "{url}", url);
        }
        else
        {
            Log.Verbose("No saved default browser, opening in system default");
            OpenInDefaultBrowser(url);
        }

        Log.Information("TryRoute completed: false (no rules matched)");
        return false;
    }

    private static string StripProtocolPrefix(string url)
    {
        Log.Debug("StripProtocolPrefix called with URL: {Url}", url);
        var prefixes = new[] { "autobrowser:", "autobrowser://" };
        foreach (var prefix in prefixes)
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(prefix.Length).TrimStart('/');
                Log.Verbose("Removed prefix '{Prefix}', result: {Url}", prefix, url);
                break;
            }
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
            Log.Verbose("Added https:// prefix, result: {Url}", url);
        }

        Log.Debug("StripProtocolPrefix completed: {Url}", url);
        return url;
    }

    private static void LaunchBrowser(string browserPath, string argumentsTemplate, string url)
    {
        Log.Information("LaunchBrowser called - Path: {BrowserPath}, ArgsTemplate: {ArgsTemplate}, URL: {Url}", browserPath, argumentsTemplate, url);
        try
        {
            var args = argumentsTemplate.Replace("{url}", url);
            Log.Verbose("Initial args after URL replacement: {Args}", args);

            if (IsFirefox(browserPath) && !args.Contains("-osint", StringComparison.OrdinalIgnoreCase))
            {
                args = $"-osint -url \"{url}\"";
            }

            if (IsEdge(browserPath))
            {
                Log.Verbose("Edge detected, using microsoft-edge protocol for tab reuse");
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"microsoft-edge:{url}",
                    UseShellExecute = true
                });
            }
            else
            {
                Log.Verbose("Starting process: {BrowserPath} {Args}", browserPath, args);
                Process.Start(new ProcessStartInfo
                {
                    FileName = browserPath,
                    Arguments = args,
                    UseShellExecute = false
                });
            }
            Log.Information("LaunchBrowser completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "LaunchBrowser failed");
            System.Windows.MessageBox.Show($"Failed to launch browser: {ex.Message}",
                "AutoBrowser Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static bool IsEdge(string browserPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(browserPath);
        return fileName.Equals("msedge", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFirefox(string browserPath)
    {
        Log.Debug("IsFirefox called with path: {BrowserPath}", browserPath);
        var fileName = Path.GetFileNameWithoutExtension(browserPath);
        var isFirefox = fileName.Equals("firefox", StringComparison.OrdinalIgnoreCase);
        Log.Verbose("IsFirefox result: {IsFirefox} (FileName: {FileName})", isFirefox, fileName);
        return isFirefox;
    }

    private static void OpenInDefaultBrowser(string url)
    {
        Log.Information("OpenInDefaultBrowser called with URL: {Url}", url);
        try
        {
            Log.Verbose("Opening URL with system default browser");
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Log.Information("OpenInDefaultBrowser completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OpenInDefaultBrowser failed");
            System.Windows.MessageBox.Show($"Failed to open URL: {ex.Message}",
                "AutoBrowser Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
