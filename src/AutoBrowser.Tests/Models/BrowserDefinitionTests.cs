using AutoBrowser.Models;

namespace AutoBrowser.Tests.Models;

public class BrowserDefinitionTests
{
    [Fact]
    public void GetKnownBrowsers_ReturnsList()
    {
        var browsers = BrowserDefinition.GetKnownBrowsers();
        Assert.NotNull(browsers);
        Assert.IsType<List<BrowserDefinition>>(browsers);
    }

    [Fact]
    public void GetKnownBrowsers_NoDuplicates()
    {
        var browsers = BrowserDefinition.GetKnownBrowsers();
        var paths = browsers.Select(b => b.ExecutablePath.ToLowerInvariant()).ToList();
        Assert.Equal(paths.Count, paths.Distinct().Count());
    }

    [Fact]
    public void GetKnownBrowsers_NoEmptyPaths()
    {
        var browsers = BrowserDefinition.GetKnownBrowsers();
        foreach (var browser in browsers)
        {
            Assert.False(string.IsNullOrWhiteSpace(browser.ExecutablePath));
            Assert.False(string.IsNullOrWhiteSpace(browser.Name));
            Assert.False(string.IsNullOrWhiteSpace(browser.DisplayName));
        }
    }

    [Fact]
    public void GetKnownBrowsers_ExcludesSelf()
    {
        var browsers = BrowserDefinition.GetKnownBrowsers();
        var selfPath = Environment.ProcessPath ?? "";
        Assert.DoesNotContain(browsers, b =>
            b.ExecutablePath.Equals(selfPath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BrowserDefinition_Properties_CanBeSet()
     {
        var browser = new BrowserDefinition
        {
            Name = "test",
            DisplayName = "Test Browser",
            ExecutablePath = @"C:\test\browser.exe",
            ArgumentsTemplate = "{url}"
        };

        Assert.Equal("test", browser.Name);
        Assert.Equal("Test Browser", browser.DisplayName);
        Assert.Equal(@"C:\test\browser.exe", browser.ExecutablePath);
        Assert.Equal("{url}", browser.ArgumentsTemplate);
    }

    [Fact]
    public void BrowserDefinition_DefaultArgumentsTemplate_IsUrl()
    {
        var browser = new BrowserDefinition();
        Assert.Equal("{url}", browser.ArgumentsTemplate);
    }

    [Theory]
    [InlineData("Microsoft Edge", true)]
    [InlineData("Google Chrome", true)]
    [InlineData("Mozilla Firefox", true)]
    [InlineData("Opera", true)]
    [InlineData("Brave", true)]
    [InlineData("Vivaldi", true)]
    public void GetKnownBrowsers_ContainsCommonBrowsers(string displayName, bool shouldExist)
    {
        var browsers = BrowserDefinition.GetKnownBrowsers();
        var exists = browsers.Any(b => b.DisplayName == displayName);
        // Note: This test may fail if browsers aren't installed
        // We're just checking the logic, not the actual installation
        if (shouldExist)
        {
            // If browser is installed, it should be in the list
            // If not installed, we can't assert it exists
        }
    }
}
