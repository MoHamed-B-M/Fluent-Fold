namespace FluentFold.Models;

/// <summary>Represents a completed organization operation that can be undone.</summary>
public class OrganizeOperation
{
    /// <summary>When the operation was performed.</summary>
    public DateTimeOffset Timestamp { get; init; }
    /// <summary>The root folder where the operation took place.</summary>
    public string SourceFolder { get; init; } = string.Empty;
    /// <summary>The list of file moves performed.</summary>
    public IReadOnlyList<FileMove> Moves { get; init; } = Array.Empty<FileMove>();
    /// <summary>Whether the operation was a copy (true) or move (false).</summary>
    public bool IsCopy { get; init; }
}
