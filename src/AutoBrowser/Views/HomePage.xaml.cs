using System.Windows.Controls;
using AutoBrowser.ViewModels;

namespace AutoBrowser.Views;

public partial class HomePage : Page
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
