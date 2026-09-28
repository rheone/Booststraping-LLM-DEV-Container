---
name: dotnet-masstransit
description: Guidance on MassTransit (verified current Apache-2.0 release 8.4.1) for building distributed .NET applications with message-based communication — IBus/IPublishEndpoint/ISendEndpoint, defining and consuming messages with IConsumer<T>, the generic transport configuration pattern (AddMassTransit + UsingXxx + ConfigureEndpoints), request/response messaging with IRequestClient<T>, retry policies and fault/error-queue handling, and sagas as long-running process state machines. Use when publishing or consuming messages with MassTransit, wiring up a bus in DI, writing a consumer, configuring retries or fault handling, implementing request/response, or testing MassTransit-based code.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# MassTransit

Guidance on MassTransit, the distributed application framework for .NET that layers publish/
subscribe, request/response, and saga-based process management on top of a message transport.
Organized by task, not by version — the current `AddMassTransit` DI-registration API is the API
this skill documents throughout.

MassTransit carries non-standard licensing considerations that vary by major version — research
current terms independently before adopting it for a commercial project.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Understanding `IBus`, `IPublishEndpoint`, `ISendEndpoint`, and how a message reaches a consumer | [references/core-concepts.md](references/core-concepts.md) |
| Defining a message contract and writing an `IConsumer<T>` | [references/consumers.md](references/consumers.md) |
| Registering `AddMassTransit` and configuring a transport | [references/transport-configuration.md](references/transport-configuration.md) |
| Implementing request/response with `IRequestClient<T>` | [references/request-response.md](references/request-response.md) |
| Configuring retries, handling faults, and reasoning about the error queue | [references/retry-and-error-handling.md](references/retry-and-error-handling.md) |
| Understanding sagas and state machines for long-running processes | [references/sagas.md](references/sagas.md) |
| Testing consumers, publishers, and saga behavior | [references/testing.md](references/testing.md) |

## Quick start

A minimal message contract, consumer, and bus registration (current API, v8.4.1):

```csharp
public record OrderSubmitted(Guid OrderId, decimal Total);

public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;
        await Task.CompletedTask; // handle it
    }
}

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmittedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

`ConfigureEndpoints(context)` scans every registered consumer and creates a receive endpoint for
each one automatically — you rarely need to hand-configure an endpoint per consumer unless you're
overriding its name or its retry/concurrency behavior. See
[transport-configuration.md](references/transport-configuration.md).

## Out of scope

- Broker-specific configuration (RabbitMQ exchange/queue topology tuning, Azure Service Bus
  namespace setup, Amazon SQS visibility timeouts) — this skill documents the transport-agnostic
  `AddMassTransit`/`UsingXxx`/`ConfigureEndpoints` pattern that every transport shares, not any one
  broker's own administrative surface.
- Full saga state-machine authoring (every `State`, `Event`, `Initially`/`During`/`Finally` block,
  saga repository persistence tuning) — [sagas.md](references/sagas.md) covers the concept and a
  minimal shape only; a production state machine with many states and compensating actions is a
  deep domain of its own.
- Message serialization format tuning and schema evolution/versioning strategy beyond the default
  contract-first approach shown in [consumers.md](references/consumers.md).
