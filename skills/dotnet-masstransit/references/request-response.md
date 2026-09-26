# Request/Response

Request/response gives you a way to send a message and asynchronously await a correlated reply,
built on top of the same publish/consume pipeline as everything else in MassTransit.

## Defining the request and response contracts

```csharp
public record CheckInventory(string Sku, int Quantity);
public record InventoryChecked(string Sku, bool Available);
```

## Responding from a consumer

```csharp
public class CheckInventoryConsumer : IConsumer<CheckInventory>
{
    public async Task Consume(ConsumeContext<CheckInventory> context)
    {
        var available = await LookUpStockAsync(context.Message.Sku, context.Message.Quantity);
        await context.RespondAsync(new InventoryChecked(context.Message.Sku, available));
    }
}
```

`RespondAsync` sends the reply back to the original requester using the correlation information
MassTransit attached to the incoming request — you never need to know or construct the requester's
address yourself.

## Making a request with IRequestClient<T>

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CheckInventoryConsumer>();
    x.AddRequestClient<CheckInventory>();

    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});

public class OrderService(IRequestClient<CheckInventory> client)
{
    public async Task<bool> IsAvailableAsync(string sku, int quantity)
    {
        Response<InventoryChecked> response =
            await client.GetResponse<InventoryChecked>(new CheckInventory(sku, quantity));

        return response.Message.Available;
    }
}
```

`AddRequestClient<TRequest>` registers an `IRequestClient<TRequest>` you inject like any other
dependency. `GetResponse<TResponse>` sends the request and returns a task that completes when the
correlated response arrives, or throws `RequestTimeoutException` if no response arrives within the
configured timeout.

## Timeouts

```csharp
Response<InventoryChecked> response = await client.GetResponse<InventoryChecked>(
    new CheckInventory(sku, quantity),
    timeout: RequestTimeout.After(s: 10));
```

Always set an explicit timeout appropriate to the operation rather than relying on the default —
the default exists to prevent an indefinite hang, not to model how long a specific request should
reasonably take.

## Multiple possible response types

A consumer can respond with one of several message types depending on outcome (a success and a
failure/rejection contract). Declare every possible response type on the request client call site
using the multi-type overload of `GetResponse`, and branch on which type actually came back, rather
than encoding success/failure as a boolean field on a single response contract when the two
outcomes carry meaningfully different data.
