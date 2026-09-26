# Assembly scanning

`RegisterAssemblyTypes` registers many types from one or more assemblies at once, filtered and
mapped to services by convention, instead of one `RegisterType<T>()` call per class. This is the
standard way to wire up large families of same-shaped components (handlers, validators,
repositories) without hand-listing every implementation.

## Basic scan

```csharp
builder.RegisterAssemblyTypes(typeof(OrderService).Assembly)
    .Where(t => t.Name.EndsWith("Repository"))
    .AsImplementedInterfaces();
```

`RegisterAssemblyTypes` accepts one or more `Assembly` instances (commonly
`typeof(SomeMarkerType).Assembly` or `Assembly.GetExecutingAssembly()`), then narrows with:

- `.Where(Func<Type, bool>)` — arbitrary predicate filtering (naming convention, namespace,
  implementing a marker interface, custom attribute presence, etc.).
- `.As<TService>()` / `.AsImplementedInterfaces()` / `.AsSelf()` — same service-exposure options as
  a single `RegisterType`, applied uniformly to every matched type.
- `.PublicOnly()` (default) vs. allowing non-public types.
- `.Except<T>(...)` to exclude a specific type from an otherwise-matching scan, optionally with its
  own separate configuration.

## Convention example: register every `IHandler<T>` implementation

```csharp
builder.RegisterAssemblyTypes(typeof(CreateOrderHandler).Assembly)
    .AsClosedTypesOf(typeof(IHandler<>))
    .InstancePerLifetimeScope();
```

`AsClosedTypesOf(typeof(IOpenGeneric<>))` matches every concrete type implementing some closed
form of the given open generic interface and registers each as that closed interface — the
scanning equivalent of the open-generic `RegisterGeneric` pattern in
`references/registration-syntax.md`, for cases where each handler is its own concrete class rather
than a single generic implementation.

## When to prefer scanning vs. explicit registration

- **Scanning** pays off once there are enough same-shaped types (handlers, validators, one class
  per domain event) that a convention reliably identifies "everything that belongs" and manual
  upkeep would just be a list that goes stale.
- **Explicit `RegisterType` calls** (directly or inside a module) stay preferable for a small,
  stable set of services, or when the mapping between implementation and service isn't a clean
  convention — a scan's `Where` predicate that grows increasingly specific to exclude
  almost-but-not-quite-matching types is a sign explicit registration would be clearer.

Scanning failures are also a common source of "why isn't this registered" confusion: a renamed
type that no longer matches a naming-convention `Where` filter, or a new type intentionally
excluded from scanning, silently stops being registered rather than producing a compile error —
weigh that discoverability cost against the boilerplate savings.
