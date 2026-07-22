using FluentFold.Helpers;

namespace FluentFold.Models;

/// <summary>Represents a duplicate file identified by content comparison.</summary>
public class DuplicateFileEntry
{
    /// <summary>The full path to the duplicate file.</summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>The file name extracted from the path.</summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);
    /// <summary>The file size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>A human-readable formatted size string.</summary>
    public string SizeFormatted => FormatHelper.FormatSize(Size);
    /// <summary>The duplicate group identifier.</summary>
    public string GroupId { get; set; } = string.Empty;
    /// <summary>Whether this file is selected for an operation.</summary>
    public bool IsSelected { get; set; }
    /// <summary>A display string indicating how many copies exist in the group.</summary>
    public string GroupCount { get; set; } = string.Empty;
}
