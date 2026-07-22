using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentFold.Models;

/// <summary>Maps a file extension to an organization category.</summary>
public sealed partial class ExtensionRule : ObservableObject
{
#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private string _categoryName = string.Empty;
#pragma warning restore MVVMTK0045
}
