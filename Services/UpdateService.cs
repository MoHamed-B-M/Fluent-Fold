using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FluentFold.Services;

public sealed class UpdateService : IUpdateService, IDisposable
{
    private const string Owner = "MoHamed-B-M";
    private const string Repo = "Fluent-Fold";
    private static readonly Uri LatestReleaseUri = new($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
    private static readonly Uri ReleasesUri = new($"https://github.com/{Owner}/{Repo}/releases");

    private readonly HttpClient _http;
    private readonly ILogger<UpdateService> _logger;
    private bool _disposed;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"FluentFold/{GetCurrentVersion()}");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var current = GetCurrentVersion();
        try
        {
            var release = await _http.GetFromJsonAsync<GitHubRelease>(LatestReleaseUri);
            if (release is null || string.IsNullOrEmpty(release.TagName))
                return NoUpdate(current);

            var latest = ParseTagVersion(release.TagName);
            if (string.IsNullOrEmpty(latest))
                return NoUpdate(current);

            var isNewer = CompareVersions(latest, current) > 0;
            return new UpdateInfo
            {
                IsAvailable = isNewer,
                LatestVersion = latest,
                CurrentVersion = current,
                ReleaseUrl = release.HtmlUrl ?? ReleasesUri.ToString(),
                Changelog = release.Body ?? ""
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error checking for updates");
            return NoUpdate(current);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Update check timed out");
            return NoUpdate(current);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return NoUpdate(current);
        }
    }

    private static UpdateInfo NoUpdate(string current) => new()
    {
        IsAvailable = false,
        CurrentVersion = current,
        LatestVersion = current,
        ReleaseUrl = ReleasesUri.ToString()
    };

    private static string GetCurrentVersion()
    {
        try
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver is not null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    private static string ParseTagVersion(string tag)
    {
        var v = tag.TrimStart('v', 'V');
        var parts = v.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}.{(parts.Length >= 3 ? parts[2] : "0")}" : v;
    }

    private static int CompareVersions(string a, string b)
    {
        var ap = a.Split('.');
        var bp = b.Split('.');
        for (int i = 0; i < Math.Max(ap.Length, bp.Length); i++)
        {
            int an = i < ap.Length && int.TryParse(ap[i], out var x) ? x : 0;
            int bn = i < bp.Length && int.TryParse(bp[i], out var y) ? y : 0;
            if (an != bn) return an.CompareTo(bn);
        }
        return 0;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}
