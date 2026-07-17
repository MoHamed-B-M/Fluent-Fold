namespace FluentFold.Models;

public class FileEntry
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset DateModified { get; set; }
}
