using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using FluentFold.Models;
using FluentFold.Services;
using FluentFold.Helpers;

namespace FluentFold.ViewModels;

public sealed partial class AnalyzerViewModel : ObservableObject
{
    private readonly IAnalyzerService _analyzer;
    private readonly ILogger<AnalyzerViewModel> _logger;
    private CancellationTokenSource? _cts;

    public AnalyzerViewModel(IAnalyzerService analyzer, ILogger<AnalyzerViewModel> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
    }

    public ObservableCollection<AnalyzerItem> Items { get; } = new();
    public ObservableCollection<AnalyzerItem> TempItems { get; } = new();
    public ObservableCollection<AnalyzerItem> CacheItems { get; } = new();
    public ObservableCollection<AnalyzerItem> DuplicateItems { get; } = new();
    public ObservableCollection<DashboardCard> DashboardCards { get; } = new();

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdleVisibility))]
    [NotifyPropertyChangedFor(nameof(ScanningVisibility))]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    private bool _isIdle = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdleVisibility))]
    [NotifyPropertyChangedFor(nameof(ScanningVisibility))]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    private bool _isScanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdleVisibility))]
    [NotifyPropertyChangedFor(nameof(ScanningVisibility))]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    private bool _hasResults;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    private double _scanProgress;

    [ObservableProperty]
    private string _totalReclaimable = string.Empty;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDeleting))]
    private bool _isDeleting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TempSizeText))]
    private long _tempSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CacheSizeText))]
    private long _cacheSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DuplicateSizeText))]
    private long _duplicateSize;

    public string TempSizeText => FormatHelper.FormatSize(TempSize);
    public string CacheSizeText => FormatHelper.FormatSize(CacheSize);
    public string DuplicateSizeText => FormatHelper.FormatSize(DuplicateSize);

    [ObservableProperty]
    private int _tempCount;

    [ObservableProperty]
    private int _cacheCount;

    [ObservableProperty]
    private int _duplicateCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TempPercent))]
    [NotifyPropertyChangedFor(nameof(CachePercent))]
    [NotifyPropertyChangedFor(nameof(DuplicatePercent))]
    private long _totalScanSize;
#pragma warning restore MVVMTK0045

    public double TempPercent => TotalScanSize > 0 ? TempSize / (double)TotalScanSize : 0;
    public double CachePercent => TotalScanSize > 0 ? CacheSize / (double)TotalScanSize : 0;
    public double DuplicatePercent => TotalScanSize > 0 ? DuplicateSize / (double)TotalScanSize : 0;

    public bool IsNotDeleting => !IsDeleting;
    public Visibility IdleVisibility => IsIdle ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ScanningVisibility => IsScanning ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ResultsVisibility => HasResults ? Visibility.Visible : Visibility.Collapsed;
    public string ProgressPercent => IsScanning ? $"Scanning: {(int)(ScanProgress * 100)}%" : string.Empty;

    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (IsScanning) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsIdle = false;
        IsScanning = true;
        HasResults = false;
        Items.Clear();
        TempItems.Clear();
        CacheItems.Clear();
        DuplicateItems.Clear();
        ScanProgress = 0;
        TotalReclaimable = string.Empty;
        TotalItems = 0;
        TempSize = 0;
        CacheSize = 0;
        DuplicateSize = 0;
        TempCount = 0;
        CacheCount = 0;
        DuplicateCount = 0;
        TotalScanSize = 0;

        try
        {
            var progress = new Progress<double>(p => ScanProgress = p * 0.85);
            var results = await _analyzer.ScanAsync(progress, ct);

            ScanProgress = 0.85;

            foreach (var item in results.OrderByDescending(i => i.Size))
                Items.Add(item);

            var totalBytes = results.Sum(i => i.Size);
            TotalReclaimable = FormatHelper.FormatSize(totalBytes);
            TotalItems = results.Count;
            TotalScanSize = totalBytes;

            PopulateCategoryGroups(results);

            ScanProgress = 1.0;
            HasResults = true;
            _logger.LogInformation("Scan complete: {Count} items, {Size} total", results.Count, totalBytes);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scan cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
        }
        finally
        {
            IsScanning = false;
            if (!HasResults && Items.Count == 0)
                IsIdle = true;
        }
    }

    private void PopulateCategoryGroups(List<AnalyzerItem> results)
    {
        DashboardCards.Clear();

        foreach (var item in results)
        {
            switch (item.Category)
            {
                case "Temp":
                    TempItems.Add(item);
                    TempSize += item.Size;
                    TempCount++;
                    break;
                case "Cache":
                    CacheItems.Add(item);
                    CacheSize += item.Size;
                    CacheCount++;
                    break;
                case "Duplicate":
                    DuplicateItems.Add(item);
                    DuplicateSize += item.Size;
                    DuplicateCount++;
                    break;
                case "LargeFile":
                    TempItems.Add(item);
                    TempSize += item.Size;
                    TempCount++;
                    break;
            }
        }

        AddDashboardCard("Temporary Files", "\uE71B", TempCount, TempSizeText, TempPercent, TempItems);
        AddDashboardCard("Cache Files", "\uE7C1", CacheCount, CacheSizeText, CachePercent, CacheItems);
        AddDashboardCard("Duplicate Files", "\uE8A1", DuplicateCount, DuplicateSizeText, DuplicatePercent, DuplicateItems);
    }

    private void AddDashboardCard(string title, string icon, int count, string sizeFormatted, double percent, ObservableCollection<AnalyzerItem> items)
    {
        if (count <= 0) return;

        DashboardCards.Add(new DashboardCard
        {
            Title = title,
            Icon = icon,
            Count = count,
            SizeFormatted = sizeFormatted,
            Percent = percent,
            Items = items
        });
    }

    [RelayCommand]
    private void CancelScan()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Items)
            item.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in Items)
            item.IsSelected = false;
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0) return;

        IsDeleting = true;
        int deleted = 0;
        long freedBytes = 0;

        try
        {
            foreach (var item in selected)
            {
                try
                {
                    if (File.Exists(item.FilePath))
                    {
                        File.Delete(item.FilePath);
                        freedBytes += item.Size;
                        deleted++;
                    }
                    Items.Remove(item);
                    RemoveFromCategory(item);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied deleting file: {File}", item.FilePath);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "IO error deleting file: {File}", item.FilePath);
                }
            }
        }
        finally
        {
            IsDeleting = false;
        }

        RecalcTotals();
    }

    private void RemoveFromCategory(AnalyzerItem item)
    {
        switch (item.Category)
        {
            case "Temp":
            case "LargeFile":
                TempItems.Remove(item);
                TempSize -= item.Size;
                TempCount--;
                break;
            case "Cache":
                CacheItems.Remove(item);
                CacheSize -= item.Size;
                CacheCount--;
                break;
            case "Duplicate":
                DuplicateItems.Remove(item);
                DuplicateSize -= item.Size;
                DuplicateCount--;
                break;
        }
    }

    private void RecalcTotals()
    {
        var totalBytes = Items.Sum(i => i.Size);
        TotalReclaimable = FormatHelper.FormatSize(totalBytes);
        TotalItems = Items.Count;
        TotalScanSize = totalBytes;
        HasResults = Items.Count > 0;
        if (!HasResults)
            IsIdle = true;
        RebuildDashboardCards();
    }

    private void RebuildDashboardCards()
    {
        DashboardCards.Clear();
        AddDashboardCard("Temporary Files", "\uE71B", TempCount, TempSizeText, TempPercent, TempItems);
        AddDashboardCard("Cache Files", "\uE7C1", CacheCount, CacheSizeText, CachePercent, CacheItems);
        AddDashboardCard("Duplicate Files", "\uE8A1", DuplicateCount, DuplicateSizeText, DuplicatePercent, DuplicateItems);
    }
}
