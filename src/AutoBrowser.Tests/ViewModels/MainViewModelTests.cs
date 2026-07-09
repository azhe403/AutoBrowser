using System.Windows;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IProtocolService> _mockProtocolService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;

    public MainViewModelTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockProtocolService = new Mock<IProtocolService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();

        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>());
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());

        // Ensure WPF Application instance is created for Application.Current cast in MainViewModel
        if (System.Windows.Application.Current == null)
        {
            _ = new App();
        }
    }

    [Fact]
    public void Constructor_LoadsRulesAndSettings()
    {
        // Arrange
        var rules = new List<RoutingRule>
        {
            new() { Name = "Rule 1", UrlPattern = "example.com", IsEnabled = true, Sequence = 1 }
        };
        _mockRuleService.Setup(x => x.LoadRules()).Returns(rules);

        // Act
        var vm = new MainViewModel(
            _mockRuleService.Object,
            _mockProtocolService.Object,
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
        var vm = new MainViewModel(
            _mockRuleService.Object,
            _mockProtocolService.Object,
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
        var vm = new MainViewModel(
            _mockRuleService.Object,
            _mockProtocolService.Object,
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

    [Fact]
    public void SelectedRule_Cleared_CommandsDisabled()
    {
        // Arrange
        var vm = new MainViewModel(
            _mockRuleService.Object,
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);
        var rule = new RoutingRule { Name = "Test Rule", UrlPattern = "test.com" };
        vm.SelectedRule = rule;

        // Act
        vm.SelectedRule = null;

        // Assert
        Assert.Null(vm.SelectedRule);
        Assert.False(vm.HasSelectedRule);
        Assert.False(vm.EditRuleCommand.CanExecute(null));
        Assert.False(vm.DeleteRuleCommand.CanExecute(null));
        Assert.False(vm.MoveUpCommand.CanExecute(null));
        Assert.False(vm.MoveDownCommand.CanExecute(null));
    }
}
