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
        var lockObj = new object();

        progress.Report(0.0);
        await ScanTempFolders(results, lockObj, ct, progress, 0.00, 0.15);
        await ScanCacheFolders(results, lockObj, ct, progress, 0.15, 0.30);
        await FindLargeFiles(results, lockObj, ct, progress, 0.30, 0.60);
        await FindDuplicates(results, results, lockObj, ct, progress, 0.60, 1.00);
        progress.Report(1.0);

        return results;
    }

    private static void ReportSubProgress(IProgress<double> progress, double start, double end, int processed, int total)
    {
        var fraction = total > 0 ? (double)processed / total : 1.0;
        progress.Report(start + (end - start) * Math.Min(1.0, fraction));
    }

    private Task ScanTempFolders(List<AnalyzerItem> results, object lockObj, CancellationToken ct,
        IProgress<double> progress, double start, double end)
    {
        var dirs = new List<string>
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")
        };

        var scanned = new List<(string Dir, List<string> Files)>();
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                scanned.Add((dir, Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).ToList()));
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

        var total = scanned.Sum(s => s.Files.Count);
        var processed = 0;
        foreach (var (dir, files) in scanned)
        {
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
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
                catch (IOException ex)
                {
                    logger.LogDebug(ex, "IO error reading temp file: {File}", file);
                }
                processed++;
                if (processed % 50 == 0 || processed == total)
                    ReportSubProgress(progress, start, end, processed, total);
            }
            logger.LogInformation("Scanned temp folder: {Dir} ({Count} files)", dir, files.Count);
        }

        ReportSubProgress(progress, start, end, processed, total);
        return Task.CompletedTask;
    }

    private Task ScanCacheFolders(List<AnalyzerItem> results, object lockObj, CancellationToken ct,
        IProgress<double> progress, double start, double end)
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

        var scanned = new List<(string Dir, List<string> Files)>();
        foreach (var dir in cacheDirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                var files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
                    .Where(f => CacheExtensions.Contains(Path.GetExtension(f)))
                    .ToList();
                scanned.Add((dir, files));
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

        var total = scanned.Sum(s => s.Files.Count);
        var processed = 0;
        foreach (var (dir, files) in scanned)
        {
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
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
                catch (IOException ex)
                {
                    logger.LogDebug(ex, "IO error reading cache file: {File}", file);
                }
                processed++;
                if (processed % 50 == 0 || processed == total)
                    ReportSubProgress(progress, start, end, processed, total);
            }
        }

        ReportSubProgress(progress, start, end, processed, total);
        return Task.CompletedTask;
    }

    private Task FindLargeFiles(List<AnalyzerItem> results, object lockObj, CancellationToken ct,
        IProgress<double> progress, double start, double end)
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

        var profiles = new List<string>();
        foreach (var drive in drives)
        {
            try
            {
                var userDir = Path.Combine(drive.RootDirectory.FullName, "Users");
                if (!Directory.Exists(userDir)) continue;
                foreach (var profile in Directory.EnumerateDirectories(userDir))
                    profiles.Add(profile);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Access denied to drive: {Drive}", drive.Name);
            }
        }

        var processedProfiles = 0;
        foreach (var profile in profiles)
        {
            ct.ThrowIfCancellationRequested();
            var completedDirs = 0;
            List<string> topLevelDirs;
            try
            {
                topLevelDirs = Directory.GetDirectories(profile).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                topLevelDirs = new List<string>();
            }

            if (topLevelDirs.Count == 0)
                topLevelDirs.Add(profile);

            foreach (var dir in topLevelDirs)
            {
                ct.ThrowIfCancellationRequested();
                EnumerateLargeFiles(dir, results, lockObj, ct);
                completedDirs++;
                var profileFraction = (processedProfiles + (double)completedDirs / topLevelDirs.Count) / profiles.Count;
                progress.Report(start + (end - start) * Math.Min(1.0, profileFraction));
            }
            processedProfiles++;
        }

        progress.Report(end);
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

    private async Task FindDuplicates(List<AnalyzerItem> source, List<AnalyzerItem> results, object lockObj, CancellationToken ct,
        IProgress<double> progress, double start, double end)
    {
        var sizeGroups = source.Where(i => i.Size > 0 && i.Category != "LargeFile")
            .GroupBy(i => i.Size)
            .Where(g => g.Count() > 1)
            .ToList();

        var candidates = sizeGroups.Sum(g => g.Count());
        var processed = 0;
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

                processed++;
                if (processed % 10 == 0 || processed == candidates)
                    ReportSubProgress(progress, start, end, processed, candidates);
            }

            if (hashes.Count > 1)
                groupId++;
        }

        ReportSubProgress(progress, start, end, processed, candidates);
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
