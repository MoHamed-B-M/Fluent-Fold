using Microsoft.Extensions.Logging;
using FluentFold.Models;

namespace FluentFold.Services;

public sealed class RenamingService(ILogger<RenamingService> logger) : IRenamingService
{
    public IReadOnlyList<RenamePreview> Preview(
        IReadOnlyList<FileEntry> files, string pattern, int startNumber, int padding)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(pattern);

        var results = new List<RenamePreview>(files.Count);
        int counter = startNumber;

        foreach (var file in files)
        {
            var dir = System.IO.Path.GetDirectoryName(file.Path) ?? string.Empty;
            var ext = System.IO.Path.GetExtension(file.Name);
            var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file.Name);

            var newName = pattern
                .Replace("{name}", nameWithoutExt)
                .Replace("{n}", counter.ToString(new string('0', padding)))
                .Replace("{ext}", ext);

            var newPath = System.IO.Path.Combine(dir, newName);

            results.Add(new RenamePreview
            {
                OriginalPath = file.Path,
                NewPath = newPath,
                OriginalName = file.Name,
                NewName = newName
            });

            counter++;
        }

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<RenamePreview>> ApplyAsync(
        IReadOnlyList<FileEntry> files, string pattern, int startNumber, int padding)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(pattern);

        var previews = Preview(files, pattern, startNumber, padding);

        foreach (var p in previews)
        {
            if (p.OriginalPath == p.NewPath) continue;

            if (File.Exists(p.NewPath))
            {
                logger.LogWarning("Skipped rename: target already exists '{NewPath}'", p.NewPath);
                continue;
            }

            try
            {
                await Task.Run(() => File.Move(p.OriginalPath, p.NewPath));
                logger.LogInformation("Renamed '{Original}' -> '{New}'", p.OriginalPath, p.NewPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to rename '{Original}' to '{New}'", p.OriginalPath, p.NewPath);
                throw;
            }
        }

        return previews;
    }
}

public class RenamePreview
{
    public string OriginalPath { get; init; } = string.Empty;
    public string NewPath { get; init; } = string.Empty;
    public string OriginalName { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
}
