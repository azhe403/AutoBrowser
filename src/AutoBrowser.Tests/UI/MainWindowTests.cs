using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Xunit;

namespace AutoBrowser.Tests.UI;

public class MainWindowTests : IDisposable
{
    private readonly AppLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    private Window GetRoutingRuleWindow(FlaUI.Core.Application app, Window mainWindow)
    {
        var child = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Window).And(cf.ByName("Routing Rule")))?.AsWindow();
        if (child != null)
        {
            return child;
        }

        var top = app.GetAllTopLevelWindows(_launcher.Automation)
            .FirstOrDefault(w => w.Name.Contains("Routing Rule"));
        if (top != null)
        {
            return top;
        }

        return mainWindow;
    }

    [Fact]
    public void App_Launches_MainWindow_IsVisible()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10)) 
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        Assert.True(mainWindow.IsAvailable);
    }

    [Fact]
    public void MainWindow_HasCorrect_Title()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        Assert.Contains("AutoBrowser", mainWindow.Name);
    }

    [Fact]
    public void MainWindow_ContainsNavigationView()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        var navView = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.List));

        Assert.NotNull(navView);
    }

    [Fact]
    public void MainWindow_HomePage_IsDefault()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var routingRulesText = mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Routing Rules"));

        Assert.NotNull(routingRulesText);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("About")]
    [InlineData("Settings")]
    public void MainWindow_CanNavigateToPage(string pageName)
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var navItem = mainWindow.FindFirstDescendant(cf =>
            cf.ByText(pageName));
        Assert.NotNull(navItem);
        navItem.Click();
        Thread.Sleep(500);

        var pageTitle = mainWindow.FindFirstDescendant(cf =>
            cf.ByText(pageName));
        Assert.NotNull(pageTitle);
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Edit")]
    [InlineData("Delete")]
    [InlineData("Move Up")]
    [InlineData("Move Down")]
    [InlineData("Test URL")]
    [InlineData("Check Update")]
    public void MainWindow_Toolbar_HasButton(string buttonText)
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var button = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText(buttonText)));

        Assert.NotNull(button);
    }

    [Fact]
    public void MainWindow_AddButton_OpensRuleEditor()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var addButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();
        Thread.Sleep(1000);

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);
        var nameBox = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("NameBox"));
        Assert.NotNull(nameBox);
    }

    [Fact]
    public void MainWindow_AddRule_FillForm_Save()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var addButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();
        Thread.Sleep(1000);

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);

        var nameBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("NameBox null");
        nameBox.Text = "Test Rule from UI";

        var patternBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PatternBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("PatternBox null");
        patternBox.Text = "test-example.com";

        var browserPathBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("BrowserPathBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("BrowserPathBox null");
        browserPathBox.Text = @"C:\test\browser.exe";

        var okButton = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(okButton);
        okButton.AsButton().Invoke();
        Thread.Sleep(1500);

        var ruleInList = mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Test Rule from UI"));

        Assert.NotNull(ruleInList);
    }

    [Fact]
    public void MainWindow_EditRule_FillForm_Save()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var addButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();
        Thread.Sleep(1000);

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);

        var nameBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("NameBox null");
        nameBox.Text = "Rule To Edit";

        var patternBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PatternBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("PatternBox null");
        patternBox.Text = "edit-me.com";

        var browserPathBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("BrowserPathBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("BrowserPathBox null");
        browserPathBox.Text = @"C:\test\browser.exe";

        var okButton = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(okButton);
        okButton.AsButton().Invoke();
        Thread.Sleep(1500);

        var addedRule = mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit"));
        Assert.NotNull(addedRule);
        
        // Select the rule
        addedRule.Click();
        Thread.Sleep(500);

        // Click Edit
        var editButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Edit")));
        Assert.NotNull(editButton);
        editButton.AsButton().Invoke();
        Thread.Sleep(1000);

        var editWindow = GetRoutingRuleWindow(app, mainWindow);

        var editNameBox = editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("editNameBox null");
        editNameBox.Text = "Rule To Edit Edited";

        var editOkButton = editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(editOkButton);
        editOkButton.AsButton().Invoke();
        Thread.Sleep(1500);

        var editedRule = mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit Edited"));
        Assert.NotNull(editedRule);
    }

    [Fact]
    public void MainWindow_DeleteRule()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10))
            ?? throw new InvalidOperationException("MainWindow null");
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(1000);

        var addButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();
        Thread.Sleep(1000);

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);

        var nameBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("NameBox null");
        nameBox.Text = "Rule To Delete";

        var patternBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PatternBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("PatternBox null");
        patternBox.Text = "delete-me.com";

        var browserPathBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("BrowserPathBox"))?.AsTextBox()
            ?? throw new InvalidOperationException("BrowserPathBox null");
        browserPathBox.Text = @"C:\test\browser.exe";

        var okButton = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(okButton);
        okButton.AsButton().Invoke();
        Thread.Sleep(1500);

        var addedRule = mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Delete"));
        Assert.NotNull(addedRule);
        
        // Select the rule
        addedRule.Click();
        Thread.Sleep(500);

        // Click Delete
        var deleteButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Delete")));
        Assert.NotNull(deleteButton);
        deleteButton.AsButton().Invoke();
        Thread.Sleep(1500);

        var deletedRule = mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Delete"));
        Assert.Null(deletedRule);
    }
}
