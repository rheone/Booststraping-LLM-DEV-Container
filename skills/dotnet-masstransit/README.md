# MassTransit

Guidance on MassTransit, the distributed application framework for .NET — the routing table (by
task, not MassTransit version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per MassTransit version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `IBus`, `IPublishEndpoint`, `ISendEndpoint`, `ConsumeContext<T>`, message delivery |
| `consumers.md` | Message contracts, `IConsumer<T>`, consumer registration and lifetime |
| `transport-configuration.md` | `AddMassTransit`, the generic `UsingXxx`/`ConfigureEndpoints` pattern |
| `request-response.md` | `IRequestClient<T>`, request timeouts, responding from a consumer |
| `retry-and-error-handling.md` | `UseMessageRetry`, fault messages, the `_error` queue, `IConsumer<Fault<T>>` |
| `sagas.md` | `MassTransitStateMachine`, saga instances, correlation — conceptual overview |
| `testing.md` | The in-memory test harness, asserting publish/consume/fault behavior |

## Scope

MassTransit only — message-based communication between .NET services via publish/subscribe,
request/response, and saga-driven process state. Out of scope: broker-specific administrative
configuration (a specific transport's own topology, connection, or infrastructure settings) and a
full saga state-machine deep dive (see [SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill. MassTransit carries non-standard licensing considerations that vary by major
version — research current terms independently before adopting it for a commercial project.
