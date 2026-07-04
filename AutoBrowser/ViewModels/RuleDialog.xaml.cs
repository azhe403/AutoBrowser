using System.Windows;
using AutoBrowser.Models;

namespace AutoBrowser.ViewModels;

public partial class RuleDialog : System.Windows.Window
{
    public RoutingRule Rule { get; private set; }

    public RuleDialog()
    {
        InitializeComponent();
        Rule = new RoutingRule();
        Owner = System.Windows.Application.Current.MainWindow;

        var browsers = BrowserDefinition.GetKnownBrowsers();
        BrowserCombo.ItemsSource = browsers;
        if (browsers.Count > 0)
            BrowserCombo.SelectedIndex = 0;
    }

    public RuleDialog(RoutingRule existing) : this()
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

    private void BrowserCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
