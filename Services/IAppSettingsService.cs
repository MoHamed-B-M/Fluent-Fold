namespace FluentFold.Services;

/// <summary>Manages application-wide settings persisted to local storage.</summary>
public interface IAppSettingsService
{
    /// <summary>Whether Windows notifications are enabled.</summary>
    bool EnableNotifications { get; set; }
    /// <summary>The default organization mode ("Move" or "Copy").</summary>
    string DefaultOrganizationMode { get; set; }
    /// <summary>Whether to preserve the original folder structure during organization.</summary>
    bool KeepFolderStructure { get; set; }
    /// <summary>The UI theme preference ("System", "Light", or "Dark").</summary>
    string ThemePreference { get; set; }
    /// <summary>Whether teaching tips are shown.</summary>
    bool ShowTeachingTips { get; set; }
    /// <summary>Whether the first-launch tip has been shown.</summary>
    bool HasShownFirstLaunchTip { get; set; }
    /// <summary>The application mode ("Pro" or "Standard").</summary>
    string AppMode { get; set; }
    /// <summary>Whether the first-launch flow has been completed.</summary>
    bool HasCompletedFirstLaunch { get; set; }
    /// <summary>The persisted main window width.</summary>
    double WindowWidth { get; set; }
    /// <summary>The persisted main window height.</summary>
    double WindowHeight { get; set; }
    /// <summary>The persisted main window X position.</summary>
    double WindowX { get; set; }
    /// <summary>The persisted main window Y position.</summary>
    double WindowY { get; set; }
    /// <summary>Whether to use the recycle bin when deleting files.</summary>
    bool UseRecycleBin { get; set; }
    /// <summary>The current sort mode for file listings.</summary>
    string SortMode { get; set; }
}
