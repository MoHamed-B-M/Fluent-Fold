using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFold.Models;
using FluentFold.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FluentFold.ViewModels;

public partial class OrganizerViewModel : ObservableObject
{
    private readonly FileOrganizerService _organizerService = new();
    private StorageFolder? _selectedFolder;

    [ObservableProperty]
    private string _folderPath = "";

    [ObservableProperty]
    private bool _isFolderSelected;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _totalFolders;

    [ObservableProperty]
    private string _statusMessage = "Select a folder to begin";

    [ObservableProperty]
    private bool _canUndo;

    public ObservableCollection<CategoryCountItem> CategoryCounts { get; } = new();

    public ObservableCollection<string> ActivityLog { get; } = new();

    public OrganizerViewModel()
    {
        InitializeCategoryCounts();
    }

    private void InitializeCategoryCounts()
    {
        foreach (FileCategory cat in Enum.GetValues<FileCategory>())
        {
            CategoryCounts.Add(new CategoryCountItem { Category = cat, Count = 0 });
        }
    }

    [RelayCommand]
    private async Task BrowseFolder()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            ViewMode = PickerViewMode.List
        };

        // WinUI 3 requires this for desktop apps
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            _selectedFolder = folder;
            FolderPath = folder.Path;
            IsFolderSelected = true;
            AddLog($"Selected folder: {folder.Path}");
            await RefreshSummaryAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshSummaryAsync()
    {
        if (_selectedFolder == null) return;

        IsBusy = true;
        StatusMessage = "Analyzing folder...";

        try
        {
            var summary = await _organizerService.GetFolderSummaryAsync(_selectedFolder);
            TotalFiles = summary.TotalFiles;
            TotalFolders = summary.TotalFolders;

            foreach (var item in CategoryCounts)
            {
                item.Count = summary.CategoryCounts.GetValueOrDefault(item.Category, 0);
            }

            StatusMessage = $"{summary.TotalFiles} files, {summary.TotalFolders} folders";
            AddLog($"Summary updated: {summary.TotalFiles} files, {summary.TotalFolders} folders");
        }
        catch (Exception ex)
        {
            StatusMessage = "Error reading folder";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OrganizeFiles()
    {
        if (_selectedFolder == null) return;

        IsBusy = true;
        CanUndo = false;
        StatusMessage = "Organizing files...";

        try
        {
            var result = await _organizerService.OrganizeByTypeAsync(_selectedFolder);
            AddLog($"Organized {result.FilesMoved} files:");
            foreach (var item in result.MovedItems)
                AddLog($"  {item}");

            StatusMessage = $"Organized {result.FilesMoved} files";
            CanUndo = _organizerService.CanUndo;
            await RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Organize failed";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RenameFiles()
    {
        if (_selectedFolder == null) return;

        // Show rename dialog - this will be handled by the View
        // The ViewModel provides the command, the View shows the ContentDialog
        IsBusy = true;
        CanUndo = false;
        StatusMessage = "Renaming files...";

        try
        {
            var result = await _organizerService.RenameFilesAsync(
                _selectedFolder, "file", 1);

            AddLog($"Renamed {result.FilesRenamed} files:");
            foreach (var (oldName, newName) in result.RenamedItems)
                AddLog($"  {oldName} \u2192 {newName}");

            StatusMessage = $"Renamed {result.FilesRenamed} files";
            CanUndo = _organizerService.CanUndo;
            await RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Rename failed";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RenameFilesWithOptions(string pattern, int startNumber)
    {
        if (_selectedFolder == null) return;

        IsBusy = true;
        CanUndo = false;
        StatusMessage = "Renaming files...";

        try
        {
            var result = await _organizerService.RenameFilesAsync(
                _selectedFolder, pattern, startNumber);

            AddLog($"Renamed {result.FilesRenamed} files with pattern '{pattern}_###':");
            foreach (var (oldName, newName) in result.RenamedItems)
                AddLog($"  {oldName} \u2192 {newName}");

            StatusMessage = $"Renamed {result.FilesRenamed} files";
            CanUndo = _organizerService.CanUndo;
            await RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Rename failed";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Undo()
    {
        if (!_organizerService.CanUndo) return;

        IsBusy = true;
        StatusMessage = "Undoing...";

        try
        {
            var result = await _organizerService.UndoLastOperationAsync();
            AddLog($"Undo: {result.Message}");
            StatusMessage = result.Message;
            CanUndo = _organizerService.CanUndo;
            await RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Undo failed";
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        ActivityLog.Add($"[{timestamp}] {message}");
    }
}

public partial class CategoryCountItem : ObservableObject
{
    public FileCategory Category { get; set; }

    [ObservableProperty]
    private int _count;

    public string CategoryName => Category.ToString();
    public string Icon => Category switch
    {
        FileCategory.Images => "\uE722",
        FileCategory.Documents => "\uE8A5",
        FileCategory.Videos => "\uE714",
        FileCategory.Audio => "\uE8D6",
        FileCategory.Archives => "\uE7B8",
        FileCategory.Code => "\uE943",
        FileCategory.Others => "\uE7C3",
        _ => "\uE7C3"
    };

    public string ColorCode => Category switch
    {
        FileCategory.Images => "#4CAF50",
        FileCategory.Documents => "#2196F3",
        FileCategory.Videos => "#FF9800",
        FileCategory.Audio => "#9C27B0",
        FileCategory.Archives => "#FF5722",
        FileCategory.Code => "#00BCD4",
        FileCategory.Others => "#78909C",
        _ => "#78909C"
    };
}
