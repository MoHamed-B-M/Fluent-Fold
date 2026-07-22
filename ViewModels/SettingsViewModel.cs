using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using FluentFold.Services;

namespace FluentFold.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private readonly IFirstLaunchService _firstLaunch;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(IAppSettingsService settings, IFirstLaunchService firstLaunch, ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _firstLaunch = firstLaunch;
        _logger = logger;
        ReloadSettings();
    }

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private bool _enableNotifications;

    [ObservableProperty]
    private string _defaultOrganizationMode = "Move";

    [ObservableProperty]
    private bool _keepFolderStructure;

    [ObservableProperty]
    private string _themePreference = "System";

    [ObservableProperty]
    private bool _isSystemTheme = true;

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _showTeachingTips;

    [ObservableProperty]
    private string _appMode = "Pro";

    [ObservableProperty]
    private bool _useRecycleBin = true;
#pragma warning restore MVVMTK0045

    public string[] OrganizationModes { get; } = ["Move", "Copy"];
    public string[] AppModes { get; } = ["Standard", "Pro"];

    partial void OnEnableNotificationsChanged(bool value) => _settings.EnableNotifications = value;
    partial void OnDefaultOrganizationModeChanged(string value) => _settings.DefaultOrganizationMode = value;
    partial void OnKeepFolderStructureChanged(bool value) => _settings.KeepFolderStructure = value;
    partial void OnShowTeachingTipsChanged(bool value) => _settings.ShowTeachingTips = value;
    partial void OnAppModeChanged(string value) => _settings.AppMode = value;
    partial void OnUseRecycleBinChanged(bool value) => _settings.UseRecycleBin = value;

    partial void OnThemePreferenceChanged(string value)
    {
        _settings.ThemePreference = value;
        IsSystemTheme = value == "System";
        IsLightTheme = value == "Light";
        IsDarkTheme = value == "Dark";
        ApplyTheme(value);
    }

    public void ReloadSettings()
    {
        try
        {
            EnableNotifications = _settings.EnableNotifications;
            DefaultOrganizationMode = _settings.DefaultOrganizationMode;
            KeepFolderStructure = _settings.KeepFolderStructure;
            ThemePreference = _settings.ThemePreference;
            ShowTeachingTips = _settings.ShowTeachingTips;
            AppMode = _settings.AppMode;
            UseRecycleBin = _settings.UseRecycleBin;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            EnableNotifications = true;
            DefaultOrganizationMode = "Move";
            KeepFolderStructure = false;
            ThemePreference = "System";
            ShowTeachingTips = true;
            AppMode = "Pro";
            UseRecycleBin = true;
        }
    }

    private static void ApplyTheme(string theme)
    {
        try
        {
            if (Application.Current is not Application app)
                return;

            app.RequestedTheme = theme switch
            {
                "Light" => ApplicationTheme.Light,
                "Dark" => ApplicationTheme.Dark,
                _ => ApplicationTheme.Light
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Theme apply failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        EnableNotifications = true;
        DefaultOrganizationMode = "Move";
        KeepFolderStructure = false;
        ThemePreference = "System";
        ShowTeachingTips = true;
        AppMode = "Pro";
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        ThemePreference = theme;
    }

    [RelayCommand]
    private void ResetOnboarding()
    {
        _firstLaunch.Reset();
    }
}
