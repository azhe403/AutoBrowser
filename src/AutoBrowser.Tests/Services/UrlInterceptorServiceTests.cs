using AutoBrowser.Models;
using AutoBrowser.Services;
using Moq;

namespace AutoBrowser.Tests.Services;

public class UrlInterceptorServiceTests
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly UrlInterceptorService _sut;

    public UrlInterceptorServiceTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _sut = new UrlInterceptorService(_mockRuleService.Object, _mockDefaultBrowserService.Object);
    }

    [Fact]
    public void TryRoute_NullUrl_ReturnsNull()
    {
        var result = _sut.TryRoute(null);
        Assert.Null(result);
    }

    [Fact]
    public void TryRoute_EmptyUrl_ReturnsNull()
    {
        var result = _sut.TryRoute("");
        Assert.Null(result);
    }

    [Fact]
    public void TryRoute_WhitespaceUrl_ReturnsNull()
    {
        var result = _sut.TryRoute("   ");
        Assert.Null(result);
    }

    [Fact]
    public void TryRoute_NoMatchingRules_ReturnsNull()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>
        {
            new() { Name = "GitHub", UrlPattern = @"github\.com", IsEnabled = true, Sequence = 1, BrowserPath = @"C:\chrome.exe" }
        });

        var result = _sut.TryRoute("https://gitlab.com/user/repo");
        Assert.Null(result);
    }

    [Fact]
    public void TryRoute_MatchingRule_ThrowsWhenBrowserNotFound()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>
        {
            new() { Name = "GitHub", UrlPattern = @"github\.com", IsEnabled = true, Sequence = 1, BrowserPath = @"C:\nonexistent\chrome.exe" }
        });

        Assert.ThrowsAny<Exception>(() => _sut.TryRoute("https://github.com/user/repo"));
    }

    [Fact]
    public void TryRoute_DisabledRule_SkipsRule()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>
        {
            new() { Name = "GitHub", UrlPattern = @"github\.com", IsEnabled = false, Sequence = 1, BrowserPath = @"C:\chrome.exe" }
        });

        var result = _sut.TryRoute("https://github.com/user/repo");
        Assert.Null(result);
    }

    [Fact]
    public void TryRoute_MultipleRules_ThrowsOnFirstMatch()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>
        {
            new() { Name = "Disabled", UrlPattern = @"github\.com", IsEnabled = false, Sequence = 1, BrowserPath = @"C:\chrome.exe" },
            new() { Name = "Enabled", UrlPattern = @"github\.com", IsEnabled = true, Sequence = 2, BrowserPath = @"C:\nonexistent\firefox.exe" }
        });

        Assert.ThrowsAny<Exception>(() => _sut.TryRoute("https://github.com/user/repo"));
    }

    [Fact]
    public void TryRoute_AutobrowserProtocol_StripsPrefix()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>
        {
            new() { Name = "GitHub", UrlPattern = @"github\.com", IsEnabled = true, Sequence = 1, BrowserPath = @"C:\nonexistent\chrome.exe" }
        });

        Assert.ThrowsAny<Exception>(() => _sut.TryRoute("autobrowser://github.com/user/repo"));
    }

    [Fact]
    public void TryRoute_FallbackBrowser_ReturnsNullWhenNotFound()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>());

        var result = _sut.TryRoute("https://example.com", @"C:\nonexistent\fallback.exe");
        Assert.Null(result);
    }

    [Fact]
    public void TryRoute_OrderBySequence_ThrowsOnFirstMatch()
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>
        {
            new() { Name = "High Priority", UrlPattern = @"github\.com", IsEnabled = true, Sequence = 10, BrowserPath = @"C:\chrome.exe" },
            new() { Name = "Low Priority", UrlPattern = @"github\.com", IsEnabled = true, Sequence = 1, BrowserPath = @"C:\nonexistent\firefox.exe" }
        });

        Assert.ThrowsAny<Exception>(() => _sut.TryRoute("https://github.com/user/repo"));
    }
}
