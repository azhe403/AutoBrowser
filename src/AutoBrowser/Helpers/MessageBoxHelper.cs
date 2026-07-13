namespace AutoBrowser.Helpers;

internal static class MessageBoxHelper
{
    public static async Task ShowAsync(string title, string content)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = content
        };
        await dialog.ShowDialogAsync();
    }

    public static async Task<bool> ShowConfirmAsync(string title, string content, string primaryText = "Yes", string secondaryText = "No")
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            SecondaryButtonText = secondaryText,
            IsCloseButtonEnabled = false
        };
        var result = await dialog.ShowDialogAsync();
        return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
    }
}
