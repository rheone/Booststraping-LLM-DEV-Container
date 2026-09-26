# Generic Command With a Result

The basic `ICommand.Execute()` returns `void` — it's built for operations whose entire purpose is a
side effect. Many commands need to hand a value back to whoever runs them: the ID of a newly
created record, a computed total, a success/failure outcome with details. `ICommand<TResult>`
covers that case without giving up the invoker's ability to treat every command uniformly.

## The interface

```csharp
public interface ICommand<out TResult>
{
    TResult Execute();
}
```

`TResult` is covariant — a command that produces a more specific result type can stand in wherever
a command producing a less specific one is expected.

## Implementing it

```csharp
public sealed class CreateOrderCommand : ICommand<int>
{
    private readonly OrderService _receiver;
    private readonly OrderRequest _request;

    public CreateOrderCommand(OrderService receiver, OrderRequest request)
    {
        _receiver = receiver;
        _request = request;
    }

    public int Execute() => _receiver.CreateOrder(_request);
}
```

An invoker that runs result-producing commands returns whatever `Execute()` hands back, rather than
discarding it:

```csharp
public sealed class CommandRunner
{
    public TResult Run<TResult>(ICommand<TResult> command) => command.Execute();
}

int orderId = new CommandRunner().Run(new CreateOrderCommand(orderService, request));
```

## Combining a result with undo

A reversible command that also produces a result implements both `ICommand<TResult>` and an undo
member, rather than trying to fold undo into the non-generic `ICommand` interface — the two concerns
are independent:

```csharp
public interface IReversibleCommand<out TResult> : ICommand<TResult>
{
    void Undo();
}

public sealed class CreateOrderCommand : IReversibleCommand<int>
{
    private readonly OrderService _receiver;
    private readonly OrderRequest _request;
    private int _createdOrderId;

    public CreateOrderCommand(OrderService receiver, OrderRequest request)
    {
        _receiver = receiver;
        _request = request;
    }

    public int Execute()
    {
        _createdOrderId = _receiver.CreateOrder(_request);
        return _createdOrderId;
    }

    public void Undo() => _receiver.DeleteOrder(_createdOrderId);
}
```

## Choosing between `ICommand` and `ICommand<TResult>` for a given operation

Use `ICommand<TResult>` only when the caller actually needs the value back to do something with it
— display it, pass it to the next step, assert on it in a test. If every current and foreseeable
caller only cares that the operation happened, `ICommand` with a `void Execute()` says that more
plainly. Adding a result type "just in case" gives every invoker a value to ignore and no benefit
in return.

A command that both needs to signal success/failure *and* return a value on success does so through
its `TResult` type itself — a small result record or a discriminated outcome type — rather than by
also throwing exceptions for expected failure paths:

```csharp
public sealed record CreateOrderResult(bool Success, int? OrderId, string? Error);

public sealed class CreateOrderCommand : ICommand<CreateOrderResult>
{
    public CreateOrderResult Execute()
    {
        // ... validation and creation logic ...
        return new CreateOrderResult(Success: true, OrderId: 42, Error: null);
    }
}
```
