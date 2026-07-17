using FluentFold.Models;

namespace FluentFold.Services;

public sealed class OrganizerService : IOrganizerService
{
    public async Task<IReadOnlyList<FileEntry>> GetFilesAsync(string directory)
    {
        var results = new List<FileEntry>();
        var dirInfo = new DirectoryInfo(directory);

        if (!dirInfo.Exists)
            return results;

        foreach (var file in dirInfo.EnumerateFiles())
        {
            results.Add(new FileEntry
            {
                Name = file.Name,
                Path = file.FullName,
                DateModified = file.LastWriteTimeUtc
            });
        }

        return await Task.FromResult(results.AsReadOnly());
    }

    public Task OrganizeFilesAsync(IReadOnlyList<FileEntry> files)
    {
        return Task.CompletedTask;
    }

    public Task RevertOrganizationAsync(string snapshotId)
    {
        return Task.CompletedTask;
    }
}
