using FluentFold.Models;

namespace FluentFold.Services;

/// <summary>Provides file renaming with pattern-based preview and execution.</summary>
public interface IRenamingService
{
    /// <summary>Previews the result of a rename operation without modifying files.</summary>
    /// <param name="files">The files to rename.</param>
    /// <param name="pattern">The naming pattern (supports {name}, {n}, {ext}).</param>
    /// <param name="startNumber">The starting counter value for {n}.</param>
    /// <param name="padding">Zero-padding width for {n}.</param>
    /// <returns>A read-only list of rename previews.</returns>
    IReadOnlyList<RenamePreview> Preview(IReadOnlyList<FileEntry> files, string pattern, int startNumber, int padding);

    /// <summary>Applies the rename operation to the specified files.</summary>
    /// <param name="files">The files to rename.</param>
    /// <param name="pattern">The naming pattern.</param>
    /// <param name="startNumber">The starting counter value.</param>
    /// <param name="padding">Zero-padding width for the counter.</param>
    /// <returns>A read-only list of rename results.</returns>
    Task<IReadOnlyList<RenamePreview>> ApplyAsync(IReadOnlyList<FileEntry> files, string pattern, int startNumber, int padding);
}
