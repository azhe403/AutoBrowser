using AutoBrowser.Services;

namespace AutoBrowser;

public partial class App : System.Windows.Application
{
    private static readonly string MutexName = "AutoBrowser-SingleInstance";

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0)
        {
            var url = e.Args[0];
            if (url.StartsWith("autobrowser:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var interceptor = new UrlInterceptorService(new ConfigurationService());
                if (interceptor.TryRoute(url))
                {
                    Shutdown();
                    return;
                }
            }
        }

        using var mutex = new System.Threading.Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show("AutoBrowser is already running in the system tray.",
                "AutoBrowser", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
