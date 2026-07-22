using FluentFold.Models;

namespace FluentFold.Services;

/// <summary>Provides file system scanning and analysis capabilities.</summary>
public interface IAnalyzerService
{
    /// <summary>Scans the system for temporary, cache, large, and duplicate files.</summary>
    /// <param name="progress">Reports progress from 0.0 to 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of analyzed items found during the scan.</returns>
    Task<List<AnalyzerItem>> ScanAsync(IProgress<double> progress, CancellationToken ct);
}
