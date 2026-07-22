using CommunityToolkit.Mvvm.ComponentModel;
using FluentFold.Helpers;

namespace FluentFold.Models;

/// <summary>Represents a file found during system analysis, categorized by type.</summary>
public sealed partial class AnalyzerItem : ObservableObject
{
    /// <summary>The full path to the file.</summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>The file name extracted from the path.</summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);
    /// <summary>The analysis category (e.g., "Temp", "Cache", "Duplicate", "LargeFile").</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>The file size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>A human-readable formatted size string.</summary>
    public string SizeFormatted => FormatHelper.FormatSize(Size);
    /// <summary>The duplicate group identifier, if applicable.</summary>
    public string? DuplicateGroup { get; set; }

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private bool _isSelected;
#pragma warning restore MVVMTK0045
}
