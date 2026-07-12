using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Windows;
using Serilog;

namespace AutoBrowser.Services;

public class UpdateService
{
    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "AutoBrowser" } }
    };

    private const string Repo = "azhe403/AutoBrowser";
    private const string ReleasesUrl = $"https://api.github.com/repos/{Repo}/releases?per_page=10";

    public async Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            Log.Information("Checking for updates from {Url}", ReleasesUrl);
            var resp = await Http.GetAsync(ReleasesUrl, ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning("Update check: 404 Not Found");
                return null;
            }

            resp.EnsureSuccessStatusCode();

            var docs = await resp.Content.ReadFromJsonAsync<List<GitHubRelease>>(ct);
            if (docs is null || docs.Count == 0)
            {
                Log.Warning("Update check: no releases found");
                return null;
            }

            Log.Debug("Found {Count} releases", docs.Count);

            ReleaseInfo? best = null;
            Version? bestVer = null;

            foreach (var doc in docs)
            {
                var tag = doc.TagName?.TrimStart('v');
                if (!Version.TryParse(tag, out var ver)) continue;

                if (bestVer is null || ver > bestVer || (ver == bestVer && !doc.Prerelease && best!.IsPreRelease))
                {
                    var assets = doc.Assets?.Select(a => new AssetInfo(a.Name ?? "unknown", a.BrowserDownloadUrl ?? "")).ToList() ?? [];
                    best = new ReleaseInfo(ver, false, doc.Prerelease, doc.HtmlUrl ?? "", assets);
                    bestVer = ver;
                }
            }

            if (best is null)
            {
                Log.Warning("Update check: no valid version tags found");
                return null;
            }

            var current = typeof(UpdateService).Assembly.GetName().Version;
            best = best with { IsNewer = current is null || best.Version > current };
            Log.Information("Latest: v{Version}, Current: v{Current}, IsNewer: {IsNewer}", best.Version, current, best.IsNewer);
            return best;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CheckForUpdateAsync failed");
            return null;
        }
    }

    public async Task DownloadAndUpdateAsync(ReleaseInfo release, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var asset = SelectAsset(release.Assets);
        if (asset is null)
            throw new InvalidOperationException("No suitable update asset found.");

        var appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var currentPid = Environment.ProcessId;
        var workspace = Path.Combine(Path.GetTempPath(), "AutoBrowserUpdate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        var zipPath = Path.Combine(workspace, "update.zip");
        using (var resp = await Http.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1;
            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var file = File.Create(zipPath);
            var buffer = new byte[81920];
            long read = 0;
            int bytes;
            while ((bytes = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, bytes), ct);
                read += bytes;
                if (total > 0)
                    progress?.Report((double)read / total);
            }
        }

        var extractPath = Path.Combine(workspace, "extracted");
        ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

        var updaterExe = Path.Combine(appDir, "AutoUpdater.exe");
        if (!File.Exists(updaterExe))
            throw new FileNotFoundException("AutoUpdater.exe not found next to the app. Use the official release package.");

        var runnerRoot = Path.Combine(workspace, "runner");
        Directory.CreateDirectory(runnerRoot);
        foreach (var f in Directory.EnumerateFiles(appDir, "AutoUpdater*"))
            File.Copy(f, Path.Combine(runnerRoot, Path.GetFileName(f)), overwrite: true);

        TryRemoveZoneIdentifier(Path.Combine(runnerRoot, "AutoUpdater.exe"));

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(runnerRoot, "AutoUpdater.exe"),
            Arguments = $"--pid {currentPid} --source \"{extractPath}\" --target \"{appDir}\" --exe \"{Path.Combine(appDir, "AutoBrowser.exe")}\" --workspace \"{workspace}\"",
            UseShellExecute = true,
        };

        Process.Start(startInfo);
        Application.Current.Shutdown();
    }

    private static AssetInfo? SelectAsset(List<AssetInfo> assets)
    {
        var isSelfContained = File.Exists(Path.Combine(AppContext.BaseDirectory, "coreclr.dll"));

        var suffix = isSelfContained ? "self-contained" : "framework-dependent";

        return assets.FirstOrDefault(a => a.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase))
               ?? assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    private static void TryRemoveZoneIdentifier(string path)
    {
        try
        {
            File.Delete(path + ":Zone.Identifier");
        }
        catch
        {
        }
    }
}

public record ReleaseInfo(Version Version, bool IsNewer, bool IsPreRelease, string PageUrl, List<AssetInfo> Assets);

public record AssetInfo(string Name, string DownloadUrl);

internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

internal class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}