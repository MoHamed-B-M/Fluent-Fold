using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using FluentFold.Models;

namespace FluentFold.Services;

public sealed class AnalyzerService(ILogger<AnalyzerService> logger) : IAnalyzerService
{
    private static readonly HashSet<string> CacheExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tmp", ".log", ".cache", ".dmp", ".etl", ".blf", ".regtrans-ms"
    };

    public async Task<List<AnalyzerItem>> ScanAsync(IProgress<double> progress, CancellationToken ct)
    {
        var results = new List<AnalyzerItem>();
        var steps = new List<Func<Task>>();
        var lockObj = new object();

        steps.Add(() => ScanTempFolders(results, lockObj, ct));
        steps.Add(() => ScanCacheFolders(results, lockObj, ct));
        steps.Add(() => FindLargeFiles(results, lockObj, ct));

        for (int i = 0; i < steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await steps[i]();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Scan step {Step} failed", i);
            }
            progress.Report((i + 1.0) / steps.Count);
        }

        var dupResults = new List<AnalyzerItem>();
        await FindDuplicates(results, dupResults, lockObj, ct);
        results.AddRange(dupResults);

        return results;
    }

    private Task ScanTempFolders(List<AnalyzerItem> results, object lockObj, CancellationToken ct)
    {
        var dirs = new List<string>
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    ct.ThrowIfCancellationRequested();
                    var info = new FileInfo(file);
                    lock (lockObj)
                    {
                        results.Add(new AnalyzerItem
                        {
                            FilePath = file,
                            Category = "Temp",
                            Size = info.Length
                        });
                    }
                }
                logger.LogInformation("Scanned temp folder: {Dir} ({Count} files)", dir, results.Count);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Access denied to temp folder: {Dir}", dir);
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogWarning(ex, "Temp folder not found: {Dir}", dir);
            }
        }

        return Task.CompletedTask;
    }

    private Task ScanCacheFolders(List<AnalyzerItem> results, object lockObj, CancellationToken ct)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var cacheDirs = new List<string>
        {
            Path.Combine(localAppData, "Microsoft", "Windows", "INetCache"),
            Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"),
            Path.Combine(localAppData, "Microsoft", "Windows", "WER"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)),
            Path.Combine(localAppData, "Temp")
        };

        foreach (var dir in cacheDirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    ct.ThrowIfCancellationRequested();
                    var ext = Path.GetExtension(file);
                    if (!CacheExtensions.Contains(ext)) continue;
                    var info = new FileInfo(file);
                    lock (lockObj)
                    {
                        results.Add(new AnalyzerItem
                        {
                            FilePath = file,
                            Category = "Cache",
                            Size = info.Length
                        });
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Access denied to cache folder: {Dir}", dir);
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogWarning(ex, "Cache folder not found: {Dir}", dir);
            }
        }

        return Task.CompletedTask;
    }

    private Task FindLargeFiles(List<AnalyzerItem> results, object lockObj, CancellationToken ct)
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
        foreach (var drive in drives)
        {
            try
            {
                var userDir = Path.Combine(drive.RootDirectory.FullName, "Users");
                if (!Directory.Exists(userDir)) continue;
                foreach (var userProfile in Directory.EnumerateDirectories(userDir))
                {
                    try
                    {
                        EnumerateLargeFiles(userProfile, results, lockObj, ct);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger.LogWarning(ex, "Access denied to profile: {Profile}", userProfile);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Access denied to drive: {Drive}", drive.Name);
            }
        }

        return Task.CompletedTask;
    }

    private void EnumerateLargeFiles(string directory, List<AnalyzerItem> results, object lockObj, CancellationToken ct)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length >= 100L * 1024 * 1024)
                    {
                        lock (lockObj)
                        {
                            results.Add(new AnalyzerItem
                            {
                                FilePath = file,
                                Category = "LargeFile",
                                Size = info.Length
                            });
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException ex)
                {
                    logger.LogDebug(ex, "IO error reading file: {File}", file);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "IO error enumerating directory: {Dir}", directory);
        }
    }

    private async Task FindDuplicates(List<AnalyzerItem> source, List<AnalyzerItem> results, object lockObj, CancellationToken ct)
    {
        var sizeGroups = source.Where(i => i.Size > 0 && i.Category != "LargeFile")
            .GroupBy(i => i.Size)
            .Where(g => g.Count() > 1)
            .ToList();

        int groupId = 0;
        foreach (var group in sizeGroups)
        {
            ct.ThrowIfCancellationRequested();
            var hashes = new Dictionary<string, string>();

            foreach (var item in group)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var hash = await ComputeHashAsync(item.FilePath, ct);
                    if (hash is null) continue;
                    if (hashes.TryGetValue(hash, out var existing))
                    {
                        lock (lockObj)
                        {
                            results.Add(new AnalyzerItem
                            {
                                FilePath = item.FilePath,
                                Category = "Duplicate",
                                Size = item.Size,
                                DuplicateGroup = $"Group {groupId}"
                            });
                            source.Remove(item);
                        }
                    }
                    else
                    {
                        hashes[hash] = item.FilePath;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error processing duplicate candidate: {File}", item.FilePath);
                }
            }

            if (hashes.Count > 1)
                groupId++;
        }
    }

    private static async Task<string?> ComputeHashAsync(string filePath, CancellationToken ct)
    {
        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
            using var md5 = MD5.Create();
            var hashBytes = await md5.ComputeHashAsync(stream, ct);
            return Convert.ToHexStringLower(hashBytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
