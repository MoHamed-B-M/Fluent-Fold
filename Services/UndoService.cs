using Microsoft.Extensions.Logging;
using FluentFold.Models;

namespace FluentFold.Services;

public sealed class UndoService(ILogger<UndoService> logger) : IUndoService
{
    private readonly Stack<OrganizeOperation> _stack = new();

    public bool CanUndo => _stack.Count > 0;

    public void Push(OrganizeOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _stack.Push(operation);
        logger.LogInformation("Undo operation pushed: {Count} moves from '{Folder}'", operation.Moves.Count, operation.SourceFolder);
    }

    public OrganizeOperation? Pop()
    {
        if (_stack.TryPop(out var op))
        {
            logger.LogInformation("Undo operation popped: {Count} moves", op.Moves.Count);
            return op;
        }
        return null;
    }

    public IReadOnlyList<OrganizeOperation> History => _stack.ToList().AsReadOnly();

    public void Clear()
    {
        _stack.Clear();
        logger.LogInformation("Undo history cleared");
    }

    public void Remove(OrganizeOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var list = _stack.ToList();
        list.Remove(operation);
        _stack.Clear();
        for (int i = list.Count - 1; i >= 0; i--)
            _stack.Push(list[i]);

        logger.LogInformation("Undo operation removed: {Count} moves", operation.Moves.Count);
    }
}
