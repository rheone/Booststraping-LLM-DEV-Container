# Testing Mediator-Based Code

## How to test it

The pattern's whole value is that a caller and a handler never reference each other directly — that
separation makes each side trivially testable in isolation, and testing across the mediator itself
mostly unnecessary.

- **Test a handler directly, not through the mediator.** A handler is a plain class with a `Handle`
  method — construct it with its dependencies (real or faked) and call `Handle` directly. Routing
  the call through a real `Mediator` and a DI container adds setup cost and couples the test to
  registration wiring, without testing anything the handler's own logic doesn't already cover.
- **Test a caller with a fake `IMediator`**, asserting on which request object was sent (and with
  what values), not on the response the fake happens to hand back — the caller's job is to
  construct the right request, and that's what the test should verify.
- **Test the dispatcher itself (if hand-rolled) once, with a couple of fake request/handler
  pairs.** This verifies resolution and invocation mechanics work at all; it isn't the place to
  re-test any specific handler's business logic.
- **A missing-handler failure deserves its own test** if the dispatcher is expected to fail loudly
  (see [handler-resolution-strategies.md](handler-resolution-strategies.md)) — assert the specific
  exception is thrown for an unregistered request type.

## Fake mediator for testing callers

```csharp
public sealed class FakeMediator : IMediator
{
    public List<object> SentRequests { get; } = new();
    private readonly Dictionary<Type, object> _responses = new();

    public void SetResponse<TResponse>(IRequest<TResponse> requestType, TResponse response) =>
        _responses[requestType.GetType()] = response!;

    public TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        SentRequests.Add(request);
        return _responses.TryGetValue(request.GetType(), out object? response)
            ? (TResponse)response
            : default!;
    }
}
```

## Most likely scenarios

**1. Testing a handler in isolation**

```csharp
[Fact]
public void GetOrderQueryHandler_ReturnsOrderDto()
{
    var repository = new FakeOrderRepository(seed: new Order(id: 1, total: 42m));
    var handler = new GetOrderQueryHandler(repository);

    OrderDto result = handler.Handle(new GetOrderQuery(1));

    Assert.Equal(42m, result.Total);
}
```

**2. Testing a caller sends the right request**

```csharp
[Fact]
public void Controller_Get_SendsGetOrderQueryWithId()
{
    var mediator = new FakeMediator();
    mediator.SetResponse(new GetOrderQuery(1), new OrderDto(1, 42m));
    var controller = new OrdersController(mediator);

    controller.Get(1);

    var sent = Assert.IsType<GetOrderQuery>(Assert.Single(mediator.SentRequests));
    Assert.Equal(1, sent.OrderId);
}
```

**3. Testing the dispatcher resolves and invokes the correct handler**

```csharp
[Fact]
public void Mediator_Send_InvokesRegisteredHandler()
{
    var services = new ServiceCollection();
    services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();
    services.AddScoped<IMediator, Mediator>();
    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    string result = mediator.Send(new Ping());

    Assert.Equal("pong", result);
}

[Fact]
public void Mediator_Send_WithNoRegisteredHandler_Throws()
{
    var provider = new ServiceCollection().AddScoped<IMediator, Mediator>().BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    Assert.Throws<InvalidOperationException>(() => mediator.Send(new Ping()));
}
```
