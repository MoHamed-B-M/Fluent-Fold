using FluentFold.Models;

namespace FluentFold.Services;

public interface IUndoService
{
    Task<bool> CanUndoAsync();
    Task<IReadOnlyList<FileEntry>?> GetUndoSnapshotAsync();
    Task RecordSnapshotAsync(IReadOnlyList<FileEntry> beforeState);
    Task UndoAsync();
}
