using Microsoft.Extensions.Logging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FluentFold.Services;

public sealed class FolderPickerService(IWindowService windowService, ILogger<FolderPickerService> logger) : IFolderPickerService
{
    public async Task<StorageFolder?> PickFolderAsync()
    {
        var picker = new FolderPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");

        InitializeWithWindow.Initialize(picker, windowService.WindowHandle);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            logger.LogInformation("Folder picked: '{Path}'", folder.Path);
        }

        return folder;
    }

    public async Task<StorageFolder?> GetPersistedFolderAsync()
    {
        try
        {
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem("PickedFolderToken"))
                return null;

            return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("PickedFolderToken");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve persisted folder (unpackaged mode may not support FutureAccessList)");
            return null;
        }
    }
}
