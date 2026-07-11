using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions;
using AutoBrowser.ViewModels;
using AutoBrowser.Views;

namespace AutoBrowser;

public partial class MainWindow : FluentWindow
{
    public MainWindow(INavigationViewPageProvider pageProvider, MainViewModel viewModel)
    {
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        InitializeComponent();
        DataContext = viewModel;

        RootNavigation.SetPageProviderService(pageProvider);
        RootNavigation.Navigated += (s, e) =>
        {
            if (e.Page is System.Windows.Controls.Page page)
            {
                StatusView.DataContext = page.DataContext;
            }
        };
        Loaded += (s, e) => RootNavigation.Navigate(typeof(HomePage));
    }
}
