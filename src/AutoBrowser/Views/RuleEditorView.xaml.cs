using System.Windows;
using System.Windows.Controls;
using AutoBrowser.Models;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser.Views;

public partial class RuleEditorView : FluentWindow
{
    public RoutingRule Rule { get; private set; }

    public RuleEditorView()
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        Rule = new RoutingRule();
        Owner = Application.Current.MainWindow;

        var browsers = BrowserDefinition.GetKnownBrowsers();
        BrowserCombo.ItemsSource = browsers;
        if (browsers.Count > 0)
            BrowserCombo.SelectedIndex = 0;

        Loaded += (_, _) => BrowserPathBox.Focus();
    }

    public RuleEditorView(RoutingRule existing) : this()
    {
        NameBox.Text = existing.Name;
        PatternBox.Text = existing.UrlPattern;
        BrowserPathBox.Text = existing.BrowserPath;

        var browsers = BrowserCombo.ItemsSource as List<BrowserDefinition>;
        var match = browsers?.FirstOrDefault(b =>
            b.ExecutablePath.Equals(existing.BrowserPath, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            BrowserCombo.SelectedItem = match;
    }

    private void BrowserCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrowserCombo.SelectedItem is BrowserDefinition browser)
            BrowserPathBox.Text = browser.ExecutablePath;
    }

    private void BrowseBrowser(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Browser Executable"
        };

        if (dialog.ShowDialog() == true)
            BrowserPathBox.Text = dialog.FileName;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) ||
            string.IsNullOrWhiteSpace(PatternBox.Text) ||
            string.IsNullOrWhiteSpace(BrowserPathBox.Text))
        {
            System.Windows.MessageBox.Show("Name, URL pattern, and browser are required.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (isValid, error) = RoutingRule.ValidatePattern(PatternBox.Text.Trim());
        if (!isValid)
        {
            System.Windows.MessageBox.Show(error, "Invalid URL Pattern",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Rule.Name = NameBox.Text.Trim();
        Rule.UrlPattern = PatternBox.Text.Trim();
        Rule.BrowserPath = BrowserPathBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
