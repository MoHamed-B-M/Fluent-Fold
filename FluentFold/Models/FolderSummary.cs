using System.Collections.Generic;

namespace FluentFold.Models;

public class FolderSummary
{
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    public Dictionary<FileCategory, int> CategoryCounts { get; set; } = new();
}

public class OrganizeResult
{
    public int FilesMoved { get; set; }
    public List<string> MovedItems { get; set; } = new();
}

public class RenameResult
{
    public int FilesRenamed { get; set; }
    public List<(string OldName, string NewName)> RenamedItems { get; set; } = new();
}

public class UndoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
