# Philosophy and Structure

The Command pattern has three roles:

- **Command** — an object exposing an `Execute` (or `Execute`-like) method, carrying whatever data
  it needs to perform the request.
- **Receiver** — the object that actually knows how to perform the operation. A command's `Execute`
  method typically delegates to the receiver rather than containing the operation's real logic
  itself.
- **Invoker** — the code that holds a command and calls `Execute` on it, without knowing what
  concrete command type it holds or what the command actually does.

```text
Invoker ---> ICommand (interface) <--- ConcreteCommand ---> Receiver
```

## The basic interface

```csharp
public interface ICommand
{
    void Execute();
}
```

A command implementation typically stores a reference to its receiver and any data the operation
needs, supplied through its constructor:

```csharp
public sealed class SendInvoiceCommand : ICommand
{
    private readonly InvoiceService _receiver;
    private readonly int _invoiceId;

    public SendInvoiceCommand(InvoiceService receiver, int invoiceId)
    {
        _receiver = receiver;
        _invoiceId = invoiceId;
    }

    public void Execute() => _receiver.Send(_invoiceId);
}
```

## Why encapsulate a request as an object at all

A direct method call — `invoiceService.Send(invoiceId)` — is simpler than constructing a command
object for the same operation. The command object earns its cost specifically when the invoker
needs to do something with the request *other than* immediately perform it:

- **Defer it.** Hold the command and execute it later, or on a different thread, without the
  invoker needing to know what "later" means for that particular operation.
- **Queue or log it.** Store a sequence of commands, replay them, or persist a record of what was
  requested — the command object is a natural unit to serialize or enqueue.
- **Undo it.** A command that also knows how to reverse itself lets the invoker maintain a
  history stack with no per-operation-type knowledge; see
  [undo-redo-and-history.md](undo-redo-and-history.md).
- **Treat unrelated operations uniformly.** An invoker that runs a sequence of different operations
  — a macro, a batch job, a UI action bound to a keystroke — handles every one of them through the
  same `ICommand` interface, regardless of what each one actually does.

If none of these apply — the operation always runs immediately, is never queued, logged, undone, or
treated uniformly alongside unrelated operations — a direct method call says the same thing with
less code. Reach for the pattern only when the *decoupling* buys you something concrete.

## The invoker's job

The invoker's only responsibility is holding a command and calling `Execute` at the right time. It
should never inspect the command's concrete type or reach into its data to make decisions — doing
so defeats the purpose of depending on `ICommand` in the first place:

```csharp
public sealed class CommandQueue
{
    private readonly Queue<ICommand> _pending = new();

    public void Enqueue(ICommand command) => _pending.Enqueue(command);

    public void RunAll()
    {
        while (_pending.Count > 0)
        {
            _pending.Dequeue().Execute();
        }
    }
}
```

`CommandQueue` never changes when a new kind of command is introduced — it only ever calls
`Execute()`.
