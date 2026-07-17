using FluentFold.Models;

namespace FluentFold.Services;

public interface IOrganizerService
{
    Task<IReadOnlyList<FileEntry>> GetFilesAsync(string directory);
    Task OrganizeFilesAsync(IReadOnlyList<FileEntry> files);
    Task RevertOrganizationAsync(string snapshotId);
}
