using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Serilog;

namespace AutoBrowser.Helpers;

/// <summary>
/// Win32 helper to force a WPF window to the foreground, even when the app is in the background or tray.
/// </summary>
internal static class WindowForegroundHelper
{
    private const int SwRestore = 9;

    /// <summary>
    /// Restores a window from minimized/hidden state and brings it to the foreground.
    /// </summary>
    /// <param name="window">The WPF window to activate.</param>
    public static void BringToFront(Window window)
    {
        Log.Verbose("BringToFront: ensuring visible and normal state");
        window.Show();
        window.WindowState = window.WindowState == WindowState.Minimized
            ? WindowState.Normal
            : window.WindowState;

        // Flash the window to grab user attention
        window.Topmost = true;
        window.Topmost = false;
        window.Activate();

        // Win32: force to foreground (works even when app is in background/tray)
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            SetForegroundWindow(handle);
            ShowWindow(handle, SwRestore);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
