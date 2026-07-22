namespace FluentFold.Models;

/// <summary>Records a single file move operation for undo purposes.</summary>
public class FileMove
{
    /// <summary>The original file path before the move.</summary>
    public string SourcePath { get; init; } = string.Empty;
    /// <summary>The destination file path after the move.</summary>
    public string DestPath { get; init; } = string.Empty;
    /// <summary>The file name.</summary>
    public string FileName { get; init; } = string.Empty;
    /// <summary>The category assigned during organization.</summary>
    public string Category { get; init; } = string.Empty;
}
