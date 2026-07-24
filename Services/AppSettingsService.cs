using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FluentFold.Services;

public sealed class AppSettingsService : IAppSettingsService, IDisposable
{
    private readonly string _filePath;
    private readonly ILogger<AppSettingsService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, string?> _values;
    private bool _loaded;
    private readonly FileSystemWatcher? _watcher;
    private bool _disposed;

    public AppSettingsService(ILogger<AppSettingsService> logger)
    {
        _logger = logger;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentFold");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        _values = [];

        try
        {
            _watcher = new FileSystemWatcher(dir, "settings.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnSettingsFileChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create settings file watcher");
        }
    }

    private void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed) return;
        try
        {
            _lock.Wait();
            _loaded = false;
        }
        finally { _lock.Release(); }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await _lock.WaitAsync();
        try
        {
            if (_loaded) return;
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                _values = JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? [];
            }
            _loaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            _values = [];
            _loaded = true;
        }
        finally { _lock.Release(); }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _lock.Wait();
        try
        {
            if (_loaded) return;
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _values = JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? [];
            }
            _loaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            _values = [];
            _loaded = true;
        }
        finally { _lock.Release(); }
    }

    private async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
        finally { _lock.Release(); }
    }

    private void Save()
    {
        _lock.Wait();
        try
        {
            var json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
        finally { _lock.Release(); }
    }

    private T Get<T>(string key, T defaultValue)
    {
        EnsureLoaded();
        _lock.Wait();
        try
        {
            if (_values.TryGetValue(key, out var raw) && raw is not null)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)raw;
                if (typeof(T) == typeof(bool) && bool.TryParse(raw, out var b))
                    return (T)(object)b;
                if (typeof(T) == typeof(double) && double.TryParse(raw, out var d))
                    return (T)(object)d;
            }
            return defaultValue;
        }
        finally { _lock.Release(); }
    }

    private void Set<T>(string key, T value)
    {
        EnsureLoaded();
        _lock.Wait();
        try
        {
            _values[key] = value?.ToString();
        }
        finally { _lock.Release(); }
        Save();
    }

    public bool EnableNotifications
    {
        get => Get(nameof(EnableNotifications), true);
        set => Set(nameof(EnableNotifications), value);
    }

    public string DefaultOrganizationMode
    {
        get => Get(nameof(DefaultOrganizationMode), "Move");
        set => Set(nameof(DefaultOrganizationMode), value);
    }

    public bool KeepFolderStructure
    {
        get => Get(nameof(KeepFolderStructure), false);
        set => Set(nameof(KeepFolderStructure), value);
    }

    public string ThemePreference
    {
        get => Get(nameof(ThemePreference), "System");
        set => Set(nameof(ThemePreference), value);
    }

    public bool ShowTeachingTips
    {
        get => Get(nameof(ShowTeachingTips), true);
        set => Set(nameof(ShowTeachingTips), value);
    }

    public bool HasShownFirstLaunchTip
    {
        get => Get(nameof(HasShownFirstLaunchTip), false);
        set => Set(nameof(HasShownFirstLaunchTip), value);
    }

    public string AppMode
    {
        get => Get(nameof(AppMode), "Pro");
        set => Set(nameof(AppMode), value);
    }

    public bool HasCompletedFirstLaunch
    {
        get => Get(nameof(HasCompletedFirstLaunch), false);
        set => Set(nameof(HasCompletedFirstLaunch), value);
    }

    public double WindowWidth
    {
        get => Get(nameof(WindowWidth), 1200.0);
        set => Set(nameof(WindowWidth), value);
    }

    public double WindowHeight
    {
        get => Get(nameof(WindowHeight), 800.0);
        set => Set(nameof(WindowHeight), value);
    }

    public double WindowX
    {
        get => Get(nameof(WindowX), -1.0);
        set => Set(nameof(WindowX), value);
    }

    public double WindowY
    {
        get => Get(nameof(WindowY), -1.0);
        set => Set(nameof(WindowY), value);
    }

    public bool UseRecycleBin
    {
        get => Get(nameof(UseRecycleBin), true);
        set => Set(nameof(UseRecycleBin), value);
    }

    public string SortMode
    {
        get => Get(nameof(SortMode), "Type");
        set => Set(nameof(SortMode), value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watcher?.Dispose();
        _lock.Dispose();
    }
}
