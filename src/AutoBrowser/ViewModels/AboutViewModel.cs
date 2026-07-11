using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace AutoBrowser.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    
    [ObservableProperty]
    private string _status = "Ready";

    [RelayCommand]
    private void OpenUrl(string url)
    {
        try
        {
            Log.Information("Opening URL: {Url}", url);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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