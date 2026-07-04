using System.Diagnostics;
using System.IO;
using AutoBrowser.Models;

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
        if (string.IsNullOrWhiteSpace(url))
            return false;

        url = url.Trim();
        url = StripProtocolPrefix(url);

        var rules = _ruleService.LoadRules()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ToList();

        foreach (var rule in rules)
        {
            if (!rule.IsMatch(url))
                continue;

            LaunchBrowser(rule.BrowserPath, rule.BrowserArguments, url);
            return true;
        }

        var savedDefault = _defaultBrowserService.GetSavedDefaultBrowser();
        if (savedDefault is not null && File.Exists(savedDefault))
        {
            LaunchBrowser(savedDefault, "{url}", url);
        }
        else
        {
            OpenInDefaultBrowser(url);
        }

        return false;
    }

    private static string StripProtocolPrefix(string url)
    {
        var prefixes = new[] { "autobrowser:", "autobrowser://" };
        foreach (var prefix in prefixes)
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(prefix.Length).TrimStart('/');
                break;
            }
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        return url;
    }

    private static void LaunchBrowser(string browserPath, string argumentsTemplate, string url)
    {
        try
        {
            var args = argumentsTemplate.Replace("{url}", url);
            Process.Start(new ProcessStartInfo
            {
                FileName = browserPath,
                Arguments = args,
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to launch browser: {ex.Message}",
                "AutoBrowser Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static void OpenInDefaultBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open URL: {ex.Message}",
                "AutoBrowser Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
