# Testing Commands

The Command pattern separates into two testable units: the invoker, which only ever calls members
on the command interface, and each command, whose `Execute` (and `Undo`) logic drives a receiver.
Test each in isolation.

## Testing an invoker with a fake command

Because the invoker depends only on the command interface, test it with a minimal fake command that
records whether — and how many times — it was called, rather than a real command with a real
receiver.

```csharp
public sealed class FakeCommand : ICommand
{
    public int ExecuteCallCount { get; private set; }

    public void Execute() => ExecuteCallCount++;
}

[Fact]
public void CommandQueue_executes_every_enqueued_command_once()
{
    var queue = new CommandQueue();
    var first = new FakeCommand();
    var second = new FakeCommand();
    queue.Enqueue(first);
    queue.Enqueue(second);

    queue.RunAll();

    Assert.Equal(1, first.ExecuteCallCount);
    Assert.Equal(1, second.ExecuteCallCount);
}
```

For a history stack, a fake reversible command that also tracks `Undo` calls verifies ordering
without needing any real receiver behavior:

```csharp
public sealed class FakeReversibleCommand : IReversibleCommand
{
    public List<string> Log { get; } = new();

    public void Execute() => Log.Add("execute");
    public void Undo() => Log.Add("undo");
}

[Fact]
public void CommandHistory_undo_reverses_the_most_recent_command()
{
    var history = new CommandHistory();
    var command = new FakeReversibleCommand();

    history.Execute(command);
    history.Undo();

    Assert.Equal(new[] { "execute", "undo" }, command.Log);
}
```

## Testing a command's own execute/undo logic

A command's own test drives it against a fake or stub receiver and asserts on the receiver's
resulting state, or on a captured record of what the command called on it — the same technique used
for any class that delegates to a collaborator.

```csharp
public sealed class FakeInventoryService : InventoryService
{
    public List<(string Sku, int Delta)> Adjustments { get; } = new();

    public override void AdjustStock(string sku, int delta) => Adjustments.Add((sku, delta));
}

[Fact]
public void AdjustInventoryCommand_applies_the_delta_to_the_given_sku()
{
    var receiver = new FakeInventoryService();
    var command = new AdjustInventoryCommand(receiver, sku: "WIDGET-1", delta: -3);

    command.Execute();

    Assert.Equal(new[] { ("WIDGET-1", -3) }, receiver.Adjustments);
}
```

## Testing undo correctness

Give a reversible command's `Undo` its own test that asserts the receiver returns to its prior
observable state after `Execute` then `Undo`, not just that `Undo` was called — a command whose
`Undo` runs the wrong reversal is a bug `Execute`'s own test can't catch.

```csharp
[Fact]
public void SetTitleCommand_undo_restores_the_previous_title()
{
    var document = new Document { Title = "Original" };
    var command = new SetTitleCommand(document, newTitle: "Updated");

    command.Execute();
    command.Undo();

    Assert.Equal("Original", document.Title);
}

[Fact]
public void SetTitleCommand_undo_before_execute_throws()
{
    var command = new SetTitleCommand(new Document(), newTitle: "Updated");

    Assert.Throws<InvalidOperationException>(() => command.Undo());
}
```

## Testing a result-producing command

For `ICommand<TResult>`, assert on the returned value directly, in addition to any receiver-state
assertions:

```csharp
[Fact]
public void CreateOrderCommand_returns_the_new_order_id()
{
    var receiver = new FakeOrderService { NextOrderId = 42 };
    var command = new CreateOrderCommand(receiver, new OrderRequest(/* ... */));

    int orderId = command.Execute();

    Assert.Equal(42, orderId);
}
```

## What doesn't need a test here

Don't write a test for a delegate-based command's closure body beyond what you'd already write for
the code it calls — a lambda passed straight to `Enqueue` has no logic of its own distinct from the
method or expression it wraps. Reserve dedicated command tests for command *classes*, where
construction, state capture, and (for reversible commands) undo correctness are real logic worth
covering on their own.
