namespace FluentFold.Services;

public sealed class UpdateInfo
{
    public bool IsAvailable { get; init; }
    public string LatestVersion { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string ReleaseUrl { get; init; } = "";
    public string Changelog { get; init; } = "";
}

public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync();
}
