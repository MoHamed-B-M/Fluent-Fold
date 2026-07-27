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
        try
        {
            var hwnd = windowService.WindowHandle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("Main window handle is not available");

            var picker = new FolderPicker
            {
                ViewMode = PickerViewMode.List,
            };
            picker.FileTypeFilter.Add("*");

            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                try
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to persist folder access token (unpackaged mode)");
                }
                logger.LogInformation("Folder picked: '{Path}'", folder.Path);
            }

            return folder;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PickFolderAsync failed");
            throw;
        }
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
