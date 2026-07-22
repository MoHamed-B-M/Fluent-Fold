using Microsoft.Extensions.Logging;
using FluentFold.Models;

namespace FluentFold.Services;

public sealed class OrganizerService(ILogger<OrganizerService> logger) : IOrganizerService
{
    public Task<List<FileEntry>> ScanFolderAsync(
        string directory,
        IReadOnlyList<ExtensionRule>? customRules = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(directory);

        var results = new List<FileEntry>();
        var dirInfo = new DirectoryInfo(directory);

        if (!dirInfo.Exists)
        {
            logger.LogWarning("Folder not found: {Directory}", directory);
            return Task.FromResult(results);
        }

        foreach (var file in dirInfo.EnumerateFiles())
        {
            ct.ThrowIfCancellationRequested();
            results.Add(new FileEntry
            {
                Name = file.Name,
                Path = file.FullName,
                Category = GetCategory(file.Extension, customRules),
                DateModified = file.LastWriteTimeUtc,
                DateCreated = file.CreationTimeUtc,
                DateAccessed = file.LastAccessTimeUtc,
                Size = file.Length
            });
        }

        logger.LogInformation("Scanned folder '{Directory}': {Count} files", directory, results.Count);
        return Task.FromResult(results);
    }

    public async Task<OrganizeOperation> OrganizeFilesAsync(
        string rootDirectory,
        IReadOnlyList<FileEntry> files,
        bool copyMode = false,
        string? folderNamingPattern = null,
        IReadOnlyList<ExtensionRule>? customRules = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(files);

        var moves = new List<FileMove>(files.Count);
        int total = files.Count;

        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = files[i];
            var source = file.Path;
            var category = GetCategory(Path.GetExtension(file.Name), customRules);
            var folderName = ResolveFolderName(category, folderNamingPattern);
            var categoryDir = Path.Combine(rootDirectory, folderName);

            Directory.CreateDirectory(categoryDir);

            var dest = Path.Combine(categoryDir, file.Name);
            if (source == dest)
            {
                progress?.Report((i + 1.0) / total);
                continue;
            }

            try
            {
                if (copyMode)
                    await Task.Run(() => File.Copy(source, dest, overwrite: false), ct);
                else
                    await Task.Run(() => File.Move(source, dest), ct);

                moves.Add(new FileMove
                {
                    SourcePath = source,
                    DestPath = dest,
                    FileName = file.Name,
                    Category = category
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to {Action} file '{Source}' to '{Dest}'", copyMode ? "copy" : "move", source, dest);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Access denied {Action} file '{Source}'", copyMode ? "copying" : "moving", source);
            }

            progress?.Report((i + 1.0) / total);
        }

        logger.LogInformation("Organized {Count} files in '{Folder}' ({Mode})", moves.Count, rootDirectory, copyMode ? "copy" : "move");
        return new OrganizeOperation
        {
            Timestamp = DateTimeOffset.Now,
            SourceFolder = rootDirectory,
            Moves = moves.AsReadOnly(),
            IsCopy = copyMode
        };
    }

    public async Task UndoOperationAsync(
        OrganizeOperation operation,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        int total = operation.Moves.Count;
        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var move = operation.Moves[i];
            try
            {
                if (operation.IsCopy)
                {
                    await Task.Run(() => File.Delete(move.DestPath), ct);
                }
                else
                {
                    var sourceDir = Path.GetDirectoryName(move.SourcePath);
                    if (sourceDir is not null)
                        Directory.CreateDirectory(sourceDir);

                    await Task.Run(() => File.Move(move.DestPath, move.SourcePath), ct);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to undo operation for file: {Dest}", move.DestPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Access denied undoing file: {Dest}", move.DestPath);
            }
            progress?.Report((i + 1.0) / total);
        }
    }

    public async Task<List<DuplicateFileEntry>> FindDuplicatesAsync(
        string directory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(directory);

        var results = new List<DuplicateFileEntry>();
        var dirInfo = new DirectoryInfo(directory);
        if (!dirInfo.Exists) return results;

        var files = dirInfo.EnumerateFiles().Where(f => f.Length > 0).ToList();
        int total = files.Count;

        var sizeGroups = files.GroupBy(f => f.Length).Where(g => g.Count() > 1).ToList();
        int groupId = 0;

        int processed = 0;
        foreach (var group in sizeGroups)
        {
            ct.ThrowIfCancellationRequested();
            var filePaths = group.Select(f => f.FullName).ToList();
            var verified = new List<string>();

            for (int i = 0; i < filePaths.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                for (int j = i + 1; j < filePaths.Count; j++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (await ByteCompareAsync(filePaths[i], filePaths[j], ct))
                        {
                            if (!verified.Contains(filePaths[i]))
                                verified.Add(filePaths[i]);
                            if (!verified.Contains(filePaths[j]))
                                verified.Add(filePaths[j]);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Error comparing files: {File1} vs {File2}", filePaths[i], filePaths[j]);
                    }
                }
                processed++;
                progress?.Report((double)processed / total);
            }

            if (verified.Count > 1)
            {
                var gid = $"Group {groupId + 1}";
                foreach (var path in verified)
                {
                    results.Add(new DuplicateFileEntry
                    {
                        FilePath = path,
                        Size = new FileInfo(path).Length,
                        GroupId = gid,
                        IsSelected = true,
                        GroupCount = $"{verified.Count} copies"
                    });
                }
                groupId++;
            }
        }

        logger.LogInformation("Found {DuplicateCount} duplicates in {GroupCount} groups in '{Directory}'", results.Count, groupId, directory);
        return results;
    }

    private static async Task<bool> ByteCompareAsync(string file1, string file2, CancellationToken ct)
    {
        const int bufferSize = 4096;
        await using var s1 = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, true);
        await using var s2 = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, true);

        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var read1 = await s1.ReadAsync(buffer1, ct);
            var read2 = await s2.ReadAsync(buffer2, ct);

            if (read1 != read2) return false;
            if (read1 == 0) return true;

            if (!buffer1.AsSpan(0, read1).SequenceEqual(buffer2.AsSpan(0, read2)))
                return false;
        }
    }

    private static string GetCategory(
        string extension,
        IReadOnlyList<ExtensionRule>? customRules)
    {
        var ext = extension.ToLowerInvariant();

        if (customRules is not null)
        {
            foreach (var rule in customRules)
            {
                var ruleExt = rule.Extension.Trim().ToLowerInvariant();
                if (!ruleExt.StartsWith("."))
                    ruleExt = "." + ruleExt;

                if (ext == ruleExt)
                    return rule.CategoryName;
            }
        }

        if (ImageExtensions.Contains(ext)) return "Images";
        if (DocumentExtensions.Contains(ext)) return "Documents";
        if (VideoExtensions.Contains(ext)) return "Videos";
        if (AudioExtensions.Contains(ext)) return "Audio";
        if (ArchiveExtensions.Contains(ext)) return "Archives";
        if (CodeExtensions.Contains(ext)) return "Code";
        return "Other";
    }

    private static string ResolveFolderName(string category, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return category;

        var now = DateTimeOffset.Now;
        return pattern
            .Replace("{category}", category)
            .Replace("{YYYY}", now.Year.ToString("D4"))
            .Replace("{MM}", now.Month.ToString("D2"))
            .Replace("{DD}", now.Day.ToString("D2"));
    }

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".tiff", ".tif", ".ico", ".heic"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf",
        ".md", ".csv", ".odt", ".ods", ".odp", ".epub", ".mobi"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".zst", ".iso"
    };

    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".py", ".js", ".ts", ".html", ".css", ".cpp", ".c", ".h", ".hpp",
        ".java", ".json", ".xml", ".yaml", ".yml", ".sh", ".bat", ".ps1", ".sql",
        ".rb", ".go", ".rs", ".swift", ".kt", ".dart", ".php", ".r", ".m", ".mm",
        ".sln", ".csproj", ".props", ".targets"
    };
}
