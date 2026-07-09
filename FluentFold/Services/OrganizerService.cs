using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using FluentFold.Models;

namespace FluentFold.Services;

public class OrganizerService
{
    private readonly Stack<List<(string originalPath, string newPath)>> _undoStack = new();

    public async Task<FolderSummary> GetFolderSummaryAsync(StorageFolder folder)
    {
        var summary = new FolderSummary();
        var items = await folder.GetItemsAsync();
        summary.TotalFolders = items.Count(i => i.IsOfType(StorageItemTypes.Folder));
        summary.TotalFiles = items.Count(i => i.IsOfType(StorageItemTypes.File));

        foreach (var item in items)
        {
            if (item is StorageFile file)
            {
                var ext = Path.GetExtension(file.Name);
                var cat = FileCategorizer.Categorize(ext);
                if (!summary.CategoryCounts.ContainsKey(cat))
                    summary.CategoryCounts[cat] = 0;
                summary.CategoryCounts[cat]++;
            }
        }
        return summary;
    }

    public async Task<OrganizeResult> OrganizeFolderAsync(StorageFolder sourceFolder, string renamePattern, bool organizeByCategory, bool renameFiles)
    {
        var result = new OrganizeResult();
        var movedFiles = new List<(string, string)>();
        var items = await sourceFolder.GetItemsAsync();
        var files = items.Where(i => i.IsOfType(StorageItemTypes.File)).Cast<StorageFile>().ToList();
        int counter = 1;

        try
        {
            foreach (var file in files)
            {
                string destFolderName;
                if (organizeByCategory)
                {
                    var ext = Path.GetExtension(file.Name);
                    var cat = FileCategorizer.Categorize(ext);
                    destFolderName = FileCategorizer.CategoryToFolderName(cat);
                }
                else
                {
                    destFolderName = "Organized";
                }

                var destFolder = await sourceFolder.CreateFolderAsync(destFolderName, CreationCollisionOption.OpenIfExists);
                string newName = file.Name;

                if (renameFiles && !string.IsNullOrWhiteSpace(renamePattern))
                {
                    var ext = Path.GetExtension(file.Name);
                    newName = $"{renamePattern}_{counter:D3}{ext}";
                    counter++;
                }

                var destFile = await file.CopyAsync(destFolder, newName, NameCollisionOption.GenerateUniqueName);
                await file.DeleteAsync();
                movedFiles.Add((file.Path, destFile.Path));
                result.FilesMoved++;
            }

            if (renameFiles && !organizeByCategory && !string.IsNullOrWhiteSpace(renamePattern))
            {
                var organizedFolder = await sourceFolder.GetFolderAsync("Organized");
                var organizedFiles = await organizedFolder.GetFilesAsync();
                counter = 1;
                foreach (var file in organizedFiles)
                {
                    var ext = Path.GetExtension(file.Name);
                    var newName = $"{renamePattern}_{counter:D3}{ext}";
                    await file.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);
                    counter++;
                }
                result.RenamedCount = organizedFiles.Count;
            }

            if (organizeByCategory)
                result.RenamedCount = counter - 1;

            _undoStack.Push(movedFiles);
            result.Success = true;
            result.Message = $"Organized {result.FilesMoved} files successfully.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error: {ex.Message}";
        }

        return result;
    }

    public async Task<bool> UndoLastAsync()
    {
        if (_undoStack.Count == 0) return false;

        var moves = _undoStack.Pop();
        foreach (var (original, current) in moves)
        {
            try
            {
                var currentFile = await StorageFile.GetFileFromPathAsync(current);
                var originalDir = Path.GetDirectoryName(original);
                if (originalDir != null)
                {
                    var origFolder = await StorageFolder.GetFolderFromPathAsync(originalDir);
                    await currentFile.CopyAsync(origFolder, Path.GetFileName(original), NameCollisionOption.GenerateUniqueName);
                    await currentFile.DeleteAsync();
                }
            }
            catch { }
        }
        return true;
    }
}