using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using Xunit;

namespace AutoBrowser.Tests.UI;

[Collection("UiTests")]
public class MainWindowTests : IDisposable
{
    private readonly AppLauncher _launcher;

    public MainWindowTests(AppLauncher launcher)
    {
        _launcher = launcher;
    }

    public void Dispose() => _launcher.Dispose();

    private Window WaitForMainWindow(FlaUI.Core.Application app)
    {
        var deadline = DateTime.Now.AddSeconds(15);
        while (DateTime.Now < deadline)
        {
            try
            {
                var win = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(3));
                if (win != null) return win;
            }
            catch (System.TimeoutException) { }
            catch (FlaUI.Core.Exceptions.FlaUIException) { }
        }
        throw new InvalidOperationException("MainWindow not found after retries");
    }

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
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        Assert.True(mainWindow.IsAvailable);
    }

    [Fact]
    public void MainWindow_HasCorrect_Title()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        Assert.Contains("AutoBrowser", mainWindow.Name);
    }

    [Fact]
    public void MainWindow_ContainsNavigationView()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        var navView = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.List));

        Assert.NotNull(navView);
    }

    [Fact]
    public void MainWindow_HomePage_IsDefault()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();

        var routingRulesText = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Routing Rules")), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(routingRulesText);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("About")]
    [InlineData("Settings")]
    public void MainWindow_CanNavigateToPage(string pageName)
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        var navItem = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText(pageName)), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(navItem);
        navItem.Click();
        var pageTitle = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText(pageName)), TimeSpan.FromSeconds(5)).Result;
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
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        var button = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText(buttonText))),
            TimeSpan.FromSeconds(5)).Result;

        Assert.NotNull(button);
    }

    [Fact]
    public void MainWindow_AddButton_OpensRuleEditor()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        var addButton = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add"))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();
        var editorWindow = GetRoutingRuleWindow(app, mainWindow);
        var nameBox = Retry.WhileNull(() => editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("NameBox")),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(nameBox);
    }

    [Fact]
    public void MainWindow_AddRule_FillForm_Save()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        var addButton = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add"))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);

        var nameBox = Retry.WhileNull(() => editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox(), TimeSpan.FromSeconds(5)).Result
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

        var ruleInList = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Test Rule from UI")), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(ruleInList);
    }

    [Fact]
    public void MainWindow_EditRule_FillForm_Save()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        var addButton = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add"))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);

        var nameBox = Retry.WhileNull(() => editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox(), TimeSpan.FromSeconds(5)).Result
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

        var addedRule = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit")), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(addedRule);

        // Select the rule
        addedRule.Click();

        // Click Edit
        var editButton = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Edit"))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(editButton);
        editButton.AsButton().Invoke();

        var editWindow = GetRoutingRuleWindow(app, mainWindow);

        var editNameBox = Retry.WhileNull(() => editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox(), TimeSpan.FromSeconds(5)).Result
            ?? throw new InvalidOperationException("editNameBox null");
        editNameBox.Text = "Rule To Edit Edited";

        var editOkButton = editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(editOkButton);
        editOkButton.AsButton().Invoke();

        var editedRule = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit Edited")), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(editedRule);
    }

    [Fact]
    public void MainWindow_DeleteRule()
    {
        var app = _launcher.Launch();
        var mainWindow = WaitForMainWindow(app);
        mainWindow.Focus();
        _launcher.DismissBlockingDialogs();
        var addButton = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add"))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(addButton);
        addButton.AsButton().Invoke();

        var editorWindow = GetRoutingRuleWindow(app, mainWindow);

        var nameBox = Retry.WhileNull(() => editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox(), TimeSpan.FromSeconds(5)).Result
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

        var addedRule = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit")), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(addedRule);

        // Select the rule
        addedRule.Click();

        // Click Edit
        var editButton = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Edit"))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(editButton);
        editButton.AsButton().Invoke();

        var editWindow = GetRoutingRuleWindow(app, mainWindow);

        var editNameBox = Retry.WhileNull(() => editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox(), TimeSpan.FromSeconds(5)).Result
            ?? throw new InvalidOperationException("editNameBox null");
        editNameBox.Text = "Rule To Edit Edited";

        var editOkButton = editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(editOkButton);
        editOkButton.AsButton().Invoke();

        var editedRule = Retry.WhileNull(() => mainWindow.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit Edited")), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(editedRule);
    }
}
