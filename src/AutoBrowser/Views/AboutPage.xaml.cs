using System.Windows.Controls;
using AutoBrowser.ViewModels;

namespace AutoBrowser.Views;

public partial class AboutPage : Page
{
    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
