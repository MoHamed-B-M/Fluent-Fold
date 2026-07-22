using CommunityToolkit.Mvvm.ComponentModel;
using FluentFold.Helpers;

namespace FluentFold.Models;

/// <summary>Represents a file eligible for cleanup (zero-byte, stale, or temporary).</summary>
public sealed partial class CleanupFileItem : ObservableObject
{
    /// <summary>The full path to the file.</summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>The file name extracted from the path.</summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);
    /// <summary>The file size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>A human-readable formatted size string.</summary>
    public string SizeFormatted => FormatHelper.FormatSize(Size);
    /// <summary>The reason this file was flagged for cleanup.</summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>The last modified timestamp.</summary>
    public DateTimeOffset LastModified { get; set; }

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private bool _isSelected = true;
#pragma warning restore MVVMTK0045
}
