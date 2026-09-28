# MassTransit

MassTransit gives a .NET service a transport-agnostic way to publish and consume messages, make
request/response calls, and run long-lived process state as sagas, without hand-rolling
broker-specific plumbing. This skill covers the `AddMassTransit` registration pattern, writing
consumers, retry and fault handling, and testing message-driven code.

> [!NOTE]
> MassTransit carries non-standard licensing considerations that vary by major version. Research current terms independently before adopting it.

## When to reach for it

- You're wiring up a bus in DI and deciding which transport (`UsingInMemory`, `UsingRabbitMq`, etc.) fits your setup.
- You need a consumer for a message type and aren't sure how MassTransit discovers and dispatches to it.
- A message keeps failing and you need to decide between a retry policy and letting it land on the error queue.
- You're modeling a long-running process (an order workflow, a multi-step approval) as a saga.
- You want to assert that a consumer published, consumed, or faulted on a message in a test.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches MassTransit buses,
consumers, request/response, retries, or sagas. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| `IBus`, `IPublishEndpoint`, `ISendEndpoint`, and how a message reaches a consumer | [references/core-concepts.md](references/core-concepts.md) |
| Defining a message contract and writing an `IConsumer<T>` | [references/consumers.md](references/consumers.md) |
| Registering `AddMassTransit` and configuring a transport | [references/transport-configuration.md](references/transport-configuration.md) |
| Request/response with `IRequestClient<T>` | [references/request-response.md](references/request-response.md) |
| Configuring retries, handling faults, and the error queue | [references/retry-and-error-handling.md](references/retry-and-error-handling.md) |
| Sagas and state machines for long-running processes | [references/sagas.md](references/sagas.md) |
| Testing consumers, publishers, and saga behavior | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a consumer for an `OrderSubmitted` message and register it with MassTransit."
- "This message keeps ending up on the error queue: help me figure out why the retry isn't catching it."
- "Model a saga for an order that needs payment confirmation before it ships."
