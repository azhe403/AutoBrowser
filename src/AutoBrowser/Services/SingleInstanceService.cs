using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Threading;
using Serilog;

namespace AutoBrowser.Services;

/// <summary>
/// Uses a named pipe to signal an already-running instance to come to foreground.
/// Protocol: second instance connects, sends a UTF-8 line ("SHOW" or "SHOW|<url>"), then disconnects.
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    private const string PipeName = "AutoBrowser-SingleInstancePipe";

    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    /// <summary>
    /// Starts the pipe server on a background thread. Invoke on the first (only) instance.
    /// </summary>
    /// <param name="onActivate">Called on the UI thread with an optional URL when a second instance signals.</param>
    /// <param name="dispatcher">The WPF dispatcher to marshal the callback onto.</param>
    public void StartServer(Action<string?> onActivate, Dispatcher dispatcher)
    {
        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(async () =>
        {
            Log.Debug("SingleInstance pipe server started");

            while (!_cts.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    Log.Verbose("Waiting for second instance connection...");
                    await server.WaitForConnectionAsync(_cts.Token);

                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var message = await reader.ReadLineAsync(_cts.Token);

                    Log.Information("Second instance sent message: {Message}", message);

                    var url = ParseUrlFromMessage(message);

                    // Marshal back to UI thread (fire-and-forget)
                    _ = dispatcher.BeginInvoke(() => onActivate(url));
                }
                catch (OperationCanceledException)
                {
                    break; // App is shutting down
                }
                catch (IOException)
                {
                    // Pipe was broken or disposed — recreate on next iteration
                    Log.Verbose("Pipe disconnected, will recreate");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Pipe server error");
                    break;
                }
                finally
                {
                    if (server is not null)
                        await server.DisposeAsync().ConfigureAwait(false);
                }

                // Brief pause so the OS fully releases the pipe handle
                try { await Task.Delay(100, _cts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }

            Log.Debug("SingleInstance pipe server stopped");
        });
    }

    /// <summary>
    /// Called by the second instance to signal the first. Returns true if a running instance was found.
    /// </summary>
    public static bool SignalExistingInstance(string? url = null)
    {
        try
        {
            var message = string.IsNullOrEmpty(url) ? "SHOW" : $"SHOW|{url}";

            using var client = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.Out,
                PipeOptions.None);

            // Short timeout — if the pipe server isn't running, there's no existing instance
            client.Connect(2000);

            using var writer = new StreamWriter(client, Encoding.UTF8);
            writer.WriteLine(message);
            writer.Flush();

            Log.Debug("Signaled existing instance: {Message}", message);
            return true;
        }
        catch (TimeoutException)
        {
            Log.Debug("No existing instance found (pipe timeout)");
            return false;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No existing instance found (pipe error)");
            return false;
        }
    }

    private static string? ParseUrlFromMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return null;

        // Format: "SHOW" or "SHOW|<url>"
        var parts = message.Split('|', 2);
        if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            return parts[1].Trim();

        return null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
