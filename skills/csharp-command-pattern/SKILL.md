---
name: csharp-command-pattern
description: Reference for the Command design pattern in C# — encapsulating a request as an object with an Execute method, so the request can be queued, logged, parameterized, or undone independently of whoever invokes it. Covers the ICommand interface, parameterized commands that carry their own data, undo/redo via a command history stack, a generic ICommand<TResult> for commands that produce a result, the delegate-based lightweight alternative (Action/Func-based commands) versus the full object-based form, extending the command set without breaking existing invokers, and testing code built around commands. Use when writing an undo/redo stack, a macro/batch-command feature, a queued or auditable operation, an "each button press is a command object" UI action, or deciding between a full ICommand class and a plain delegate for a given operation.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Command Pattern

The Command pattern turns a request — "do this operation, with this data" — into an object in its
own right, instead of a direct method call. Once a request is an object, you can hold onto it,
queue it, log it, undo it, or hand it to code that has no idea what the operation actually does.

## Quick start

```csharp
public interface ICommand
{
    void Execute();
}

public sealed class MoveShapeCommand : ICommand
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
}

// The invoker holds a command without knowing what it does.
ICommand command = new MoveShapeCommand(shape, dx: 10, dy: 0);
command.Execute();
```

`MoveShapeCommand` carries its own data (`_shape`, `_dx`, `_dy`) — the invoker calling `Execute()`
supplies nothing beyond the command itself.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning the pattern's roles (command, invoker, receiver) and the basic `ICommand` shape | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| Writing a command that carries its own request data | [references/parameterized-commands.md](references/parameterized-commands.md) |
| Building undo/redo with a command history stack | [references/undo-redo-and-history.md](references/undo-redo-and-history.md) |
| Writing a command that produces a result — `ICommand<TResult>` | [references/generic-command-with-result.md](references/generic-command-with-result.md) |
| Deciding between a full command object and a plain `Action`/`Func` delegate | [references/delegate-based-commands.md](references/delegate-based-commands.md) |
| Adding a new command type without touching existing invokers or commands | [references/extending-with-new-commands.md](references/extending-with-new-commands.md) |
| Testing code that executes commands, or testing a command's own logic | [references/testing-commands.md](references/testing-commands.md) |

## Out of scope

- Any specific mediator or messaging library's dispatch mechanism. The command object itself, and
  the invoker that calls `Execute` on it, are described generically — apply them directly or behind
  whatever dispatch mechanism a given project already uses.
- CQRS as an architectural style. This skill covers the Command *object* — encapsulating one
  request as one object with an execute operation — not a broader read/write architectural split.
