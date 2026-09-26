# Parameterized Commands

A command carries the data its operation needs as instance state, set at construction time, rather
than as arguments to `Execute()`. `Execute()` itself always takes the same shape — no parameters, or
a fixed context type — regardless of what any particular command instance does, because the
invoker calling `Execute()` doesn't know or care what data a given command is carrying.

## Shape

```csharp
public interface ICommand
{
    void Execute();
}

public sealed class AdjustInventoryCommand : ICommand
{
    private readonly InventoryService _receiver;
    private readonly string _sku;
    private readonly int _delta;

    public AdjustInventoryCommand(InventoryService receiver, string sku, int delta)
    {
        _receiver = receiver;
        _sku = sku;
        _delta = delta;
    }

    public void Execute() => _receiver.AdjustStock(_sku, _delta);
}
```

Two commands of the same type, constructed with different data, are independent objects the
invoker treats identically:

```csharp
ICommand restock = new AdjustInventoryCommand(inventory, sku: "WIDGET-1", delta: +50);
ICommand sale = new AdjustInventoryCommand(inventory, sku: "WIDGET-1", delta: -3);

foreach (var command in new[] { restock, sale })
{
    command.Execute();
}
```

## Keeping parameterization immutable

Store the command's data in `readonly` fields (or init-only properties) set once at construction.
A command whose data can change after construction — a mutable property set by the invoker after
the command was created — breaks the assumption that a command fully describes one specific
request; two different callers holding the same command instance could end up executing different
operations depending on when they read or wrote its state.

```csharp
public sealed class SendEmailCommand : ICommand
{
    public string Recipient { get; }
    public string Subject { get; }

    public SendEmailCommand(string recipient, string subject)
    {
        Recipient = recipient;
        Subject = subject;
    }

    public void Execute() { /* send using Recipient, Subject */ }
}
```

Exposing `Recipient` and `Subject` as `get`-only properties lets a caller inspect a queued
command's data (useful for logging or a "pending operations" display) without exposing a way to
mutate it after the fact.

## Validating parameters early

Validate a command's data in its constructor, not inside `Execute()`, when the data is invalid on
its face (a null recipient, a negative quantity that can never be valid for this operation). This
surfaces a construction-time mistake immediately, at the point the command was built, rather than
later when something dequeues and executes it — often far from the code that built it incorrectly.

```csharp
public AdjustInventoryCommand(InventoryService receiver, string sku, int delta)
{
    if (string.IsNullOrWhiteSpace(sku))
    {
        throw new ArgumentException("SKU is required.", nameof(sku));
    }

    _receiver = receiver;
    _sku = sku;
    _delta = delta;
}
```

Reserve validation inside `Execute()` for checks that genuinely depend on state only known at
execution time (current stock level, current account balance) — those can't be checked any earlier
than the moment the command actually runs.
