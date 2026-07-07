using AutoBrowser.Models;

namespace AutoBrowser.Tests.Models;

public class RoutingRuleTests
{
    [Fact]
    public void IsMatch_RegexPattern_MatchesCorrectly()
    {
        var rule = new RoutingRule
        {
            Name = "GitHub",
            UrlPattern = @"^https?://(www\.)?github\.com",
            IsEnabled = true
        };

        Assert.True(rule.IsMatch("https://github.com/user/repo"));
        Assert.True(rule.IsMatch("http://www.github.com/user/repo"));
        Assert.True(rule.IsMatch("https://github.com"));
        Assert.False(rule.IsMatch("https://gitlab.com/user/repo"));
        // Regex.IsMatch finds partial matches, so extra text still matches
        Assert.True(rule.IsMatch("https://github.com/user/repo extra"));
    }

    [Fact]
    public void IsMatch_SubstringPattern_MatchesCorrectly()
    {
        var rule = new RoutingRule
        {
            Name = "ClickUp",
            UrlPattern = "clickup.com",
            IsEnabled = true
        };

        Assert.True(rule.IsMatch("https://bluebirdgroup.clickup.com/t/123"));
        Assert.True(rule.IsMatch("http://clickup.com/login"));
        // "notclickup.com" contains "clickup.com" as substring, so it matches
        Assert.True(rule.IsMatch("https://notclickup.com"));
        Assert.False(rule.IsMatch("https://example.com"));
    }

    [Fact]
    public void IsMatch_CaseInsensitive_MatchesCorrectly()
    {
        var rule = new RoutingRule
        {
            Name = "GitHub",
            UrlPattern = @"github\.com",
            IsEnabled = true
        };

        Assert.True(rule.IsMatch("https://GITHUB.COM/user"));
        Assert.True(rule.IsMatch("https://Github.Com/user"));
    }

    [Fact]
    public void IsMatch_EmptyPattern_ReturnsFalse()
    {
        var rule = new RoutingRule
        {
            Name = "Empty",
            UrlPattern = "",
            IsEnabled = true
        };

        Assert.False(rule.IsMatch("https://github.com"));
    }

    [Fact]
    public void IsMatch_EmptyUrl_ReturnsFalse()
    {
        var rule = new RoutingRule
        {
            Name = "GitHub",
            UrlPattern = @"github\.com",
            IsEnabled = true
        };

        Assert.False(rule.IsMatch(""));
        Assert.False(rule.IsMatch("  "));
    }

    [Fact]
    public void IsMatch_InvalidRegex_FallsBackToSubstring()
    {
        var rule = new RoutingRule
        {
            Name = "Invalid Regex",
            UrlPattern = "[invalid",
            IsEnabled = true
        };

        // Should fall back to substring matching
        Assert.True(rule.IsMatch("https://example.com/[invalid/path"));
        Assert.False(rule.IsMatch("https://example.com/valid/path"));
    }

    [Fact]
    public void IsMatch_ComplexRegex_MatchesCorrectly()
    {
        var rule = new RoutingRule
        {
            Name = "Jira",
            UrlPattern = @"^https?://.*\.atlassian\.net/browse/.*",
            IsEnabled = true
        };

        Assert.True(rule.IsMatch("https://company.atlassian.net/browse/PROJ-123"));
        Assert.True(rule.IsMatch("http://team.atlassian.net/browse/BUG-456"));
        Assert.False(rule.IsMatch("https://atlassian.net"));
        Assert.False(rule.IsMatch("https://company.atlassian.net/project/123"));
    }

    [Fact]
    public void BrowserDisplayName_WithKnownBrowser_ReturnsDisplayName()
    {
        var rule = new RoutingRule
        {
            Name = "Test",
            UrlPattern = "test",
            BrowserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
        };

        // BrowserDisplayName depends on BrowserDefinition.GetKnownBrowsers()
        // which checks if the file exists, so we just verify it doesn't throw
        var displayName = rule.BrowserDisplayName;
        Assert.NotNull(displayName);
    }

    [Fact]
    public void BrowserDisplayName_WithUnknownBrowser_ReturnsFileName()
    {
        var rule = new RoutingRule
        {
            Name = "Test",
            UrlPattern = "test",
            BrowserPath = @"C:\Custom\Path\mybrowser.exe"
        };

        var displayName = rule.BrowserDisplayName;
        Assert.Equal("mybrowser", displayName);
    }

    [Fact]
    public void BrowserDisplayName_EmptyPath_ReturnsEmpty()
    {
        var rule = new RoutingRule
        {
            Name = "Test",
            UrlPattern = "test",
            BrowserPath = ""
        };

        Assert.Equal(string.Empty, rule.BrowserDisplayName);
    }

    [Theory]
    [InlineData(@"github\.com", true)]
    [InlineData(@"^https?://.*", true)]
    [InlineData(@"[invalid", false)]
    [InlineData(@"(unclosed", false)]
    [InlineData(@"", false)]
    [InlineData(@"   ", false)]
    public void ValidatePattern_ReturnsExpectedResult(string pattern, bool expectedValid)
    {
        var (isValid, error) = RoutingRule.ValidatePattern(pattern);
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
            Assert.NotNull(error);
    }
}
