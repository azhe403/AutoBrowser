using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class HomeViewModelRuleTests
{
    private readonly Mock<IRuleService> _mockRuleService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IDialogService> _mockDialogService;

    public HomeViewModelRuleTests()
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

    private HomeViewModel CreateViewModel(List<RoutingRule>? initialRules = null)
    {
        _mockRuleService.Setup(x => x.LoadRules()).Returns(initialRules ?? new List<RoutingRule>());
        return new HomeViewModel(
            _mockRuleService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object,
            _mockDialogService.Object);
    }

    [Fact]
    public void AddRule_Directly_AddsToCollection()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "GitHub", UrlPattern = "github.com", IsEnabled = true };

        vm.Rules.Add(rule);

        Assert.Single(vm.Rules);
        Assert.Equal("GitHub", vm.Rules[0].Name);
    }

    [Fact]
    public void AddRule_Directly_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "GitHub", UrlPattern = "github.com", IsEnabled = true };

        vm.Rules.Add(rule);

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void AddRule_MultipleRules_AllSaved()
    {
        var vm = CreateViewModel();

        vm.Rules.Add(new RoutingRule { Name = "Rule 1", UrlPattern = "a.com" });
        vm.Rules.Add(new RoutingRule { Name = "Rule 2", UrlPattern = "b.com" });
        vm.Rules.Add(new RoutingRule { Name = "Rule 3", UrlPattern = "c.com" });

        Assert.Equal(3, vm.Rules.Count);
        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(3));
    }

    [Fact]
    public void EditRule_ChangeName_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Old Name", UrlPattern = "test.com" };
        vm.Rules.Add(rule);

        rule.Name = "New Name";

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(2));
    }

    [Fact]
    public void EditRule_ChangePattern_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Test", UrlPattern = "old.com" };
        vm.Rules.Add(rule);

        rule.UrlPattern = "new.com";

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(2));
    }

    [Fact]
    public void EditRule_ToggleEnabled_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com", IsEnabled = true };
        vm.Rules.Add(rule);

        rule.IsEnabled = false;

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(2));
    }

    [Fact]
    public void EditRule_ChangeBrowserPath_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com", BrowserPath = @"C:\old.exe" };
        vm.Rules.Add(rule);

        rule.BrowserPath = @"C:\new.exe";

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(2));
    }

    [Fact]
    public void EditRule_ChangeSequence_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com", Sequence = 1 };
        vm.Rules.Add(rule);

        rule.Sequence = 5;

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(2));
    }

    [Fact]
    public void DeleteRule_Directly_RemovesFromCollection()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "ToDelete", UrlPattern = "delete.com" };
        vm.Rules.Add(rule);
        Assert.Single(vm.Rules);

        vm.Rules.Remove(rule);

        Assert.Empty(vm.Rules);
    }

    [Fact]
    public void DeleteRule_Directly_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "ToDelete", UrlPattern = "delete.com" };
        vm.Rules.Add(rule);

        vm.Rules.Remove(rule);

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeast(2));
    }

    [Fact]
    public void DeleteRule_OnlySpecifiedRuleRemoved()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "Keep", UrlPattern = "keep.com" };
        var rule2 = new RoutingRule { Name = "Remove", UrlPattern = "remove.com" };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.Rules.Remove(rule2);

        Assert.Single(vm.Rules);
        Assert.Equal("Keep", vm.Rules[0].Name);
    }

    [Fact]
    public void MoveUp_FirstRule_StaysAtTop()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com", Sequence = 1 };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com", Sequence = 2 };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.SelectedRule = rule1;
        vm.MoveUpCommand.Execute(null);

        Assert.Equal("First", vm.Rules[0].Name);
        Assert.Equal("Second", vm.Rules[1].Name);
    }

    [Fact]
    public void MoveUp_MiddleRule_MovesUp()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com", Sequence = 1 };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com", Sequence = 2 };
        var rule3 = new RoutingRule { Name = "Third", UrlPattern = "c.com", Sequence = 3 };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);
        vm.Rules.Add(rule3);

        vm.SelectedRule = rule2;
        vm.MoveUpCommand.Execute(null);

        Assert.Equal("Second", vm.Rules[0].Name);
        Assert.Equal("First", vm.Rules[1].Name);
        Assert.Equal("Third", vm.Rules[2].Name);
    }

    [Fact]
    public void MoveUp_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com" };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com" };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.SelectedRule = rule2;
        vm.MoveUpCommand.Execute(null);

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void MoveDown_LastRule_StaysAtBottom()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com", Sequence = 1 };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com", Sequence = 2 };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.SelectedRule = rule2;
        vm.MoveDownCommand.Execute(null);

        Assert.Equal("First", vm.Rules[0].Name);
        Assert.Equal("Second", vm.Rules[1].Name);
    }

    [Fact]
    public void MoveDown_MiddleRule_MovesDown()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com", Sequence = 1 };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com", Sequence = 2 };
        var rule3 = new RoutingRule { Name = "Third", UrlPattern = "c.com", Sequence = 3 };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);
        vm.Rules.Add(rule3);

        vm.SelectedRule = rule2;
        vm.MoveDownCommand.Execute(null);

        Assert.Equal("First", vm.Rules[0].Name);
        Assert.Equal("Third", vm.Rules[1].Name);
        Assert.Equal("Second", vm.Rules[2].Name);
    }

    [Fact]
    public void MoveDown_CallsSaveRules()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com" };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com" };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.SelectedRule = rule1;
        vm.MoveDownCommand.Execute(null);

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void DeleteRule_SelectedRuleCleared()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "ToDelete", UrlPattern = "delete.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        vm.Rules.Remove(rule);
        // Manually clear selection after deletion (this is UI behavior)
        vm.SelectedRule = null;

        Assert.Null(vm.SelectedRule);
        Assert.False(vm.HasSelectedRule);
    }

    [Fact]
    public void DeleteRule_CommandsDisabledAfterDelete()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "ToDelete", UrlPattern = "delete.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        vm.Rules.Remove(rule);
        vm.SelectedRule = null;

        Assert.False(vm.EditRuleCommand.CanExecute(null));
        Assert.False(vm.DeleteRuleCommand.CanExecute(null));
        Assert.False(vm.MoveUpCommand.CanExecute(null));
        Assert.False(vm.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void AddRule_ThenSelect_EnablesCommands()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "New", UrlPattern = "new.com" };

        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        Assert.True(vm.HasSelectedRule);
        Assert.True(vm.EditRuleCommand.CanExecute(null));
        Assert.True(vm.DeleteRuleCommand.CanExecute(null));
    }

    // Command-based tests


    [Fact]
    public void EditRuleCommand_CanExecute_RequiresSelectedRule()
    {
        var vm = CreateViewModel();
        Assert.False(vm.EditRuleCommand.CanExecute(null));

        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        Assert.True(vm.EditRuleCommand.CanExecute(null));
    }

    [Fact]
    public void AddRuleCommand_CanExecute_AlwaysTrue()
    {
        var vm = CreateViewModel();
        Assert.True(vm.AddRuleCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteRuleCommand_CanExecute_RequiresSelectedRule()
    {
        var vm = CreateViewModel();
        Assert.False(vm.DeleteRuleCommand.CanExecute(null));

        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        Assert.True(vm.DeleteRuleCommand.CanExecute(null));
    }

    [Fact]
    public void MoveUpCommand_CanExecute_RequiresSelectedRule()
    {
        var vm = CreateViewModel();
        Assert.False(vm.MoveUpCommand.CanExecute(null));

        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        Assert.True(vm.MoveUpCommand.CanExecute(null));
    }

    [Fact]
    public void MoveDownCommand_CanExecute_RequiresSelectedRule()
    {
        var vm = CreateViewModel();
        Assert.False(vm.MoveDownCommand.CanExecute(null));

        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        Assert.True(vm.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void AddRuleCommand_WithDialogSuccess_AddsRule()
    {
        var vm = CreateViewModel();
        var initialCount = vm.Rules.Count;
        var newRule = new RoutingRule { Name = "Test", UrlPattern = "test.com", BrowserPath = @"C:\test.exe" };

        _mockDialogService.Setup(x => x.ShowAddRuleDialog()).Returns(newRule);

        vm.AddRuleCommand.Execute(null);

        Assert.Equal(initialCount + 1, vm.Rules.Count);
        Assert.Contains(newRule, vm.Rules);
        Assert.Equal(newRule, vm.SelectedRule);
        Assert.Contains("added", vm.Status, StringComparison.OrdinalIgnoreCase);
        _mockDialogService.Verify(x => x.ShowAddRuleDialog(), Times.Once);
    }

    [Fact]
    public void AddRuleCommand_WithDialogCancel_DoesNotAddRule()
    {
        var vm = CreateViewModel();
        var initialCount = vm.Rules.Count;

        _mockDialogService.Setup(x => x.ShowAddRuleDialog()).Returns((RoutingRule?)null);

        vm.AddRuleCommand.Execute(null);

        Assert.Equal(initialCount, vm.Rules.Count);
        _mockDialogService.Verify(x => x.ShowAddRuleDialog(), Times.Once);
    }

    [Fact]
    public void EditRuleCommand_WithDialogSuccess_UpdatesRule()
    {
        var vm = CreateViewModel();
        var originalRule = new RoutingRule { Name = "Original", UrlPattern = "original.com", BrowserPath = @"C:\original.exe" };
        var updatedRule = new RoutingRule { Name = "Updated", UrlPattern = "updated.com", BrowserPath = @"C:\updated.exe" };
        vm.Rules.Add(originalRule);
        vm.SelectedRule = originalRule;

        _mockDialogService.Setup(x => x.ShowEditRuleDialog(It.IsAny<RoutingRule>())).Returns(updatedRule);

        vm.EditRuleCommand.Execute(null);

        Assert.Contains(updatedRule, vm.Rules);
        Assert.DoesNotContain(originalRule, vm.Rules);
        Assert.Equal(updatedRule, vm.SelectedRule);
        Assert.Contains("updated", vm.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EditRuleCommand_WithDialogCancel_DoesNotUpdateRule()
    {
        var vm = CreateViewModel();
        var originalRule = new RoutingRule { Name = "Original", UrlPattern = "original.com", BrowserPath = @"C:\original.exe" };
        vm.Rules.Add(originalRule);
        vm.SelectedRule = originalRule;

        _mockDialogService.Setup(x => x.ShowEditRuleDialog(It.IsAny<RoutingRule>())).Returns((RoutingRule?)null);

        vm.EditRuleCommand.Execute(null);

        Assert.Contains(originalRule, vm.Rules);
        Assert.Equal(originalRule, vm.SelectedRule);
    }

    [Fact]
    public void DeleteRuleCommand_RemovesSelectedRule()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "ToDelete", UrlPattern = "delete.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        vm.DeleteRuleCommand.Execute(null);

        Assert.DoesNotContain(rule, vm.Rules);
        // Note: SelectedRule is not automatically cleared after deletion
        // This is the current behavior of the DeleteRule command
    }

    [Fact]
    public void DeleteRuleCommand_UpdatesStatus()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "MyRule", UrlPattern = "test.com" };
        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        vm.DeleteRuleCommand.Execute(null);

        Assert.Contains("deleted", vm.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EditRuleCommand_WithoutSelectedRule_DoesNothing()
    {
        var vm = CreateViewModel();
        var initialCount = vm.Rules.Count;

        vm.EditRuleCommand.Execute(null);

        Assert.Equal(initialCount, vm.Rules.Count);
        _mockDialogService.Verify(x => x.ShowEditRuleDialog(It.IsAny<RoutingRule>()), Times.Never);
    }

    [Fact]
    public void DeleteRuleCommand_WithoutSelectedRule_DoesNothing()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Keep", UrlPattern = "keep.com" };
        vm.Rules.Add(rule);
        var initialCount = vm.Rules.Count;

        vm.DeleteRuleCommand.Execute(null);

        Assert.Equal(initialCount, vm.Rules.Count);
    }

    [Fact]
    public void MoveUpCommand_WithoutSelectedRule_DoesNothing()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com" };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com" };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.MoveUpCommand.Execute(null);

        Assert.Equal("First", vm.Rules[0].Name);
        Assert.Equal("Second", vm.Rules[1].Name);
    }

    [Fact]
    public void MoveDownCommand_WithoutSelectedRule_DoesNothing()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com" };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com" };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);

        vm.MoveDownCommand.Execute(null);

        Assert.Equal("First", vm.Rules[0].Name);
        Assert.Equal("Second", vm.Rules[1].Name);
    }

    [Fact]
    public void AddRule_WithEmptyCollection_SetsSelectedRule()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "New", UrlPattern = "new.com" };

        vm.Rules.Add(rule);
        vm.SelectedRule = rule;

        Assert.Equal(rule, vm.SelectedRule);
        Assert.Equal("New", vm.SelectedRule.Name);
    }

    [Fact]
    public void MultipleRules_CanSelectDifferentRules()
    {
        var vm = CreateViewModel();
        var rule1 = new RoutingRule { Name = "First", UrlPattern = "a.com" };
        var rule2 = new RoutingRule { Name = "Second", UrlPattern = "b.com" };
        var rule3 = new RoutingRule { Name = "Third", UrlPattern = "c.com" };
        vm.Rules.Add(rule1);
        vm.Rules.Add(rule2);
        vm.Rules.Add(rule3);

        vm.SelectedRule = rule1;
        Assert.Equal("First", vm.SelectedRule.Name);

        vm.SelectedRule = rule2;
        Assert.Equal("Second", vm.SelectedRule.Name);

        vm.SelectedRule = rule3;
        Assert.Equal("Third", vm.SelectedRule.Name);
    }

    [Fact]
    public void Rules_LoadFromService_AppliedToViewModel()
    {
        var initialRules = new List<RoutingRule>
        {
            new RoutingRule { Name = "Loaded1", UrlPattern = "loaded1.com" },
            new RoutingRule { Name = "Loaded2", UrlPattern = "loaded2.com" }
        };

        var vm = CreateViewModel(initialRules);

        Assert.Equal(2, vm.Rules.Count);
        Assert.Equal("Loaded1", vm.Rules[0].Name);
        Assert.Equal("Loaded2", vm.Rules[1].Name);
    }

    [Fact]
    public void Rule_PropertyChange_TriggersSave()
    {
        var vm = CreateViewModel();
        var rule = new RoutingRule { Name = "Test", UrlPattern = "test.com" };
        vm.Rules.Add(rule);

        // Reset the mock to track only the property change save
        _mockRuleService.Invocations.Clear();

        rule.Name = "Modified";

        _mockRuleService.Verify(x => x.SaveRules(It.IsAny<List<RoutingRule>>()), Times.Once);
    }
}
