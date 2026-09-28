# Lifetime scopes

Autofac's lifetime model is scope-based, not purely "singleton vs. transient" — every resolution
happens *within* an `ILifetimeScope`, and a component's registered lifetime determines how sharing
works relative to that scope hierarchy. The root `IContainer` is itself the outermost
`ILifetimeScope`; every `BeginLifetimeScope()` call creates a child scope nested under it (or under
whatever scope it was called on).

## `InstancePerDependency` (the default)

A brand-new instance is created every time the service is resolved, anywhere. This is the default
when no lifetime is specified on a registration — `builder.RegisterType<T>().As<IT>()` with no
`.SingleInstance()`/`.InstancePerLifetimeScope()`/etc. is `InstancePerDependency` implicitly, and
calling `.InstancePerDependency()` explicitly is a no-op that just documents the choice.

```csharp
builder.RegisterType<OrderProcessor>().As<IOrderProcessor>(); // new instance per Resolve call
```

## `SingleInstance`

Exactly one instance for the lifetime of the root container (or the scope it's registered in, if
registered directly into a child scope rather than the root builder). Shared by every resolver
everywhere below that scope.

```csharp
builder.RegisterType<InMemoryCache>().As<ICache>().SingleInstance();
```

Disposed only when the *scope it was registered into* is disposed — for a component registered at
the root builder, that means it lives until the whole `IContainer` is disposed, typically at
process shutdown. Registering something `SingleInstance` that should really be scoped shorter
("over-scoping") is a common source of stale state and leaks — see `references/common-pitfalls.md`.

## `InstancePerLifetimeScope`

One instance per `ILifetimeScope` — shared by everything resolved *within* that scope (including
its children, unless a child re-registers the same service), but a fresh instance is created for
each new scope created with `BeginLifetimeScope()`.

```csharp
builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();

using var requestScope = container.BeginLifetimeScope();
var uow1 = requestScope.Resolve<IUnitOfWork>();
var uow2 = requestScope.Resolve<IUnitOfWork>();
// uow1 == uow2: same scope, same instance

using var anotherScope = container.BeginLifetimeScope();
var uow3 = anotherScope.Resolve<IUnitOfWork>();
// uow3 != uow1: different scope
```

This is the workhorse lifetime for "one instance per unit of work" (one per web request, one per
message handled, one per background job iteration) when combined with an app framework or manual
code that begins a new scope per unit of work.

## `InstancePerMatchingLifetimeScope(object tag)`

Like `InstancePerLifetimeScope`, but shared across an entire *tagged* scope and all of its
descendants, rather than being pinned to the exact scope that resolved it. Scopes are tagged with
`BeginLifetimeScope(tag)`; Autofac walks up the scope hierarchy from the resolving scope to find
the nearest ancestor (or self) tagged with a matching value, and shares the instance there.

```csharp
builder.RegisterType<RequestContext>().As<IRequestContext>()
    .InstancePerMatchingLifetimeScope("request");

using var requestScope = container.BeginLifetimeScope("request");
using var nestedScope = requestScope.BeginLifetimeScope(); // untagged child
var ctx = nestedScope.Resolve<IRequestContext>(); // shared with requestScope, not a new instance
```

If no ancestor scope carries the matching tag, resolution throws
`Autofac.Core.Lifetime.DependencyResolutionException` — this lifetime is a contract that "this
component may only be resolved from within a scope tagged X," which is useful as a guardrail
against accidentally resolving a request-scoped component outside of a request.

## `InstancePerRequest` (web)

A convenience wrapper, historically tied to the now-largely-superseded
`Autofac.Integration.Web`/OWIN-era per-HTTP-request scope tag. In modern ASP.NET Core apps built on
`Autofac.Extensions.DependencyInjection`, the framework already begins one child lifetime scope per
HTTP request automatically, so `InstancePerLifetimeScope()` registered in that pipeline behaves as
"one per request" without needing the separate `InstancePerRequest` API — reach for
`InstancePerLifetimeScope` in ASP.NET Core code and treat `InstancePerRequest` as a legacy name you
may still encounter in older codebases rather than something to add to new code.

## Nested lifetime scopes and disposal

`BeginLifetimeScope()` (optionally with a tag, optionally with a `configurationAction` to add
scope-local registrations) creates a child scope. Child scopes:

- Inherit every registration visible from their parent, plus anything registered locally via the
  `configurationAction` overload (local registrations can override/shadow a parent's for that
  scope and its descendants).
- Track every `IDisposable` they construct and dispose them, in reverse construction order, when
  the scope itself is disposed — this is true for `InstancePerDependency`,
  `InstancePerLifetimeScope`, and `InstancePerMatchingLifetimeScope` components resolved within
  that scope.
- Disposing a parent scope disposes all still-open child scopes recursively.

```csharp
builder.RegisterType<SqlConnectionWrapper>().As<IConnectionWrapper>()
    .InstancePerLifetimeScope();

using (var scope = container.BeginLifetimeScope(cfg =>
    cfg.RegisterInstance(new RequestId(Guid.NewGuid())).AsSelf()))
{
    var conn = scope.Resolve<IConnectionWrapper>();
    // ... use conn ...
} // conn (and any other IDisposable resolved in `scope`) is disposed here
```

`SingleInstance` components are the one exception: they're owned by whichever scope they were
*registered* in (the root, if registered on the original `ContainerBuilder`), not by whatever child
scope happens to resolve them, so disposing a child scope never disposes a singleton.

Always wrap scopes you create explicitly in `using`/`await using` — an unbounded, never-disposed
chain of child scopes is a memory leak, since each one keeps every `InstancePerLifetimeScope`
component it constructed alive until disposed.
