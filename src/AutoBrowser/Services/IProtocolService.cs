namespace AutoBrowser.Services;

public interface IProtocolService
{
    bool RegisterProtocolHandler();
    bool UnregisterProtocolHandler();
    bool IsProtocolRegistered();
    string? GetRegisteredPath();
}
