using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoBrowser.Models;

public partial class RoutingRule : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _urlPattern = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrowserDisplayName))]
    private string _browserPath = string.Empty;

    [ObservableProperty]
    private string _browserArguments = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private int _sequence;

    public string BrowserDisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BrowserPath))
                return string.Empty;

            var fileName = Path.GetFileNameWithoutExtension(BrowserPath);
            var known = BrowserDefinition.GetKnownBrowsers();
            var match = known.FirstOrDefault(b =>
                b.ExecutablePath.Equals(BrowserPath, StringComparison.OrdinalIgnoreCase));
            return match?.DisplayName ?? fileName;
        }
    }

    public bool IsMatch(string url)
    {
        if (string.IsNullOrWhiteSpace(UrlPattern) || string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            return Regex.IsMatch(url, UrlPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (RegexParseException)
        {
            return url.Contains(UrlPattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static (bool IsValid, string? Error) ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return (false, "URL pattern is required");

        try
        {
            _ = new Regex(pattern);
            return (true, null);
        }
        catch (RegexParseException ex)
        {
            return (false, $"Invalid regex: {ex.Message}");
        }
    }
}
