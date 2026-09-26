# MediatR

Guidance on MediatR, the in-process mediator/CQRS-style messaging library for .NET — the routing
table (by task, not MediatR version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per MediatR version

| File | Covers |
| --- | --- |
| `core-concepts.md` | IRequest/IRequestHandler, INotification/INotificationHandler, IMediator/ISender/IPublisher |
| `registration.md` | AddMediatR, assembly scanning, service lifetimes |
| `pipeline-behaviors.md` | IPipelineBehavior\<,>, ordering, logging/validation/transaction examples |
| `notifications-and-publishing.md` | Publish, ForeachAwaitPublisher vs TaskWhenAllPublisher, custom INotificationPublisher |
| `request-response-vs-notifications.md` | when to use Send vs Publish |
| `streaming.md` | IStreamRequest/IStreamRequestHandler, IAsyncEnumerable |
| `exception-handling.md` | IRequestExceptionHandler, IRequestExceptionAction |
| `cqrs-pattern.md` | commands vs queries as convention, not enforcement |
| `pitfalls-and-tradeoffs.md` | overuse, traceability cost, indirection tradeoffs |
| `testing.md` | unit testing handlers, behaviors, and full-pipeline integration tests |

## Scope

MediatR only — in-process request/response and publish/subscribe messaging within a single
process. Out of scope: other mediator libraries, distributed/out-of-process messaging (message
brokers, service buses), and ASP.NET Core concerns beyond DI registration.

Each reference file notes a version-introduced fact inline (e.g. the v12.0
`INotificationPublisher` extension point); version is not the file-splitting axis for this
skill (see [SKILL.md](SKILL.md) for why).

MediatR carries a non-standard license as of v13.0 — research current terms independently
before adopting it for a project.
