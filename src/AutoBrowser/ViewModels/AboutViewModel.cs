using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace AutoBrowser.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    
    [ObservableProperty]
    private string _status = "Ready";

    [RelayCommand]
    private void OpenUrl(string url)
    {
        try
        {
            Log.Information("Opening URL: {Url}", url);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open URL: {Url}", url);
        }
    }
}