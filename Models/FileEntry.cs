namespace FluentFold.Models;

/// <summary>Represents a file discovered during folder scanning.</summary>
public class FileEntry
{
    /// <summary>The file name (with extension).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The full file path.</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>The assigned category (e.g., "Images", "Documents").</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>The last write time in UTC.</summary>
    public DateTimeOffset DateModified { get; set; }
    /// <summary>The creation time in UTC.</summary>
    public DateTimeOffset DateCreated { get; set; }
    /// <summary>The last access time in UTC.</summary>
    public DateTimeOffset DateAccessed { get; set; }
    /// <summary>The file size in bytes.</summary>
    public long Size { get; set; }
}
