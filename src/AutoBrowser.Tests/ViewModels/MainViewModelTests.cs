using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AutoBrowser.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;

    public MainViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());


    }

    [Fact]
    public void AppVersion_And_WindowTitle_ReturnsCorrectValues()
    {
        // Act
        var vm = new MainViewModel(_mockSettingsService.Object);

        // Assert
        Assert.NotNull(vm.AppVersion);
        Assert.Contains(vm.AppVersion, vm.WindowTitle);
        Assert.StartsWith("AutoBrowser v", vm.WindowTitle);
    }
}

public class AboutViewModelTests
{
    [Fact]
    public void AppVersion_ReturnsCorrectValue()
    {
        // Act
        var vm = new AboutViewModel();

        // Assert
        Assert.NotNull(vm.AppVersion);
        Assert.NotNull(vm.OpenUrlCommand);
    }
}

public class SettingsViewModelTests
{
    private readonly Mock<IProtocolService> _mockProtocolService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;

    public SettingsViewModelTests()
    {
        _mockProtocolService = new Mock<IProtocolService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());


    }

    [Fact]
    public void Constructor_InitializesSettings()
    {
        // Act
        var vm = new SettingsViewModel(
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);

        // Assert
        Assert.NotNull(vm.AvailableBrowsers);
        Assert.True(vm.MinimizeToTray);
    }
}

public class HomeViewModelTests
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;

    public HomeViewModelTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();

        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>());
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());


    }

    [Fact]
    public void Constructor_LoadsRules()
    {
        // Arrange
        var rules = new List<RoutingRule>
        {
            new() { Name = "Rule 1", UrlPattern = "example.com", IsEnabled = true, Sequence = 1 }
        };
        _mockRuleService.Setup(x => x.LoadRules()).Returns(rules);

        // Act
        var vm = new HomeViewModel(
            _mockRuleService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);

        // Assert
        Assert.Single(vm.Rules);
        Assert.Equal("Rule 1", vm.Rules[0].Name);
    }

    [Fact]
    public void SelectedRule_InitiallyNull_CommandsDisabled()
    {
        // Act
        var vm = new HomeViewModel(
            _mockRuleService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);

        // Assert
        Assert.Null(vm.SelectedRule);
        Assert.False(vm.HasSelectedRule);
        Assert.False(vm.EditRuleCommand.CanExecute(null));
        Assert.False(vm.DeleteRuleCommand.CanExecute(null));
        Assert.False(vm.MoveUpCommand.CanExecute(null));
        Assert.False(vm.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void SelectedRule_Set_CommandsEnabled()
    {
        // Arrange
        var vm = new HomeViewModel(
            _mockRuleService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);
        var rule = new RoutingRule { Name = "Test Rule", UrlPattern = "test.com" };

        // Act
        vm.SelectedRule = rule;

        // Assert
        Assert.NotNull(vm.SelectedRule);
        Assert.True(vm.HasSelectedRule);
        Assert.True(vm.EditRuleCommand.CanExecute(null));
        Assert.True(vm.DeleteRuleCommand.CanExecute(null));
        Assert.True(vm.MoveUpCommand.CanExecute(null));
        Assert.True(vm.MoveDownCommand.CanExecute(null));
    }
}