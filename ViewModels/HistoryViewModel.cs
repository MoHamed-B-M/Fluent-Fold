using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using FluentFold.Models;
using FluentFold.Services;

namespace FluentFold.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IUndoService _undo;
    private readonly IOrganizerService _organizer;
    private readonly ILogger<HistoryViewModel> _logger;

    public HistoryViewModel(IUndoService undo, IOrganizerService organizer, ILogger<HistoryViewModel> logger)
    {
        _undo = undo;
        _organizer = organizer;
        _logger = logger;
        RefreshHistory();
    }

    public ObservableCollection<OrganizeOperation> Operations { get; } = new();

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoHistory))]
    private bool _hasHistory;

    [ObservableProperty]
    private bool _isWorking;

    public bool HasNoHistory => !HasHistory;
#pragma warning restore MVVMTK0045

    public void RefreshHistory()
    {
        Operations.Clear();
        foreach (var op in _undo.History)
            Operations.Add(op);

        HasHistory = Operations.Count > 0;
    }

    [RelayCommand]
    private async Task UndoOperationAsync(OrganizeOperation operation)
    {
        if (operation is null) return;

        IsWorking = true;
        try
        {
            await _organizer.UndoOperationAsync(operation);
            _undo.Remove(operation);
            RefreshHistory();
            _logger.LogInformation("Undid operation: {Count} moves", operation.Moves.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo operation");
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAllAsync()
    {
        var allOps = _undo.History.ToList();
        if (allOps.Count == 0) return;

        IsWorking = true;
        try
        {
            foreach (var op in allOps)
                await _organizer.UndoOperationAsync(op);

            _undo.Clear();
            RefreshHistory();
            _logger.LogInformation("Restored all {Count} operations", allOps.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore all operations");
        }
        finally
        {
            IsWorking = false;
        }
    }
}
