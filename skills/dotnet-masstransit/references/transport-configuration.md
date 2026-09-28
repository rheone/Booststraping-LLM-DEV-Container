# Transport Configuration

MassTransit separates *what* your application publishes/consumes from *which* message transport
carries those messages. Every transport (a broker-backed one for production, or the in-memory one
for local development and testing) plugs into the same `AddMassTransit` registration shape.

## The generic configuration pattern

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmittedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

Every transport package exposes its own `UsingXxx` method (in-memory's is `UsingInMemory`) with the
same two-parameter callback shape: `context` (the `IBusRegistrationContext`, used to look up
registered consumers) and `cfg` (the transport-specific bus factory configurator). Swapping
transports means swapping which `UsingXxx` method you call and supplying that transport's own
connection details — the rest of your registration (`AddConsumer`, `ConfigureEndpoints`, retry and
saga configuration) stays identical regardless of which transport is underneath.

## ConfigureEndpoints does the wiring for you

`cfg.ConfigureEndpoints(context)` inspects every consumer, saga, and activity registered through
`x.AddConsumer<T>()` / `x.AddSagaStateMachine<...>()` and creates one receive endpoint per
registered type, using a default naming convention derived from the type name. This is the normal
path — you opt out of it only when you need to name an endpoint explicitly or apply
endpoint-specific settings that don't belong globally.

## Configuring an individual endpoint

```csharp
x.UsingInMemory((context, cfg) =>
{
    cfg.ReceiveEndpoint("order-submitted-queue", e =>
    {
        e.ConfigureConsumer<OrderSubmittedConsumer>(context);
        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    });
});
```

Reach for an explicit `ReceiveEndpoint` block when a specific consumer needs its own retry policy,
concurrency limit, or a queue name that doesn't match the default convention — leave every other
consumer under the automatic `ConfigureEndpoints` call rather than converting the whole
configuration to explicit endpoints just because one consumer needs a tweak.

## Bus-level vs. endpoint-level configuration

Settings applied directly on `cfg` (outside any `ReceiveEndpoint` block) apply bus-wide — a default
retry policy, a default message topology convention, or bus-wide middleware. Settings applied
inside a `ReceiveEndpoint` block apply only to that endpoint and override the bus-wide default for
consumers registered there. Put a setting at the narrowest scope that needs it; a retry policy every
consumer should share belongs at the bus level, one only a single consumer needs belongs on its
endpoint.

## In-memory transport for local development

The in-memory transport (`UsingInMemory`) requires no external broker and is useful for local
development and for the test harness (see [testing.md](testing.md)). It does not persist messages
across a process restart and has no cross-process delivery — do not use it as a production
transport.
