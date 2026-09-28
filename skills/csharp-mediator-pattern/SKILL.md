---
name: csharp-mediator-pattern
description: Guidance on the Mediator design pattern in C# as a hand-rolled pattern — an IMediator-like interface that decouples a set of colleague objects/handlers from knowing about each other directly, a simple in-process request/handler dispatch implementation from scratch (a dictionary or DI-resolved handler lookup keyed by request type), the classic GoF intent of mediating communication between UI/component colleagues versus the common modern usage of mediator-style dispatch for CQRS-style commands and queries, and when hand-rolling a small dispatcher is preferable to adopting a full third-party mediator framework. Use when designing decoupled communication between a set of objects that would otherwise reference each other directly, implementing request/handler dispatch without a framework dependency, reviewing or writing a Send/Publish-style dispatcher, or deciding whether a project's dispatch needs justify a dependency versus a dozen lines of hand-rolled code. Described generically as a pattern you implement yourself — does not name or require any specific third-party mediator library.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Mediator Pattern

Mediator is a **design pattern**, not a package — there is nothing to install and no version to
pin. This skill documents the pattern itself, hand-rolled from scratch: an `IMediator`-shaped
interface, an in-process dispatcher that resolves the right handler for a request, the two
distinct intents the pattern gets used for, and when writing this yourself beats depending on a
framework for it.

Everything below is a **from-scratch implementation** you write directly in a project — never a
specific third-party mediator library's API surface, because the pattern's dispatch mechanics fit
in well under a hundred lines and don't require one.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Understanding what problem Mediator solves and its two distinct intents | [references/core-concept-and-two-intents.md](references/core-concept-and-two-intents.md) |
| Writing an in-process request/handler dispatcher from scratch | [references/hand-rolled-dispatcher.md](references/hand-rolled-dispatcher.md) |
| Writing a reusable, type-parameterized mediator/handler interface pair | [references/generic-mediator.md](references/generic-mediator.md) |
| Deciding how the dispatcher finds the right handler for a request | [references/handler-resolution-strategies.md](references/handler-resolution-strategies.md) |
| Deciding whether to hand-roll a dispatcher or adopt a full framework for it | [references/when-to-hand-roll-vs-framework.md](references/when-to-hand-roll-vs-framework.md) |
| Testing code that sends requests through a mediator, or testing a handler itself | [references/testing.md](references/testing.md) |
| Adding a new request type or handler without touching existing dispatch code | [references/extending.md](references/extending.md) |

## Quick start

```csharp
public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    TResponse Handle(TRequest request);
}

public interface IMediator
{
    TResponse Send<TResponse>(IRequest<TResponse> request);
}
```

A minimal dispatcher resolves the handler for whatever concrete request type it receives and hands
control to it — the caller never references the handler type directly:

```csharp
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _services;
    public Mediator(IServiceProvider services) => _services = services;

    public TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        dynamic handler = _services.GetRequiredService(handlerType);
        return handler.Handle((dynamic)request);
    }
}
```

That's the entire mechanism. Start with
[references/core-concept-and-two-intents.md](references/core-concept-and-two-intents.md) for what
problem this solves and which of its two common intents you're actually reaching for, then
[references/hand-rolled-dispatcher.md](references/hand-rolled-dispatcher.md) for a dispatcher that
avoids the `dynamic` cast above.

## Out of scope

- Naming or depending on any specific third-party mediator library's API, pipeline behaviors, or
  package conventions. Everything here is a from-scratch implementation; adapt it freely.
- Cross-process or distributed messaging (message buses, queues, service-to-service RPC) — this
  skill covers strictly in-process, same-thread-or-same-async-context dispatch between objects in
  one running application.
- General CQRS architecture (how commands and queries relate to a domain model, read/write model
  separation) beyond the dispatch mechanism itself — this skill covers the mediator as a dispatch
  tool a CQRS-style codebase can use, not the architecture around it.
