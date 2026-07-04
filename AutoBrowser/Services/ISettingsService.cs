using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}
