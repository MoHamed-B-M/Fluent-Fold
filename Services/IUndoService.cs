using FluentFold.Models;

namespace FluentFold.Services;

/// <summary>Manages a stack of organization operations for undo capability.</summary>
public interface IUndoService
{
    /// <summary>Returns true if there are operations available to undo.</summary>
    bool CanUndo { get; }
    /// <summary>Pushes a new operation onto the undo stack.</summary>
    void Push(OrganizeOperation operation);
    /// <summary>Pops the most recent operation from the stack.</summary>
    OrganizeOperation? Pop();
    /// <summary>Returns a read-only snapshot of the full undo history.</summary>
    IReadOnlyList<OrganizeOperation> History { get; }
    /// <summary>Clears all operations from the undo stack.</summary>
    void Clear();
    /// <summary>Removes a specific operation from the undo stack.</summary>
    void Remove(OrganizeOperation operation);
}
