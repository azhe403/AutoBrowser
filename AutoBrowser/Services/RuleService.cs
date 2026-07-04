using System.IO;
using System.Text.Json;
using AutoBrowser.Models;

namespace AutoBrowser.Services;

public class RuleService : IRuleService
{
    private static readonly string DataDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string ConfigPath = Path.Combine(DataDir, "rules.json");

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
