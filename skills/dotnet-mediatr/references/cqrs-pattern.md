# CQRS with MediatR: Convention, Not Enforcement

MediatR is frequently described as "a CQRS library." That's imprecise in a way worth being exact
about: **MediatR has no concept of "command" or "query."** There is no `ICommand`, no `IQuery`, no
built-in distinction anywhere in the library between a request that mutates state and one that
only reads it. Everything is an `IRequest<TResponse>`. CQRS-with-MediatR is a naming and folder-
structure convention that application code layers on top of MediatR, not a feature MediatR ships
or enforces.

## What MediatR actually gives you toward CQRS

- A uniform dispatch mechanism (`Send`) so callers (controllers, minimal API endpoints) don't
  need a different injected service per use case — they depend on `ISender` and pass in whichever
  request object represents the operation.
- A single-handler-per-request model that naturally maps to "one class does one thing," which
  fits the CQRS habit of a dedicated handler per command/query rather than a shared service class
  with many methods.
- Pipeline behaviors that can be scoped differently for commands vs. queries if you introduce your
  own marker interfaces (below) — e.g. only wrapping commands in a transaction, or only caching
  queries.

## The convention: introduce your own markers

Because MediatR doesn't distinguish commands from queries, projects that want that distinction
enforced (or just documented) typically define their own marker interfaces that extend
`IRequest<TResponse>`:

```csharp
public interface ICommand<TResponse> : IRequest<TResponse>;
public interface ICommand : IRequest;
public interface IQuery<TResponse> : IRequest<TResponse>;

public sealed record CreateOrder(string CustomerId, decimal Total) : ICommand<Guid>;
public sealed record GetOrderById(Guid OrderId) : IQuery<OrderDto?>;
```

These marker interfaces add nothing to MediatR's dispatch behavior by themselves — `Send` treats
`ICommand<Guid>` exactly like any other `IRequest<Guid>`, because it *is* one. Their value is
entirely for: readability at the call site, generic pipeline-behavior constraints (`where TRequest
: ICommand<TResponse>` to build a transaction behavior that only wraps commands), and static
analysis/architecture tests (e.g. asserting "nothing in the Queries folder implements `ICommand`")
that a team chooses to enforce on top.

## Typical folder/naming convention

```text
Application/
  Orders/
    Commands/
      CreateOrder/
        CreateOrder.cs              (the ICommand<Guid> record)
        CreateOrderHandler.cs
        CreateOrderValidator.cs     (e.g. FluentValidation, consumed by a ValidationBehavior)
    Queries/
      GetOrderById/
        GetOrderById.cs             (the IQuery<OrderDto?> record)
        GetOrderByIdHandler.cs
```

One request, one handler, one file group, colocated by use case rather than by technical layer
(all handlers together, all DTOs together) — this "vertical slice" organization is a very common
pairing with MediatR-based CQRS, though it is a separate organizational choice from MediatR
itself and can be done with or without MediatR.

## Where this stops being "real" CQRS

Textbook CQRS (Command Query Responsibility Segregation) as originally described goes further than
"commands and queries are named differently" — it typically implies separate read and write
*models*, and often separate data stores or at minimum separate query paths optimized
independently from the write path (e.g. a denormalized read model updated via events). MediatR's
command/query convention gives you the vocabulary and the dispatch mechanism, but adopting MediatR
does not, by itself, give a project a segregated read model — that's an architecture decision made
independently, usually only justified when read and write scaling/shape needs have genuinely
diverged. Most MediatR-based "CQRS" codebases are, more precisely, "same data store, same model,
consistently-named commands and queries dispatched through a common mediator" — which is a
perfectly reasonable and common pattern, just not the full textbook definition. Being precise
about which one a given codebase actually has avoids over-claiming architectural rigor it doesn't
have.

## Practical guidance

- Introduce `ICommand`/`IQuery` markers when the project is large enough that the distinction
  earns its keep (different pipeline treatment, architecture tests, onboarding clarity). For a
  small project, plain `IRequest<T>` throughout, named descriptively, may be all the "CQRS" that's
  actually needed.
- Don't claim a codebase implements "CQRS" in the strict sense just because it uses MediatR with
  Commands/ and Queries/ folders — say what's actually there (naming/dispatch convention vs. a
  genuinely segregated read/write model) when it matters for a design discussion.
