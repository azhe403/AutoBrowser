namespace AutoBrowser.Services;

public interface IDefaultBrowserService
{
    bool RegisterAsDefaultBrowser();
    bool UnregisterAsDefaultBrowser();
    bool IsDefaultBrowser();
    string? GetRegisteredPath();
    string? GetSavedDefaultBrowser();
}
