namespace AutoBrowser.ViewModels;

public partial class InputDialog : System.Windows.Window
{
    public string? Result { get; private set; }

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
        Owner = System.Windows.Application.Current.MainWindow;
        InputBox.Focus();
        InputBox.SelectAll();
    }

    private void Ok_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Result = InputBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
