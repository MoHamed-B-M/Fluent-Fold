using Windows.Storage;

namespace FluentFold.Services;

/// <summary>Abstraction for folder picking and persisted folder access.</summary>
public interface IFolderPickerService
{
    /// <summary>Shows the folder picker dialog and persists the selection.</summary>
    /// <returns>The selected folder, or null if cancelled.</returns>
    Task<StorageFolder?> PickFolderAsync();

    /// <summary>Retrieves the last persisted folder without showing a dialog.</summary>
    /// <returns>The previously picked folder, or null if none exists.</returns>
    Task<StorageFolder?> GetPersistedFolderAsync();
}
