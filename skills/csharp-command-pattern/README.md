# C# Command Pattern

Reference for the Command design pattern in C#: encapsulating a request as an object with an
`Execute` method, so it can be queued, logged, parameterized, or undone independently of whoever
invokes it. The routing table (by situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern, not per package or version — Command is a behavioral
pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `philosophy-and-structure.md` | command, invoker, receiver roles; the basic `ICommand` shape; when to reach for the pattern |
| `parameterized-commands.md` | commands that carry their own request data via constructor parameters |
| `undo-redo-and-history.md` | reversible commands, a command history stack, redo after undo |
| `generic-command-with-result.md` | `ICommand<TResult>` for commands that produce a value |
| `delegate-based-commands.md` | `Action`/`Func`-based lightweight commands versus the full object form |
| `extending-with-new-commands.md` | adding a new command type without touching existing invokers or commands |
| `testing-commands.md` | testing an invoker against a fake command; testing a command's own execute/undo logic |

## Scope

A behavioral design pattern, not a package — there is no version or license to track. Guidance
applies to any C# codebase; the object-based form has been expressible since C# 1.0, delegate-based
commands became lightweight with anonymous methods (C# 2.0) and lambda expressions (C# 3.0), and
`ICommand<TResult>` uses generics (C# 2.0 onward).

Out of scope: any specific mediator or messaging library's dispatch mechanism, and CQRS as a
broader architectural style. See [SKILL.md](SKILL.md) for the full out-of-scope list.
