using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.UI.Xaml.Controls;
using FluentFold.Models;
using FluentFold.Services;
using FluentFold.Helpers;

namespace FluentFold.ViewModels;

public sealed partial class OrganizerViewModel : ObservableObject
{
    private readonly IFolderPickerService _folderPicker;
    private readonly IOrganizerService _organizer;
    private readonly IUndoService _undo;
    private readonly IAppSettingsService _settings;
    private readonly IRenamingService _renamer;
    private readonly IRulesEngine _rulesEngine;
    private readonly ILogger<OrganizerViewModel> _logger;
    private StorageFolder? _currentFolder;
    private string? _currentPath;
    private CancellationTokenSource? _cts;

    public OrganizerViewModel(
        IFolderPickerService folderPicker,
        IOrganizerService organizer,
        IUndoService undo,
        IAppSettingsService settings,
        IRenamingService renamer,
        IRulesEngine rulesEngine,
        ILogger<OrganizerViewModel> logger)
    {
        _folderPicker = folderPicker;
        _organizer = organizer;
        _undo = undo;
        _settings = settings;
        _renamer = renamer;
        _rulesEngine = rulesEngine;
        _logger = logger;

        SortMode = _settings.SortMode;
        RefreshMode();
        _ = RestorePersistedFolderAsync();
    }

    public ObservableCollection<FileEntry> Files { get; } = new();
    public ObservableCollection<ExtensionRule> CustomRules { get; } = new();
    public ObservableCollection<CleanupFileItem> CleanupFiles { get; } = new();
    public ObservableCollection<DuplicateFileEntry> DuplicateFiles { get; } = new();
    public ObservableCollection<RuleModel> RuleModels { get; } = new();

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private string _selectedFolderPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSelectButton))]
    private bool _hasFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    private bool _isWorking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    private double _progressValue;

    public string ProgressPercent => IsWorking ? $"{(int)(ProgressValue * 100)}%" : string.Empty;
    public bool ShowSelectButton => !HasFolder;

    [ObservableProperty]
    private bool _isInfoBarOpen;

    [ObservableProperty]
    private string _infoBarMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _infoBarSeverity;

    [ObservableProperty]
    private bool _showCleanupSuggestion;

    [ObservableProperty]
    private int _cleanupStaleFileCount;

    [ObservableProperty]
    private string _cleanupSuggestionMessage = string.Empty;

    [ObservableProperty]
    private int _cleanupZeroByteFileCount;

    [ObservableProperty]
    private bool _showDuplicatePanel;

    [ObservableProperty]
    private int _duplicateGroupCount;

    [ObservableProperty]
    private string _duplicateTotalSize = string.Empty;

    [ObservableProperty]
    private int _imageCount;

    [ObservableProperty]
    private int _documentCount;

    [ObservableProperty]
    private int _videoCount;

    [ObservableProperty]
    private int _audioCount;

    [ObservableProperty]
    private int _archiveCount;

    [ObservableProperty]
    private int _codeCount;

    [ObservableProperty]
    private int _otherCount;

    [ObservableProperty]
    private string _renamePattern = "{name}_{n}{ext}";

    [ObservableProperty]
    private int _renameStartNumber = 1;

    [ObservableProperty]
    private int _renamePadding = 3;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canOrganize;

    [ObservableProperty]
    private string _sortMode = "Type";

    public string[] SortModes { get; } = ["Type", "Date Created", "Date Accessed", "Name", "Size"];

    public bool UseRecycleBin => _settings.UseRecycleBin;

    [ObservableProperty]
    private string _newRuleExtension = string.Empty;

    [ObservableProperty]
    private string _newRuleCategoryName = string.Empty;

    [ObservableProperty]
    private string _folderNamingPattern = string.Empty;

    [ObservableProperty]
    private string _newTriggerValue = string.Empty;

    [ObservableProperty]
    private string _newTriggerCategoryName = string.Empty;

    [ObservableProperty]
    private int _newTriggerTypeIndex;

    [ObservableProperty]
    private bool _isProMode = true;

    [ObservableProperty]
    private bool _isStandardMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    [NotifyPropertyChangedFor(nameof(IsGridView))]
    private string _viewMode = "List";

    public bool IsListView => ViewMode == "List";
    public bool IsGridView => ViewMode == "Grid";
#pragma warning restore MVVMTK0045

    public void RefreshMode()
    {
        var pro = _settings.AppMode == "Pro";
        IsProMode = pro;
        IsStandardMode = !pro;
    }

    [RelayCommand]
    private void CloseInfoBar()
    {
        IsInfoBarOpen = false;
    }

    [RelayCommand]
    private void DismissCleanupSuggestion() => ShowCleanupSuggestion = false;

    partial void OnSortModeChanged(string value)
    {
        _settings.SortMode = value;
        if (_currentFolder is not null && Files.Count > 0)
        {
            var sorted = SortEntries([.. Files]);
            Files.Clear();
            foreach (var entry in sorted)
                Files.Add(entry);
        }
    }

    private List<FileEntry> SortEntries(List<FileEntry> entries) => SortMode switch
    {
        "Date Created" => [.. entries.OrderBy(f => f.DateCreated)],
        "Date Accessed" => [.. entries.OrderBy(f => f.DateAccessed)],
        "Name" => [.. entries.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)],
        "Size" => [.. entries.OrderByDescending(f => f.Size)],
        _ => [.. entries.OrderBy(f => f.Category).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)]
    };

    [RelayCommand]
    private void CancelOperation()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void AddCustomRule()
    {
        var ext = NewRuleExtension.Trim();
        var cat = NewRuleCategoryName.Trim();

        if (string.IsNullOrEmpty(ext) || string.IsNullOrEmpty(cat))
        {
            ShowInfoBar(ResourceHelper.GetError("ExtensionRequired"), InfoBarSeverity.Warning);
            return;
        }

        if (!ext.StartsWith("."))
            ext = "." + ext;

        foreach (var existing in CustomRules)
        {
            if (existing.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                ShowInfoBar(ResourceHelper.FormatError("RuleAlreadyExists", ext), InfoBarSeverity.Warning);
                return;
            }
        }

        CustomRules.Add(new ExtensionRule
        {
            Extension = ext.ToLowerInvariant(),
            CategoryName = cat
        });

        NewRuleExtension = string.Empty;
        NewRuleCategoryName = string.Empty;

        if (_currentFolder is not null)
            _ = LoadFolderAsync(_currentFolder);

        ShowInfoBar($"Added rule: {ext} → {cat}", InfoBarSeverity.Success);
    }

    [RelayCommand]
    private void RemoveCustomRule(ExtensionRule rule)
    {
        CustomRules.Remove(rule);

        if (_currentFolder is not null)
            _ = LoadFolderAsync(_currentFolder);
    }

    [RelayCommand]
    private void AddTriggerRule()
    {
        var value = NewTriggerValue.Trim();
        var cat = NewTriggerCategoryName.Trim();

        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(cat))
        {
            ShowInfoBar(ResourceHelper.GetError("TriggerRequired"), InfoBarSeverity.Warning);
            return;
        }

        RuleModels.Add(new RuleModel
        {
            TriggerType = (TriggerType)NewTriggerTypeIndex,
            TriggerValue = value,
            TargetCategory = cat
        });

        NewTriggerValue = string.Empty;
        NewTriggerCategoryName = string.Empty;

        if (_currentFolder is not null)
            _ = LoadFolderAsync(_currentFolder);

        ShowInfoBar($"Added trigger rule: {value} → {cat}", InfoBarSeverity.Success);
    }

    [RelayCommand]
    private void RemoveTriggerRule(RuleModel rule)
    {
        RuleModels.Remove(rule);

        if (_currentFolder is not null)
            _ = LoadFolderAsync(_currentFolder);
    }

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var folder = await _folderPicker.PickFolderAsync();
        if (folder is null) return;

        await LoadFolderAsync(folder);
    }

    [RelayCommand]
    private async Task OrganizeAsync()
    {
        if (_currentPath is null || Files.Count == 0) return;
        if (IsWorking) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsWorking = true;
        ProgressValue = 0;
        ShowInfoBar(ResourceHelper.GetLog("OrganizingFiles"), InfoBarSeverity.Informational);

        try
        {
            var snapshot = Files.ToList();
            var rules = CustomRules.ToList();
            var pattern = string.IsNullOrWhiteSpace(FolderNamingPattern) ? null : FolderNamingPattern;

            var copyMode = _settings.DefaultOrganizationMode == "Copy";
            var progress = new Progress<double>(p => ProgressValue = p);

            var operation = await _organizer.OrganizeFilesAsync(
                _currentPath, snapshot, copyMode, pattern, rules, progress, ct);

            _undo.Push(operation);
            CanUndo = _undo.CanUndo;
            ProgressValue = 1.0;

            await LoadFolderAsync(_currentFolder!);
            ShowCleanupSuggestion = false;

            var action = copyMode ? "Copied" : "Moved";
            ShowInfoBar($"{action} {operation.Moves.Count} file(s) into organized folders.", InfoBarSeverity.Success);
            ShowNotification(ResourceHelper.GetLog("OrganizationComplete"), $"{action} {operation.Moves.Count} file(s) in {Path.GetFileName(_currentPath)}.");
        }
        catch (OperationCanceledException)
        {
            ShowInfoBar(ResourceHelper.GetLog("OperationCancelled"), InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Organization failed");
            ShowInfoBar(ResourceHelper.FormatError("OperationFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        var operation = _undo.Pop();
        if (operation is null) return;
        if (IsWorking) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsWorking = true;
        ProgressValue = 0;
        ShowInfoBar(ResourceHelper.GetLog("UndoingOperation"), InfoBarSeverity.Informational);

        try
        {
            var progress = new Progress<double>(p => ProgressValue = p);
            await _organizer.UndoOperationAsync(operation, progress, ct);
            CanUndo = _undo.CanUndo;

            if (_currentFolder is not null)
                await LoadFolderAsync(_currentFolder);

            ShowInfoBar($"Restored {operation.Moves.Count} file(s).", InfoBarSeverity.Success);
            ShowNotification(ResourceHelper.GetLog("UndoComplete"), $"Restored {operation.Moves.Count} file(s) to their original locations.");
        }
        catch (OperationCanceledException)
        {
            ShowInfoBar(ResourceHelper.GetLog("OperationCancelled"), InfoBarSeverity.Warning);
            _undo.Push(operation);
            CanUndo = _undo.CanUndo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Undo failed");
            ShowInfoBar(ResourceHelper.FormatError("UndoFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAllAsync()
    {
        var allOps = _undo.History.ToList();
        if (allOps.Count == 0) return;
        if (IsWorking) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsWorking = true;
        ProgressValue = 0;
        ShowInfoBar(ResourceHelper.GetLog("RestoringAll"), InfoBarSeverity.Informational);

        try
        {
            double step = 1.0 / allOps.Count;
            for (int i = 0; i < allOps.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await _organizer.UndoOperationAsync(allOps[i]);
                ProgressValue = (i + 1) * step;
            }

            _undo.Clear();
            CanUndo = false;

            if (_currentFolder is not null)
                await LoadFolderAsync(_currentFolder);

            ShowInfoBar($"Restored {allOps.Count} operation(s).", InfoBarSeverity.Success);
            ShowNotification(ResourceHelper.GetLog("RestoreComplete"), $"Restored {allOps.Count} operation(s) to original state.");
        }
        catch (OperationCanceledException)
        {
            ShowInfoBar(ResourceHelper.GetLog("OperationCancelled"), InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore all failed");
            ShowInfoBar(ResourceHelper.FormatError("RestoreFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private void SetListView() => ViewMode = "List";

    [RelayCommand]
    private void SetGridView() => ViewMode = "Grid";

    [RelayCommand]
    private async Task RenameAsync()
    {
        if (Files.Count == 0) return;

        IsWorking = true;

        try
        {
            var selected = Files.ToList();
            var results = await _renamer.ApplyAsync(selected, RenamePattern, RenameStartNumber, RenamePadding);
            var renamed = results.Count(r => r.OriginalPath != r.NewPath);
            ShowInfoBar($"Renamed {renamed} file(s).", InfoBarSeverity.Success);

            if (_currentFolder is not null)
                await LoadFolderAsync(_currentFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rename failed");
            ShowInfoBar(ResourceHelper.FormatError("RenameFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task FindDuplicatesAsync()
    {
        if (_currentPath is null) return;
        if (IsWorking) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        DuplicateFiles.Clear();
        ShowDuplicatePanel = false;
        IsWorking = true;
        ProgressValue = 0;

        try
        {
            var progress = new Progress<double>(p => ProgressValue = p);
            var results = await _organizer.FindDuplicatesAsync(_currentPath, progress, ct);

            foreach (var dup in results)
                DuplicateFiles.Add(dup);

            var groups = results.Select(d => d.GroupId).Distinct().Count();
            DuplicateGroupCount = groups;
            var totalBytes = results.Sum(d => d.Size);
            DuplicateTotalSize = FormatHelper.FormatSize(totalBytes);
            ShowDuplicatePanel = results.Count > 0;

            if (results.Count > 0)
                ShowInfoBar($"Found {results.Count} duplicate file(s) in {groups} group(s).", InfoBarSeverity.Informational);
            else
                ShowInfoBar(ResourceHelper.GetLog("NoDuplicatesFound"), InfoBarSeverity.Success);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Duplicate scan failed");
            ShowInfoBar(ResourceHelper.FormatError("DuplicateScanFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private void DismissDuplicatePanel() => ShowDuplicatePanel = false;

    private async Task LoadFolderAsync(StorageFolder folder)
    {
        _currentFolder = folder;
        _currentPath = folder.Path;
        SelectedFolderPath = folder.Path;
        HasFolder = true;
        ShowDuplicatePanel = false;

        IsWorking = true;
        ProgressValue = 0;
        try
        {
            var extensionRules = CustomRules.ToList();
            var entries = await _organizer.ScanFolderAsync(folder.Path, extensionRules);
            ProgressValue = 0.4;

            var triggerRules = RuleModels.ToList();
            if (triggerRules.Count > 0)
            {
                foreach (var entry in entries)
                {
                    var ruleCategory = _rulesEngine.ApplyRules(entry.Name, triggerRules);
                    if (!string.IsNullOrEmpty(ruleCategory))
                        entry.Category = ruleCategory;
                }
            }
            ProgressValue = 0.6;

            entries = SortEntries(entries);

            Files.Clear();
            foreach (var entry in entries)
                Files.Add(entry);

            UpdateCategoryCounts(entries);
            CanOrganize = entries.Count > 0;
            DetectCleanupFiles(folder.Path);
            ProgressValue = 1.0;
        }
        finally
        {
            IsWorking = false;
        }
    }

    private void DetectCleanupFiles(string directory)
    {
        CleanupFiles.Clear();
        CleanupZeroByteFileCount = 0;
        CleanupStaleFileCount = 0;
        var threshold = DateTimeOffset.UtcNow.AddYears(-1);
        int tempFileCount = 0;
        int emptyFolderCount = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                try
                {
                    var info = new FileInfo(file);
                    var isZeroByte = info.Length == 0;
                    var isStale = info.LastWriteTimeUtc < threshold.UtcDateTime;
                    var isTemp = info.Extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
                              || info.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
                              || info.Extension.Equals(".cache", StringComparison.OrdinalIgnoreCase);

                    if (isZeroByte || isStale || isTemp)
                    {
                        var reasons = new List<string>();
                        if (isZeroByte) reasons.Add("Zero-byte file");
                        if (isStale) reasons.Add($"Not modified since {info.LastWriteTime:yyyy-MM-dd}");
                        if (isTemp) reasons.Add("Temporary file");

                        CleanupFiles.Add(new CleanupFileItem
                        {
                            FilePath = file,
                            Size = info.Length,
                            LastModified = info.LastWriteTime,
                            Reason = string.Join(", ", reasons)
                        });
                        if (isZeroByte) CleanupZeroByteFileCount++;
                        if (isStale) CleanupStaleFileCount++;
                        if (isTemp) tempFileCount++;
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }

            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                try
                {
                    if (Directory.EnumerateFileSystemEntries(dir).Any()) continue;
                    CleanupFiles.Add(new CleanupFileItem
                    {
                        FilePath = dir,
                        Size = 0,
                        LastModified = Directory.GetLastWriteTime(dir),
                        Reason = "Empty folder"
                    });
                    emptyFolderCount++;
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        var parts = new List<string>();
        if (CleanupZeroByteFileCount > 0) parts.Add($"{CleanupZeroByteFileCount} zero-byte");
        if (CleanupStaleFileCount > 0) parts.Add($"{CleanupStaleFileCount} stale");
        if (tempFileCount > 0) parts.Add($"{tempFileCount} temp");
        if (emptyFolderCount > 0) parts.Add($"{emptyFolderCount} empty folder(s)");

        CleanupSuggestionMessage = $"Found {string.Join(", ", parts)}. Review & delete to free up space.";
        ShowCleanupSuggestion = CleanupFiles.Count > 0;
    }

    private void UpdateCategoryCounts(List<FileEntry> entries)
    {
        ImageCount = entries.Count(e => e.Category == "Images");
        DocumentCount = entries.Count(e => e.Category == "Documents");
        VideoCount = entries.Count(e => e.Category == "Videos");
        AudioCount = entries.Count(e => e.Category == "Audio");
        ArchiveCount = entries.Count(e => e.Category == "Archives");
        CodeCount = entries.Count(e => e.Category == "Code");
        OtherCount = entries.Count(e => e.Category is not ("Images" or "Documents" or "Videos" or "Audio" or "Archives" or "Code"));
    }

    private void ShowInfoBar(string message, InfoBarSeverity severity)
    {
        InfoBarMessage = message;
        InfoBarSeverity = severity;
        IsInfoBarOpen = true;
    }

    public void ShowInfoBarInvoke(string message, InfoBarSeverity severity) => ShowInfoBar(message, severity);

    private void ShowNotification(string title, string body)
    {
        if (!_settings.EnableNotifications) return;

        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show notification");
        }
    }

    private async Task RestorePersistedFolderAsync()
    {
        var folder = await _folderPicker.GetPersistedFolderAsync();
        if (folder is not null)
            await LoadFolderAsync(folder);
    }
}
