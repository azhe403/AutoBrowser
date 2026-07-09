using Wpf.Ui.Controls;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.ViewModels;

namespace AutoBrowser;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }
}
