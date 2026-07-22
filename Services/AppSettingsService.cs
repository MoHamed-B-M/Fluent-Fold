using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace FluentFold.Services;

public sealed class AppSettingsService(ILogger<AppSettingsService> logger) : IAppSettingsService
{
    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

    public bool EnableNotifications
    {
        get => GetBool(nameof(EnableNotifications), true);
        set => SetBool(nameof(EnableNotifications), value);
    }

    public string DefaultOrganizationMode
    {
        get => GetString(nameof(DefaultOrganizationMode), "Move");
        set => SetString(nameof(DefaultOrganizationMode), value);
    }

    public bool KeepFolderStructure
    {
        get => GetBool(nameof(KeepFolderStructure), false);
        set => SetBool(nameof(KeepFolderStructure), value);
    }

    public string ThemePreference
    {
        get => GetString(nameof(ThemePreference), "System");
        set => SetString(nameof(ThemePreference), value);
    }

    public bool ShowTeachingTips
    {
        get => GetBool(nameof(ShowTeachingTips), true);
        set => SetBool(nameof(ShowTeachingTips), value);
    }

    public bool HasShownFirstLaunchTip
    {
        get => GetBool(nameof(HasShownFirstLaunchTip), false);
        set => SetBool(nameof(HasShownFirstLaunchTip), value);
    }

    public string AppMode
    {
        get => GetString(nameof(AppMode), "Pro");
        set => SetString(nameof(AppMode), value);
    }

    public bool HasCompletedFirstLaunch
    {
        get => GetBool(nameof(HasCompletedFirstLaunch), false);
        set => SetBool(nameof(HasCompletedFirstLaunch), value);
    }

    public double WindowWidth
    {
        get => GetDouble(nameof(WindowWidth), 1200);
        set => SetDouble(nameof(WindowWidth), value);
    }

    public double WindowHeight
    {
        get => GetDouble(nameof(WindowHeight), 800);
        set => SetDouble(nameof(WindowHeight), value);
    }

    public double WindowX
    {
        get => GetDouble(nameof(WindowX), -1);
        set => SetDouble(nameof(WindowX), value);
    }

    public double WindowY
    {
        get => GetDouble(nameof(WindowY), -1);
        set => SetDouble(nameof(WindowY), value);
    }

    public bool UseRecycleBin
    {
        get => GetBool(nameof(UseRecycleBin), true);
        set => SetBool(nameof(UseRecycleBin), value);
    }

    public string SortMode
    {
        get => GetString(nameof(SortMode), "Type");
        set => SetString(nameof(SortMode), value);
    }

    private bool GetBool(string key, bool defaultValue)
    {
        var val = _settings.Values[key] as string;
        return val is not null && bool.TryParse(val, out var result) ? result : defaultValue;
    }

    private void SetBool(string key, bool value)
    {
        _settings.Values[key] = value.ToString();
        logger.LogDebug("Setting '{Key}' = {Value}", key, value);
    }

    private string GetString(string key, string defaultValue) => _settings.Values[key] as string ?? defaultValue;

    private void SetString(string key, string value)
    {
        _settings.Values[key] = value;
        logger.LogDebug("Setting '{Key}' = {Value}", key, value);
    }

    private double GetDouble(string key, double defaultValue)
    {
        var val = _settings.Values[key] as string;
        return val is not null && double.TryParse(val, out var result) ? result : defaultValue;
    }

    private void SetDouble(string key, double value)
    {
        _settings.Values[key] = value.ToString("F0");
        logger.LogDebug("Setting '{Key}' = {Value}", key, value);
    }
}
