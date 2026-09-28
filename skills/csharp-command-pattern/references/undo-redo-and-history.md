# Undo/Redo and Command History

Undo/redo support extends the basic command interface with an `Undo` operation, and adds a history
stack the invoker uses to track which commands have run and in what order.

## The reversible command interface

```csharp
public interface IReversibleCommand : ICommand
{
    void Undo();
}
```

Each command implements `Undo` by reversing exactly what its own `Execute` did — it needs enough
state captured at execution time (or construction time) to reverse the operation, not just to
perform it.

```csharp
public sealed class MoveShapeCommand : IReversibleCommand
{
    private readonly Shape _shape;
    private readonly int _dx, _dy;

    public MoveShapeCommand(Shape shape, int dx, int dy)
    {
        _shape = shape;
        _dx = dx;
        _dy = dy;
    }

    public void Execute() => _shape.MoveBy(_dx, _dy);
    public void Undo() => _shape.MoveBy(-_dx, -_dy);
}
```

Some operations can't compute their reversal from their own input alone — a command that sets a
value needs to remember the *previous* value to undo correctly, captured the moment `Execute` runs:

```csharp
public sealed class SetTitleCommand : IReversibleCommand
{
    private readonly Document _document;
    private readonly string _newTitle;
    private string? _previousTitle;

    public SetTitleCommand(Document document, string newTitle)
    {
        _document = document;
        _newTitle = newTitle;
    }

    public void Execute()
    {
        _previousTitle = _document.Title;
        _document.Title = _newTitle;
    }

    public void Undo()
    {
        if (_previousTitle is null)
        {
            throw new InvalidOperationException("Cannot undo a command that has not executed.");
        }

        _document.Title = _previousTitle;
    }
}
```

## The history stack

The invoker maintains two stacks: one for commands available to undo, one for commands available to
redo. Executing a new command clears the redo stack — once you branch off with a new action, the
previously-undone commands are no longer a valid "next" state to redo into.

```csharp
public sealed class CommandHistory
{
    private readonly Stack<IReversibleCommand> _undoStack = new();
    private readonly Stack<IReversibleCommand> _redoStack = new();

    public void Execute(IReversibleCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
    }
}
```

Every caller runs commands through `CommandHistory.Execute` rather than calling a command's
`Execute()` directly — bypassing the history means that command is never available to undo.

## Composite (macro) undo

A batch of commands that should undo as one unit — a "macro" — implements `IReversibleCommand`
itself, wrapping an ordered list of inner commands, executing them in order and undoing them in
reverse order:

```csharp
public sealed class MacroCommand : IReversibleCommand
{
    private readonly IReadOnlyList<IReversibleCommand> _commands;

    public MacroCommand(IReadOnlyList<IReversibleCommand> commands) => _commands = commands;

    public void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }

    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
```

Reversing in the opposite order matters whenever a later command's effect depends on an earlier
one having already run — undoing in execution order instead of reverse can leave the receiver in a
state none of the individual commands ever produced.

## Bounding history size

An unbounded undo stack keeps every command — and everything each one captured to make `Undo`
possible — alive for the life of the history object. For a long-running session, cap the stack at a
fixed depth and discard the oldest entry once the cap is reached, so memory use doesn't grow
without bound over time.
