namespace FluentFold.Helpers;

/// <summary>
/// Provides utility methods for formatting values commonly displayed in the UI.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats a byte count into a human-readable string (e.g., "1.5 MB").
    /// </summary>
    /// <param name="bytes">The byte count to format.</param>
    /// <returns>A formatted string with the appropriate unit (B, KB, MB, or GB).</returns>
    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
