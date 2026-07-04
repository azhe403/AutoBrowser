using Microsoft.Win32;

namespace AutoBrowser.Services;

public class ProtocolService : IProtocolService
{
    private const string AppName = "AutoBrowser";
    private const string ProtocolName = "autobrowser";

    public bool RegisterProtocolHandler()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            using var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{ProtocolName}");
            key.SetValue("", $"URL:{AppName} Protocol");
            key.SetValue("URL Protocol", "");

            using var commandKey = key.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UnregisterProtocolHandler()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProtocolName}", false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsProtocolRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Classes\{ProtocolName}");
            return key != null;
        }
        catch
        {
            return false;
        }
    }
}
