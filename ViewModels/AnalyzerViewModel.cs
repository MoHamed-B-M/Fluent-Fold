using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private readonly Stopwatch _stopwatch = new();

    public AnalyzerViewModel(IAnalyzerService analyzer, ILogger<AnalyzerViewModel> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
    }

    public ObservableCollection<AnalyzerItem> Items { get; } = new();
    public ObservableCollection<AnalyzerItem> TempItems { get; } = new();
    public ObservableCollection<AnalyzerItem> CacheItems { get; } = new();
    public ObservableCollection<AnalyzerItem> DuplicateItems { get; } = new();

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
    [NotifyPropertyChangedFor(nameof(EmptyResultsVisibility))]
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
    [NotifyPropertyChangedFor(nameof(TempCardVisibility))]
    private int _tempCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CacheCardVisibility))]
    private int _cacheCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DuplicateCardVisibility))]
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
    public Visibility EmptyResultsVisibility => HasResults && TotalItems == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TempCardVisibility => TempCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CacheCardVisibility => CacheCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DuplicateCardVisibility => DuplicateCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public string ProgressPercent => IsScanning ? $"Scanning: {(int)(ScanProgress * 100)}%" : string.Empty;

    private string _elapsedText = string.Empty;
    public string ElapsedText
    {
        get => _elapsedText;
        set
        {
            if (_elapsedText == value) return;
            _elapsedText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressTimeText));
        }
    }

    private string _remainingText = string.Empty;
    public string RemainingText
    {
        get => _remainingText;
        set
        {
            if (_remainingText == value) return;
            _remainingText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressTimeText));
        }
    }

    public string ProgressTimeText => $"Elapsed: {ElapsedText} · Estimated remaining: {RemainingText}";

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
            _stopwatch.Restart();
            var progress = new Progress<double>(p => OnScanProgress(p));
            var results = await Task.Run(async () => await _analyzer.ScanAsync(progress, ct));

            ScanProgress = 1.0;

            foreach (var item in results.OrderByDescending(i => i.Size))
                Items.Add(item);

            var totalBytes = results.Sum(i => i.Size);
            TotalReclaimable = FormatHelper.FormatSize(totalBytes);
            TotalItems = results.Count;
            TotalScanSize = totalBytes;

            PopulateCategoryGroups(results);

            HasResults = true;
            _stopwatch.Stop();
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

    private void OnScanProgress(double p)
    {
        ScanProgress = p;

        var elapsed = _stopwatch.Elapsed;
        double remainingSeconds = p > 0.02 ? elapsed.TotalSeconds * (1 - p) / p : 0;
        ElapsedText = FormatDuration(elapsed);
        RemainingText = remainingSeconds > 0 ? FormatDuration(TimeSpan.FromSeconds(remainingSeconds)) : "—";
    }

    private static string FormatDuration(TimeSpan t)
    {
        return t.TotalMinutes >= 1
            ? $"{(int)t.TotalMinutes}m {t.Seconds}s"
            : $"{t.Seconds}s";
    }

    private void PopulateCategoryGroups(List<AnalyzerItem> results)
    {
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
    private void ToggleTempSelection()
    {
        var allSelected = TempItems.Count > 0 && TempItems.All(i => i.IsSelected);
        foreach (var item in TempItems)
            item.IsSelected = !allSelected;
    }

    [RelayCommand]
    private void ToggleCacheSelection()
    {
        var allSelected = CacheItems.Count > 0 && CacheItems.All(i => i.IsSelected);
        foreach (var item in CacheItems)
            item.IsSelected = !allSelected;
    }

    [RelayCommand]
    private void ToggleDuplicateSelection()
    {
        var allSelected = DuplicateItems.Count > 0 && DuplicateItems.All(i => i.IsSelected);
        foreach (var item in DuplicateItems)
            item.IsSelected = !allSelected;
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
    }
}
