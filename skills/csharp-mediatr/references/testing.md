# Testing

MediatR's design — plain classes implementing plain interfaces, resolved via DI — means most
testing does not require MediatR itself to be involved at all. Reach for a real `IMediator`/
`ISender` only for the outermost layer of tests that specifically want to verify the pipeline
wiring.

## Unit testing a handler directly (no MediatR needed)

A handler is just a class implementing `IRequestHandler<TRequest, TResponse>` — instantiate it
directly and call `Handle`. There is no need to go through `Send`, register anything with DI, or
touch MediatR's types at all:

```csharp
[Fact]
public async Task Handle_ValidOrder_ReturnsNewOrderId()
{
    var repository = Substitute.For<IOrderRepository>();
    var handler = new CreateOrderHandler(repository);

    var result = await handler.Handle(new CreateOrder("customer-1", 42.00m), CancellationToken.None);

    result.Should().NotBe(Guid.Empty);
    await repository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
}
```

This is the fastest and most common layer of MediatR-adjacent testing: it exercises the actual
business logic, has no dependency on the DI container or assembly scanning being configured
correctly, and fails for reasons directly traceable to the handler's own code.

## Testing a pipeline behavior in isolation

A behavior is also just a class — construct it directly, and supply a `RequestHandlerDelegate`
stand-in for `next` (a simple lambda) rather than a real downstream handler, so the test isolates
the behavior's own logic:

```csharp
[Fact]
public async Task Handle_WhenValidationFails_ThrowsWithoutCallingNext()
{
    var validator = Substitute.For<IValidator<CreateOrder>>();
    validator.ValidateAsync(Arg.Any<CreateOrder>(), Arg.Any<CancellationToken>())
        .Returns(new ValidationResult([new ValidationFailure("Total", "must be positive")]));

    var behavior = new ValidationBehavior<CreateOrder, Guid>([validator]);
    var nextCalled = false;
    RequestHandlerDelegate<Guid> next = _ =>
    {
        nextCalled = true;
        return Task.FromResult(Guid.NewGuid());
    };

    var act = () => behavior.Handle(new CreateOrder("customer-1", -5m), next, CancellationToken.None);

    await act.Should().ThrowAsync<ValidationException>();
    nextCalled.Should().BeFalse();
}
```

Asserting that `next` was (or wasn't) invoked is the key thing a behavior test needs to verify
beyond ordinary input/output assertions — it's the behavior's equivalent of "did it short-circuit
correctly," and it's easy to get wrong in the implementation (e.g. accidentally calling `next`
even on the failure path) without a test specifically checking for it.

## Integration-testing the full pipeline via IMediator

Reserve this for tests that want to verify the pipeline **wiring** itself — that behaviors are
registered in the right order, that validation actually runs before a transaction opens, that a
request with no handler fails the way you expect, or that a full vertical slice (controller-level
concern down through handler) behaves correctly together. Build a minimal `ServiceCollection` with
real `AddMediatR` registration (and real or fake dependencies as appropriate) rather than mocking
`IMediator` itself:

```csharp
[Fact]
public async Task CreateOrder_WithInvalidTotal_NeverPersists()
{
    var services = new ServiceCollection();
    services.AddSingleton(Substitute.For<IOrderRepository>());
    services.AddValidatorsFromAssembly(typeof(CreateOrder).Assembly);
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(CreateOrder).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        cfg.LicenseKey = TestLicenseKey; // or set MEDIATR_LICENSE_KEY in the test environment
    });

    using var provider = services.BuildServiceProvider();
    var sender = provider.GetRequiredService<ISender>();

    var act = () => sender.Send(new CreateOrder("customer-1", -5m));

    await act.Should().ThrowAsync<ValidationException>();
    await provider.GetRequiredService<IOrderRepository>()
        .DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
}
```

Avoid mocking `IMediator`/`ISender` itself in tests that exercise a controller/endpoint calling
`Send` — a mocked sender that just returns a canned value tells you nothing about whether the real
pipeline (validation, transactions, the actual handler logic) works, and it duplicates, in mock
setup, exactly the behavior the real registration would give you for free. Reserve `ISender`
mocking for tests of code that merely calls `Send` and needs the *call* to be verifiable (e.g. "the
endpoint sent the right command"), not for tests that are trying to verify business behavior.

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| A handler's business logic | Instantiate the handler directly; no MediatR involved |
| A single behavior's short-circuit/wrapping logic | Instantiate the behavior directly with a stand-in `next` delegate |
| Behavior registration order, or full request-to-response wiring | Build a real `ServiceProvider` with `AddMediatR` and resolve `ISender` |
| A controller/endpoint's interaction with the mediator | Mock `ISender`/`IPublisher` and assert the call was made with the right request — not the outcome |

Most of the test suite should sit in the first row — handler unit tests are cheap, fast, and
directly traceable to a failure. The full-pipeline integration layer should be a much smaller set
of tests that specifically exist to catch registration/wiring mistakes the unit tests can't see.
