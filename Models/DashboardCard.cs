using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentFold.Models;

/// <summary>Represents a summary card displayed on the analyzer dashboard.</summary>
public sealed partial class DashboardCard : ObservableObject
{
    /// <summary>The display title of the card.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>The icon glyph for the card.</summary>
    public string Icon { get; set; } = string.Empty;
    /// <summary>The item count for this category.</summary>
    public int Count { get; set; }
    /// <summary>A formatted size string for the category total.</summary>
    public string SizeFormatted { get; set; } = string.Empty;
    /// <summary>The percentage of total scan size this category represents.</summary>
    public double Percent { get; set; }
    /// <summary>The collection of items in this category.</summary>
    public ObservableCollection<AnalyzerItem> Items { get; set; } = new();

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private bool _isExpanded;
#pragma warning restore MVVMTK0045
}
