using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IConfigurationService
{
    List<RoutingRule> LoadRules();
    void SaveRules(List<RoutingRule> rules);
    bool RegisterProtocolHandler();
    bool UnregisterProtocolHandler();
    bool IsProtocolRegistered();
    bool RegisterAsDefaultBrowser();
    bool UnregisterAsDefaultBrowser();
    bool IsDefaultBrowser();
    string? GetSavedDefaultBrowser();
}
