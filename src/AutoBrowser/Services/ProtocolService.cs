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

    public string? GetRegisteredPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProtocolName}\shell\open\command");
            var cmd = key?.GetValue("") as string;
            if (string.IsNullOrEmpty(cmd)) return null;

            // Extract path from: "path" "%1"
            cmd = cmd.Trim();
            if (cmd.StartsWith('"'))
            {
                var end = cmd.IndexOf('"', 1);
                return end > 0 ? cmd[1..end] : null;
            }
            var space = cmd.IndexOf(' ');
            return space > 0 ? cmd[..space] : cmd;
        }
        catch
        {
            return null;
        }
    }
}
