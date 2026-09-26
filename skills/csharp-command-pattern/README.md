# C# Command Pattern

The Command pattern turns a request ("do this operation, with this data") into an object in its
own right, instead of a direct method call. Once a request is an object, you can hold onto it,
queue it, log it, undo it, or hand it to code that has no idea what the operation actually does.
This skill covers parameterized commands, undo/redo history, a generic result-producing command,
and the lightweight delegate-based alternative.

## When to reach for it

- You're building an undo/redo stack and need each user action captured as a reversible object.
- You're implementing a macro or batch-command feature that replays a sequence of operations.
- You need a queued or auditable operation: something that gets created now and executed, logged,
  or replayed later.
- You're deciding between a full `ICommand` class and a plain delegate for a given operation.

## Using it

This skill is model-invoked: it activates automatically when the conversation touches undo/redo,
queued or auditable operations, or a UI action modeled as "each button press is a command object".
You can also invoke it directly by asking for it or typing `/csharp-command-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| Command, invoker, and receiver roles; the basic `ICommand` shape | [references/philosophy-and-structure.md](references/philosophy-and-structure.md) |
| Commands that carry their own request data via constructor parameters | [references/parameterized-commands.md](references/parameterized-commands.md) |
| Reversible commands, a command history stack, and redo after undo | [references/undo-redo-and-history.md](references/undo-redo-and-history.md) |
| A generic `ICommand<TResult>` for commands that produce a value | [references/generic-command-with-result.md](references/generic-command-with-result.md) |
| `Action`/`Func`-based lightweight commands versus the full object form | [references/delegate-based-commands.md](references/delegate-based-commands.md) |
| Adding a new command type without touching existing invokers | [references/extending-with-new-commands.md](references/extending-with-new-commands.md) |
| Testing an invoker against a fake command, and testing a command's own logic | [references/testing-commands.md](references/testing-commands.md) |

## Example prompts

- "I need an undo/redo stack for these editing operations. Help me model each one as a command."
- "Should this button action be a full `ICommand` class or just a delegate?"
- "Help me add a macro feature that replays a batch of commands in sequence."
