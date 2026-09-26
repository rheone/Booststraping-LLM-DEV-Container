# Common pitfalls

## Captive dependencies

A "captive dependency" is a shorter-lived service accidentally held alive for longer than its own
registered lifetime because something longer-lived captured a reference to it. The classic shape:
a `SingleInstance` component takes an `InstancePerLifetimeScope`/`InstancePerDependency` component
as a constructor parameter. Autofac constructs the singleton (and its dependency) exactly once, so
that "per-scope" dependency is silently pinned for the lifetime of the singleton — every later
"new" instance other code resolves is a different object than the one the singleton is actually
using, and the captured one may reference resources (a `DbContext`, a request-scoped
correlation ID) tied to a scope that's long since ended.

```csharp
// Bug: CurrentUserAccessor is InstancePerLifetimeScope (per-request), but CachingService
// is SingleInstance — CachingService permanently captures whichever user resolved first.
builder.RegisterType<CurrentUserAccessor>().As<ICurrentUserAccessor>().InstancePerLifetimeScope();
builder.RegisterType<CachingService>().As<ICachingService>().SingleInstance();
```

Fix by either narrowing the singleton's lifetime to match its dependency, or by injecting
`Func<ICurrentUserAccessor>`/`Lazy<ICurrentUserAccessor>` (see `references/relationship-types.md`)
so the singleton re-resolves fresh on each use instead of capturing one instance at construction.
Autofac has no dedicated "captive dependency" warning built in, but its general resolve-pipeline
diagnostics (`container.SubscribeToDiagnostics(tracer)`, subclassing `DiagnosticTracerBase`, or the
`Autofac.Diagnostics.DotGraph` package to visualize a resolve operation as a graph) can be attached
while investigating an unexpected shared-state bug, to see exactly which registrations were
activated for which scope during a given resolve.

## Over-scoping to `SingleInstance`

Reaching for `.SingleInstance()` as a default "make it fast, avoid re-construction" habit is a
common overcorrection. Symptoms: state from one logical operation (a request, a job) leaking into
the next because the component was never re-created; thread-safety bugs in a component that was
fine when constructed fresh per use but wasn't written to be shared across concurrent
callers; and the captive-dependency problem above, since a singleton can only safely depend on
other singletons (or things resolved fresh via `Func<T>`/`Lazy<T>`). Default to
`InstancePerDependency` (the implicit default) or `InstancePerLifetimeScope`, and promote to
`SingleInstance` deliberately — for genuinely stateless, thread-safe, expensive-to-construct
components (a compiled regex cache, a configuration snapshot) — not as a blanket performance habit.

## Disposal surprises with lifetime scopes

- A component resolved from a scope that never gets disposed (a scope created and never wrapped in
  `using`/never explicitly disposed) leaks — its `IDisposable`s are never released, since disposal
  is scope-triggered, not garbage-collector-triggered.
- `RegisterInstance` does **not** dispose the instance you handed it when the container/scope is
  disposed, by default — the caller retains ownership. If Autofac should own and dispose it, use
  `.OwnedByLifetimeScope()` explicitly (or dispose it yourself, since you constructed it).
- Resolving an `InstancePerLifetimeScope`/`InstancePerDependency` component from the *root*
  container directly (rather than from a short-lived child scope) ties its disposal to
  process/container shutdown even though its registered lifetime suggests something shorter — the
  lifetime a component is *registered* with only bounds how long it can be shared *within* a given
  scope; disposal timing is still governed by whichever scope actually resolved it.
- Disposing a parent scope while a child scope built from it is still in active use invalidates
  that child — resolving from a disposed scope throws `ObjectDisposedException`. Keep scope
  lifetimes strictly nested (child disposed at or before its parent), never overlapping in the
  reverse order.

## Resolving via Service Locator instead of constructor injection

Passing `IContainer`/`ILifetimeScope`/`IComponentContext` itself into application code (rather than
the specific dependencies that code needs) and calling `.Resolve<T>()` deep inside business logic
hides the real dependency graph, defeats Autofac's own registration-validation tooling (nothing
declares the dependency, so nothing catches it missing until that code path actually runs), and
makes unit testing that code require a real container instead of a handful of plain mocks/fakes.
Reserve direct container access for the composition root and a small number of well-known
framework integration points (e.g. a controller-activation hook); everywhere else, declare
dependencies as constructor parameters and let Autofac wire them.
