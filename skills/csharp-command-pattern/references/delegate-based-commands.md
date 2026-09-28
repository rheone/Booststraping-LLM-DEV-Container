# Delegate-Based Commands

For a request with no need for undo, no data beyond what a closure can capture, and no identity
beyond "run this," a plain `Action` or `Func<TResult>` delegate is a lighter-weight stand-in for a
full command object. The delegate itself plays the role of `Execute`; there's no class, no
constructor, and no interface implementation.

## The lightweight form

```csharp
public sealed class SimpleCommandQueue
{
    private readonly Queue<Action> _pending = new();

    public void Enqueue(Action command) => _pending.Enqueue(command);

    public void RunAll()
    {
        while (_pending.Count > 0)
        {
            _pending.Dequeue().Invoke();
        }
    }
}

var queue = new SimpleCommandQueue();
queue.Enqueue(() => shape.MoveBy(10, 0));
queue.Enqueue(() => Console.WriteLine("moved"));
queue.RunAll();
```

The lambda closes over whatever data it needs (`shape`, `10`, `0`) instead of that data living in
named fields on a command class. This reads naturally for a short, ad hoc sequence of actions where
naming a class per action would add ceremony without adding clarity.

## A delegate-based result command

The same substitution applies to `ICommand<TResult>` — a `Func<TResult>` plays the same role:

```csharp
public sealed class SimpleCommandRunner
{
    public TResult Run<TResult>(Func<TResult> command) => command.Invoke();
}

int orderId = new SimpleCommandRunner().Run(() => orderService.CreateOrder(request));
```

## Delegate-based undo

Undo support survives the delegate form by pairing two delegates — one to do, one to undo — instead
of one method per concern on an interface:

```csharp
public sealed record DelegateCommand(Action Do, Action Undo);

public sealed class DelegateCommandHistory
{
    private readonly Stack<DelegateCommand> _undoStack = new();

    public void Execute(DelegateCommand command)
    {
        command.Do();
        _undoStack.Push(command);
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        _undoStack.Pop().Undo();
    }
}

var history = new DelegateCommandHistory();
int previousDx = shape.X;
history.Execute(new DelegateCommand(
    Do: () => shape.MoveBy(10, 0),
    Undo: () => shape.MoveTo(previousDx, shape.Y)));
```

This works, but notice the closure has to capture whatever state `Undo` needs (`previousDx` here) at
the point the `DelegateCommand` is constructed — there's no constructor step of its own to do that
capture at a well-defined moment the way a command class's `Execute()` method provides.

## When to choose which form

| Signal | Choose |
| --- | --- |
| The operation has meaningful identity — it gets logged, serialized, inspected, or compared by type elsewhere in the codebase | A full command class implementing `ICommand`/`ICommand<TResult>` |
| The operation needs to capture execution-time state to support undo, beyond what a closure can hold cleanly | A full command class — see [undo-redo-and-history.md](undo-redo-and-history.md) |
| The operation is a short, one-off action, run once, with no identity needed beyond "the thing that runs" | An `Action`/`Func<TResult>` delegate |
| Many similar operations differ only by a couple of parameters, and a class per variant would be pure boilerplate | A delegate, or a single parameterized command class reused with different constructor arguments — see [parameterized-commands.md](parameterized-commands.md) |

The two forms aren't mutually exclusive within one codebase: use whichever a given operation's needs
call for, without normalizing everything to one style for consistency's own sake.
