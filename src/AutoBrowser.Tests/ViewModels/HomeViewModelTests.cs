using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class HomeViewModelTests
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IDialogService> _mockDialogService;

    public HomeViewModelTests()
    {
        _mockRuleService = new Mock<IRuleService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockDialogService = new Mock<IDialogService>();

        _mockRuleService.Setup(x => x.LoadRules()).Returns(new List<RoutingRule>());
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());
        _mockDialogService.Setup(x => x.ShowAddRuleDialog()).Returns((RoutingRule?)null);
        _mockDialogService.Setup(x => x.ShowEditRuleDialog(It.IsAny<RoutingRule>())).Returns((RoutingRule?)null);
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
            _mockSettingsService.Object,
            _mockDialogService.Object);

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
            _mockSettingsService.Object,
            _mockDialogService.Object);

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
            _mockSettingsService.Object,
            _mockDialogService.Object);
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
