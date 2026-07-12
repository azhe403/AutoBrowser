using System.Reflection;
using System.Windows;
using AutoBrowser.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace AutoBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly UpdateService _updateService = new();

    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    public string WindowTitle => $"AutoBrowser v{AppVersion} - URL Router";

    public MainViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task StartSilentUpdateCheckAsync(bool force = false)
    {
        var settings = _settingsService.LoadSettings();
        var elapsed = DateTime.Now - settings.LastUpdateCheckTime;
        if (!force && elapsed < TimeSpan.FromHours(1))
        {
            Log.Debug("Silent update check skipped, last check was {Elapsed} ago", elapsed);
            return;
        }

        await CheckForUpdateSilentAsync();
    }

    private async Task CheckForUpdateSilentAsync()
    {
        try
        {
            Log.Information("Silent update check starting");
            var release = await _updateService.CheckForUpdateAsync();

            var settings = _settingsService.LoadSettings();
            settings.LastUpdateCheckTime = DateTime.Now;
            _settingsService.SaveSettings(settings);

            if (release is null || !release.IsNewer)
            {
                Log.Debug("Silent update check: no update available");
                return;
            }
            Log.Information("Silent update check: v{Version} available, showing dialog", release.Version);
            await ShowUpdateDialogAsync(release);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Silent update check failed");
        }
    }

    private async Task ShowUpdateDialogAsync(ReleaseInfo release)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Update Available",
            Content = $"Version {release.Version} is available.\n\nCurrent: {typeof(UpdateService).Assembly.GetName().Version}\n\nDownload and install?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            Width = 500,
            MinWidth = 500
        };
        dialog.Owner = Application.Current.MainWindow;
        var result = await dialog.ShowDialogAsync();
        if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded)
        {
            Application.Current.MainWindow.Focus();
        }

        Log.Information("Update dialog result: {Result} for v{Version}", result, release.Version);

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

        var progress = new Progress<double>(p =>
        {
            var pct = (int)(p * 100);
            Log.Debug("Silent download progress: {Pct}%", pct);
        });

        await _updateService.DownloadAndUpdateAsync(release, progress);
    }
}