using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using AutoBrowser.Helpers;
using Serilog;

namespace AutoBrowser;

public partial class App
{
    public void ProcessUrl(string url)
    {
        Log.Debug("ProcessUrl called: {Url}", url);

        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var fallbackPath = _settingsService.LoadSettings().FallbackBrowserPath;
        var browser = interceptor.TryRoute(url, fallbackPath);
        if (browser is not null)
        {
            Log.Information("URL routed via {Browser}: {Url}", browser, url);
            ShowNotification("AutoBrowser", $"Routed via {browser}:\n{url}");
            return;
        }

        Log.Warning("No rule matched for URL: {Url}", url);
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
    }

    public void ActivateFromTray(string? url = null)
    {
        if (_mainWindow == null) return;
        Log.Information("ActivateFromTray called, Url={Url}", url ?? "(none)");

        WindowForegroundHelper.BringToFront(_mainWindow);

        if (!string.IsNullOrEmpty(url))
        {
            Log.Information("Processing forwarded URL: {Url}", url);
            ProcessUrl(url);
        }

        Log.Debug("ActivateFromTray complete");
    }

    private static bool IsUrl(string? value) =>
        value is not null
        && (value.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private bool TryRouteUrl(string url)
    {
        Log.Information("Routing URL via UrlInterceptorService");
        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var fallbackPath = _settingsService.LoadSettings().FallbackBrowserPath;
        var browser = interceptor.TryRoute(url, fallbackPath);

        if (browser is not null)
        {
            Log.Debug("URL routed via {Browser}, shutting down", browser);
            ShowNotification("AutoBrowser", $"Routed via {browser}:\n{url}");
            return true;
        }

        Log.Debug("No match for URL, showing notification and continuing to main window");
        ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        return false;
    }

    private void StartPipeServer()
    {
        _singleInstanceService = new SingleInstanceService();
        _singleInstanceService.StartServer(
            url =>
            {
                Log.Information("Second instance requested activation, Url={Url}", url ?? "(none)");
                ActivateFromTray(url);
            },
            Dispatcher);
    }
}
