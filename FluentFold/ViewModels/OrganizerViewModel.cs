using CommunityToolkit.Mvvm.ComponentModel;
using FluentFold.Models;
using FluentFold.Services;
using System.Collections.ObjectModel;

namespace FluentFold.ViewModels;

public partial class OrganizerViewModel : ObservableObject
{
    internal OrganizerService _organizer = new();
    internal UndoService _undo;

    [ObservableProperty] private string _selectedFolderPath = "";
    [ObservableProperty] private string _renamePattern = "";
    [ObservableProperty] private bool _organizeByCategory = true;
    [ObservableProperty] private bool _renameFiles;
    [ObservableProperty] private bool _isWorking;
    [ObservableProperty] private FolderSummary? _folderSummary;
    [ObservableProperty] private bool _canUndo;

    public ObservableCollection<CategoryCountItem> CategoryItems { get; } = new();

    public OrganizerViewModel()
    {
        _undo = new UndoService(_organizer);
    }
}

public class CategoryCountItem
{
    public string CategoryName { get; set; } = "";
    public int Count { get; set; }
}