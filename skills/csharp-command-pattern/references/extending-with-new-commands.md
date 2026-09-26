# Extending With New Commands

Adding a new kind of operation to a codebase built around the Command pattern should mean writing
one new command class — never modifying the invoker, the command interface, or any existing
command.

## Adding a new command

1. Confirm the command interface (`ICommand`, `ICommand<TResult>`, or a reversible variant) already
   expresses what the new operation needs: an `Execute` with no inputs beyond the command's own
   state, and — if applicable — a result type or an `Undo` member.
2. Write the new command class implementing that interface, carrying whatever data the operation
   needs through its constructor.
3. Wherever the application constructs commands and hands them to an invoker (a UI action binding,
   a queue producer, a script), add the construction of the new command type. The invoker itself —
   the queue, the history stack, the runner — needs no change, since it only ever calls members on
   the interface.

```csharp
// Existing.
public sealed class MoveShapeCommand : IReversibleCommand { /* ... */ }

// New operation, new command class, invoker and interface unchanged.
public sealed class ResizeShapeCommand : IReversibleCommand
{
    private readonly Shape _shape;
    private readonly int _widthDelta, _heightDelta;

    public ResizeShapeCommand(Shape shape, int widthDelta, int heightDelta)
    {
        _shape = shape;
        _widthDelta = widthDelta;
        _heightDelta = heightDelta;
    }

    public void Execute() => _shape.ResizeBy(_widthDelta, _heightDelta);
    public void Undo() => _shape.ResizeBy(-_widthDelta, -_heightDelta);
}
```

Both `MoveShapeCommand` and `ResizeShapeCommand` push onto the same `CommandHistory` and undo
through the same stack, without the history object knowing either type exists.

## When the interface itself needs a new member

If every command in the codebase needs a new capability (say, a `Description` property for a log
view), adding it to the shared interface is a breaking change to every existing command — each one
now has to implement the new member. Two ways to add the capability without forcing every existing
command to change:

- Give the new member a default implementation on the interface (a C# 8.0+ default interface
  member) when a sensible default exists — existing commands compile unchanged, and only new or
  updated commands need to override it.
- Introduce a second, narrower interface for the new capability (`IDescribableCommand : ICommand`)
  that only the commands that support it implement, and have consuming code check for that
  interface where it needs the extra capability, falling back to plain `ICommand` behavior when a
  given command doesn't implement it.

Reserve a breaking addition to the base interface for cases where genuinely every command — present
and future — must support the new member with no reasonable default.

## Testing the extension in isolation

A new command's tests exercise only its own `Execute` (and `Undo`, if reversible) against a fake or
stub receiver — see [testing-commands.md](testing-commands.md). Existing commands, the invoker, and
the history stack need no new tests as a result of adding one, because none of their behavior
changed.
