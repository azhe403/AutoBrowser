using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IRuleService
{
    List<RoutingRule> LoadRules();
    void SaveRules(List<RoutingRule> rules);
}
