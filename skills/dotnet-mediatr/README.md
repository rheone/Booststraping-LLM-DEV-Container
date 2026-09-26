# MediatR

MediatR wires up in-process request/response and publish/subscribe messaging so a controller,
endpoint, or service doesn't call a handler directly: it sends a message through a mediator
instead. This skill covers requests and handlers, notification fan-out, pipeline behaviors,
streaming, and the testing patterns each of those needs.

> [!NOTE]
> MediatR carries a non-standard license as of v13.0. Research current terms independently before adopting it.

## When to reach for it

- You're deciding whether a use case should be a request/response call or a fire-and-forget notification.
- A pipeline behavior (logging, validation, a transaction wrapper) is running in the wrong order and you need to know why.
- You're setting up `AddMediatR` for the first time and choosing how handlers get discovered and scoped.
- You want a handler or a behavior under test without spinning up the whole DI pipeline.

## Using it

This skill is model-invoked: it fires automatically when your prompt touches MediatR requests,
handlers, behaviors, or notification publishing. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| `IRequest`/`IRequestHandler`, `INotification`/`INotificationHandler`, `IMediator`/`ISender`/`IPublisher` | [references/core-concepts.md](references/core-concepts.md) |
| `AddMediatR`, assembly scanning, handler service lifetimes | [references/registration.md](references/registration.md) |
| Cross-cutting logic that wraps every request, and behavior ordering | [references/pipeline-behaviors.md](references/pipeline-behaviors.md) |
| Publishing a notification to multiple handlers, sequential vs. parallel fan-out | [references/notifications-and-publishing.md](references/notifications-and-publishing.md) |
| Choosing between `Send` and `Publish` for a given use case | [references/request-response-vs-notifications.md](references/request-response-vs-notifications.md) |
| Streaming results back from a handler with `IAsyncEnumerable` | [references/streaming.md](references/streaming.md) |
| Centralizing exception translation across handlers | [references/exception-handling.md](references/exception-handling.md) |
| Structuring commands vs. queries as a convention | [references/cqrs-pattern.md](references/cqrs-pattern.md) |
| Recognizing when MediatR adds more indirection than it's worth | [references/pitfalls-and-tradeoffs.md](references/pitfalls-and-tradeoffs.md) |
| Unit testing a handler or behavior, and integration-testing the full pipeline | [references/testing.md](references/testing.md) |

## Example prompts

- "Why does my logging behavior run after validation instead of before it?"
- "Should this be a MediatR command or a notification, given the caller doesn't need a result back?"
- "Write a unit test for this request handler without touching the real database."
