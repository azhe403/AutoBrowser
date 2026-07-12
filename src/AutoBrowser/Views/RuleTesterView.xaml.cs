using System.Windows;
using AutoBrowser.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoBrowser.Views;

public partial class RuleTesterView : FluentWindow
{
    private static readonly SettingsService _settingsService = new();

    public string? Result { get; private set; }

    public RuleTesterView(string title, string prompt, string defaultValue = "")
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;

        var settings = _settingsService.LoadSettings();
        InputBox.Text = string.IsNullOrWhiteSpace(defaultValue) ? settings.LastTestUrl : defaultValue;

        Owner = Application.Current.MainWindow;
        InputBox.Focus();
        InputBox.SelectAll();
        InputBox.TextChanged += (_, _) => UpdateValidation();
        UpdateValidation();
    }

    private void UpdateValidation()
    {
        var url = InputBox.Text?.Trim() ?? "";
        var isValid = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                      && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        OkButton.IsEnabled = isValid;
        ValidationText.Text = isValid ? "" : "Enter a valid URL (e.g. https://example.com)";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result = InputBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(Result))
        {
            var settings = _settingsService.LoadSettings();
            settings.LastTestUrl = Result;
            _settingsService.SaveSettings(settings);
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
