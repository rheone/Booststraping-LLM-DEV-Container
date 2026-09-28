# Pitfalls and Tradeoffs

MediatR is a small library solving a specific problem (decoupling a caller from a handler within
one process). It is not free of cost, and it is not always the right tool. This file is a neutral
accounting of the tradeoffs, not an argument for or against adoption — current licensing terms are
a separate axis entirely, and worth researching independently before deciding.

## Overuse for simple in-process calls

The most common critique: introducing `IRequest`/`IRequestHandler`/`Send` for a call that could be
a plain method call on an injected service adds a layer of indirection that buys nothing when
there is exactly one caller and exactly one implementation, no cross-cutting pipeline behavior
that needs to apply, and no intent to ever swap the implementation. A one-line `orderService.
CreateOrder(...)` is not improved by becoming `sender.Send(new CreateOrder(...))` plus a
`CreateOrderHandler` class in a separate file, if nothing about testability, pipeline behaviors,
or decoupling is actually being exercised.

MediatR earns its cost when at least one of these is true:

- A pipeline behavior genuinely needs to apply uniformly (logging, validation, transactions) and
  doing so via method interception/AOP would otherwise require a different, heavier mechanism.
- The number of use cases is large enough that "one class per use case" meaningfully improves
  navigability over a small number of large multi-method services.
- Decoupling controllers/endpoints from the application layer's concrete types is a genuine goal
  (e.g. for testing the web layer against `ISender` alone).

It does not automatically earn its cost just because a codebase "uses CQRS" or "uses Clean
Architecture" — those are architectural choices independent of MediatR specifically (see
[cqrs-pattern.md](cqrs-pattern.md)).

## Debugging and traceability cost

Because dispatch happens through DI resolution at runtime (`Send(new SomeRequest(...))`), IDE
"Go to Definition" / "Find Usages" does not, by itself, jump from a `Send` call to the handler
that actually runs it — the connection is made by matching generic type arguments through
reflection-based registration, not a direct method reference. This has real, practical costs:

- New team members (and AI coding assistants navigating the codebase) have to know the convention
  ("handler class name usually matches the request name, in a sibling file/folder") rather than
  following a compiler-verified reference.
- A request type with **no** registered handler compiles fine and only fails at the `Send` call
  site, at runtime — there is no compile-time verification that every `IRequest<T>` has exactly
  one handler. Catching this class of bug requires either a runtime smoke test that exercises
  every request type, or a dedicated architecture/reflection test that asserts handler coverage
  at build/test time.
- Stack traces through a multi-behavior pipeline are deeper and noisier than a direct call stack,
  since each behavior is a frame the exception passes through.

Mitigations that reduce this cost without eliminating it: consistent naming/co-location
conventions (see [cqrs-pattern.md](cqrs-pattern.md)), a build-time or test-time check that every
`IRequest<T>` has a discoverable handler, and keeping the pipeline behavior count small and
well-documented so the "invisible middleware" surface stays comprehensible.

## Indirection tradeoff in code review

A reviewer looking at a single PR that adds a new command often cannot see, from that PR alone,
every pipeline behavior that will run against it — logging, validation, transactions, and any
exception handlers are all defined elsewhere and apply implicitly by matching generic constraints.
This is the same tradeoff any cross-cutting/AOP-style mechanism makes (it's not unique to
MediatR), but it's worth naming explicitly: reviewing "what does this command actually do end to
end" requires knowing the pipeline configuration, not just reading the handler class.

## Notification fan-out makes "what listens to this" hard to answer by inspection

Because any assembly-scanned `INotificationHandler<TSomeEvent>` picks up a notification
automatically, there is no single place that lists "everything that reacts to `OrderCreated`" —
answering that question requires a text search across the codebase (or a dedicated architecture
test) rather than reading a single dispatch site. This is the flip side of the decoupling
`Publish`/`INotification` is meant to provide (see
[request-response-vs-notifications.md](request-response-vs-notifications.md)) — the same property
that makes it easy to add a new reaction without touching the publisher also makes the full set of
reactions non-obvious from any one file.

## Neutral summary

None of the above is a reason to avoid MediatR outright — plenty of production codebases use it
productively at scale, and the pipeline-behavior/decoupling benefits are real when the conditions
above are met. It is a reason to make the adoption decision deliberately (per use case, even —
mixing plain service calls for simple cases with MediatR for pipeline-heavy or event-fan-out cases
is a legitimate middle ground) rather than reflexively routing every operation through it.
