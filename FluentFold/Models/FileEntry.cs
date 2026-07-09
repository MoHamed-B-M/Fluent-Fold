using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace FluentFold.Models;

public enum FileCategory
{
    Images,
    Documents,
    Videos,
    Audio,
    Archives,
    Code,
    Other
}

public class FileEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Extension { get; set; } = "";
    public FileCategory Category { get; set; }
    public bool IsFile { get; set; } = true;
    public long Size { get; set; }
    public string? OriginalPath { get; set; }
}

public class FolderSummary
{
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    public Dictionary<FileCategory, int> CategoryCounts { get; set; } = new();
}

public class OrganizeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int FilesMoved { get; set; }
    public int RenamedCount { get; set; }
}