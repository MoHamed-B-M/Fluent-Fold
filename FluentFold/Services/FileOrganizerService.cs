using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentFold.Models;
using Windows.Storage;

namespace FluentFold.Services;

public class FileOrganizerService
{
    private static readonly Dictionary<FileCategory, string[]> CategoryExtensions = new()
    {
        [FileCategory.Images] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".ico", ".webp" },
        [FileCategory.Documents] = new[] { ".pdf", ".doc", ".docx", ".txt", ".xls", ".xlsx", ".ppt", ".pptx", ".md", ".csv", ".json", ".xml", ".html", ".css", ".js" },
        [FileCategory.Videos] = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" },
        [FileCategory.Audio] = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" },
        [FileCategory.Archives] = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".iso" },
        [FileCategory.Code] = new[] { ".py", ".java", ".cpp", ".c", ".h", ".js", ".ts", ".go", ".rs", ".rb", ".php", ".sql", ".sh", ".bat", ".ps1" },
    };

    private readonly Stack<UndoAction> _undoStack = new();

    public static FileCategory GetCategory(string extension)
    {
        var ext = extension.ToLowerInvariant();
        foreach (var kvp in CategoryExtensions)
        {
            if (kvp.Value.Contains(ext))
                return kvp.Key;
        }
        return FileCategory.Others;
    }

    public async Task<FolderSummary> GetFolderSummaryAsync(StorageFolder folder)
    {
        var summary = new FolderSummary();
        var items = await folder.GetItemsAsync();

        foreach (var item in items)
        {
            if (item is StorageFile file)
            {
                summary.TotalFiles++;
                var cat = GetCategory(file.FileType);
                if (summary.CategoryCounts.ContainsKey(cat))
                    summary.CategoryCounts[cat]++;
                else
                    summary.CategoryCounts[cat] = 1;
            }
            else if (item is StorageFolder sf && !IsCategoryFolder(sf.Name))
            {
                summary.TotalFolders++;
            }
        }

        return summary;
    }

    private static bool IsCategoryFolder(string name)
    {
        return Enum.GetNames<FileCategory>().Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<OrganizeResult> OrganizeByTypeAsync(StorageFolder folder)
    {
        var result = new OrganizeResult();
        var undoAction = new UndoAction { Type = "organize" };

        var items = await folder.GetItemsAsync();
        var createdFolders = new HashSet<string>();

        foreach (var item in items)
        {
            if (item is not StorageFile file)
                continue;

            var category = GetCategory(file.FileType);
            var categoryName = category.ToString();

            var categoryFolder = await folder.CreateFolderAsync(categoryName, CreationCollisionOption.OpenIfExists);
            createdFolders.Add(categoryName);

            var destName = await ResolveCollisionAsync(categoryFolder, file.Name);
            var movedFile = await file.MoveAsync(categoryFolder, destName, NameCollisionOption.ReplaceExisting);

            undoAction.Moves.Add(new FileMove
            {
                CurrentPath = movedFile.Path,
                OriginalPath = file.Path
            });

            result.FilesMoved++;
            result.MovedItems.Add($"{file.Name} \u2192 {categoryName}\\{movedFile.Name}");
        }

        undoAction.CreatedFolders = createdFolders.ToList();

        if (result.FilesMoved > 0)
            _undoStack.Push(undoAction);

        return result;
    }

    private static async Task<string> ResolveCollisionAsync(StorageFolder folder, string desiredName)
    {
        var name = Path.GetFileNameWithoutExtension(desiredName);
        var ext = Path.GetExtension(desiredName);
        var candidate = desiredName;
        int counter = 1;

        while (await folder.TryGetItemAsync(candidate) != null)
        {
            candidate = $"{name}_{counter}{ext}";
            counter++;
        }

        return candidate;
    }

    public async Task<RenameResult> RenameFilesAsync(StorageFolder folder, string pattern, int startNumber)
    {
        var result = new RenameResult();
        var files = new List<StorageFile>();
        var items = await folder.GetItemsAsync();

        foreach (var item in items)
        {
            if (item is StorageFile file)
                files.Add(file);
        }

        files = files.OrderBy(f => f.Name).ToList();
        if (files.Count == 0)
            return result;

        var undoAction = new UndoAction { Type = "rename" };
        int maxIndex = files.Count + startNumber - 1;
        int padding = maxIndex.ToString().Length;

        int index = startNumber;
        foreach (var file in files)
        {
            var originalName = file.Name;
            var ext = file.FileType;
            var paddedIndex = index.ToString().PadLeft(padding, '0');
            var newName = $"{pattern}_{paddedIndex}{ext}";
            var targetPath = System.IO.Path.Combine(folder.Path, newName);

            if (File.Exists(targetPath))
            {
                newName = $"{pattern}_{paddedIndex}_copy{ext}";
            }

            await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);

            undoAction.Moves.Add(new FileMove
            {
                CurrentPath = System.IO.Path.Combine(folder.Path, newName),
                OriginalPath = System.IO.Path.Combine(folder.Path, originalName)
            });

            result.FilesRenamed++;
            result.RenamedItems.Add((originalName, newName));
            index++;
        }

        if (result.FilesRenamed > 0)
            _undoStack.Push(undoAction);

        return result;
    }

    public async Task<UndoResult> UndoLastOperationAsync()
    {
        if (_undoStack.Count == 0)
            return new UndoResult { Success = false, Message = "Nothing to undo" };

        var action = _undoStack.Pop();
        int undone = 0;

        var moves = action.Type == "rename"
            ? Enumerable.Reverse(action.Moves).ToList()
            : action.Moves;

        foreach (var move in moves)
        {
            try
            {
                var sourceFile = await StorageFile.GetFileFromPathAsync(move.CurrentPath);
                var destFolder = await StorageFolder.GetFolderFromPathAsync(
                    Path.GetDirectoryName(move.OriginalPath)!);
                var destName = Path.GetFileName(move.OriginalPath);

                await sourceFile.MoveAsync(destFolder, destName, NameCollisionOption.ReplaceExisting);
                undone++;
            }
            catch
            {
                // skip files that can't be restored
            }
        }

        if (action.Type == "organize" && undone > 0)
        {
            foreach (var catName in action.CreatedFolders)
            {
                try
                {
                    var catFolder = await StorageFolder.GetFolderFromPathAsync(
                        System.IO.Path.Combine(
                            Path.GetDirectoryName(action.Moves[0].OriginalPath)!,
                            catName));
                    var remaining = await catFolder.GetItemsAsync();
                    if (remaining.Count == 0)
                        await catFolder.DeleteAsync();
                }
                catch
                {
                    // skip folders that can't be cleaned up
                }
            }
        }

        return new UndoResult
        {
            Success = undone > 0,
            Message = undone > 0
                ? $"Undo successful: {undone} file(s) reverted"
                : "Nothing to undo"
        };
    }

    public bool CanUndo => _undoStack.Count > 0;

    private class UndoAction
    {
        public string Type { get; set; } = "";
        public List<FileMove> Moves { get; set; } = new();
        public List<string> CreatedFolders { get; set; } = new();
    }

    private class FileMove
    {
        public string CurrentPath { get; set; } = "";
        public string OriginalPath { get; set; } = "";
    }
}
