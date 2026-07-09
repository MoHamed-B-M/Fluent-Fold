using System;
using System.Collections.Generic;
using System.Linq;
using FluentFold.Models;

namespace FluentFold.Services;

public static class FileCategorizer
{
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico", ".tiff", ".tif", ".heic", ".heif" };

    private static readonly HashSet<string> DocExts = new(StringComparer.OrdinalIgnoreCase)
    { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".csv", ".md", ".odt", ".ods", ".odp", ".pages", ".numbers", ".key" };

    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
    { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp" };

    private static readonly HashSet<string> AudioExts = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus", ".aiff" };

    private static readonly HashSet<string> ArchiveExts = new(StringComparer.OrdinalIgnoreCase)
    { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab", ".dmg" };

    private static readonly HashSet<string> CodeExts = new(StringComparer.OrdinalIgnoreCase)
    { ".cs", ".py", ".js", ".ts", ".jsx", ".tsx", ".html", ".css", ".scss", ".less", ".cpp", ".c", ".h", ".hpp",
      ".java", ".kt", ".swift", ".go", ".rs", ".rb", ".php", ".sh", ".bat", ".ps1", ".sql", ".xml", ".json", ".yaml", ".yml",
      ".toml", ".ini", ".cfg", ".dockerfile", ".sln", ".csproj", ".vcxproj" };

    public static FileCategory Categorize(string extension)
    {
        if (ImageExts.Contains(extension)) return FileCategory.Images;
        if (DocExts.Contains(extension)) return FileCategory.Documents;
        if (VideoExts.Contains(extension)) return FileCategory.Videos;
        if (AudioExts.Contains(extension)) return FileCategory.Audio;
        if (ArchiveExts.Contains(extension)) return FileCategory.Archives;
        if (CodeExts.Contains(extension)) return FileCategory.Code;
        return FileCategory.Other;
    }

    public static string CategoryToFolderName(FileCategory category) => category switch
    {
        FileCategory.Images => "Images",
        FileCategory.Documents => "Documents",
        FileCategory.Videos => "Videos",
        FileCategory.Audio => "Audio",
        FileCategory.Archives => "Archives",
        FileCategory.Code => "Code",
        FileCategory.Other => "Other",
        _ => "Other"
    };
}