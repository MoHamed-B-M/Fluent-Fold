using FluentFold.Models;

namespace FluentFold.Services;

/// <summary>Provides file organization, scanning, duplicate detection, and undo capabilities.</summary>
public interface IOrganizerService
{
    /// <summary>Scans a directory and categorizes all files by extension.</summary>
    /// <param name="directory">The directory to scan.</param>
    /// <param name="customRules">Optional custom extension-to-category mappings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of categorized file entries.</returns>
    Task<List<FileEntry>> ScanFolderAsync(
        string directory,
        IReadOnlyList<ExtensionRule>? customRules = null,
        CancellationToken ct = default);

    /// <summary>Organizes files into categorized subdirectories.</summary>
    /// <param name="rootDirectory">The root directory containing the files.</param>
    /// <param name="files">The files to organize.</param>
    /// <param name="copyMode">If true, copies files; otherwise moves them.</param>
    /// <param name="folderNamingPattern">Optional pattern for naming folders (supports {category}, {YYYY}, {MM}, {DD}).</param>
    /// <param name="customRules">Optional custom extension-to-category rules.</param>
    /// <param name="progress">Reports progress from 0.0 to 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An operation record that can be used to undo the organization.</returns>
    Task<OrganizeOperation> OrganizeFilesAsync(
        string rootDirectory,
        IReadOnlyList<FileEntry> files,
        bool copyMode = false,
        string? folderNamingPattern = null,
        IReadOnlyList<ExtensionRule>? customRules = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>Undoes a previous organization operation by restoring files to their original locations.</summary>
    /// <param name="operation">The operation to undo.</param>
    /// <param name="progress">Reports progress from 0.0 to 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UndoOperationAsync(
        OrganizeOperation operation,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>Finds duplicate files in a directory by comparing file sizes and contents.</summary>
    /// <param name="directory">The directory to scan.</param>
    /// <param name="progress">Reports progress from 0.0 to 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of duplicate file entries grouped by content.</returns>
    Task<List<DuplicateFileEntry>> FindDuplicatesAsync(
        string directory,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
