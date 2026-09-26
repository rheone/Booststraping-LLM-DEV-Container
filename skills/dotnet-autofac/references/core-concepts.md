# Core concepts

## `ContainerBuilder`

`Autofac.ContainerBuilder` is the entry point: a write-once, throw-away object used to accumulate
registrations before building an immutable container. A `ContainerBuilder` instance can only be
used to `Build()` once — after that, further registrations require either `builder.Update(...)`
(discouraged; mutates a live container and is largely a legacy escape hatch) or a fresh builder
plus module composition at startup.

```csharp
var builder = new ContainerBuilder();
builder.RegisterType<OrderService>().As<IOrderService>();
builder.RegisterType<SqlOrderRepository>().As<IOrderRepository>();

IContainer container = builder.Build();
```

## `RegisterType<T>` / `RegisterType(Type)`

Registers a concrete class so the container can construct it via reflection, picking a
constructor and satisfying its parameters recursively from other registrations. `RegisterType<T>`
is the generic, compile-time-checked form; `RegisterType(Type)` takes a runtime `Type` — used
heavily by assembly-scanning APIs (see `references/assembly-scanning.md`).

```csharp
builder.RegisterType<OrderService>();          // registered, but not exposed "as" anything queryable
builder.RegisterType<OrderService>().As<IOrderService>();  // exposed as the interface
```

Without an `As<T>`/`AsSelf`/`AsImplementedInterfaces` call, a plain `RegisterType<T>()` registers
the component but only makes it resolvable as its own concrete type — see
`references/registration-syntax.md` for how service exposure actually works and its defaults.

Autofac picks the constructor with the most parameters it can satisfy by default. If constructor
selection needs to be explicit (e.g. multiple public constructors, ambiguous), use
`.UsingConstructor(...)` to pin one down.

## `RegisterInstance<T>`

Registers an already-constructed object as a singleton — the container never constructs it, never
disposes it by default (the caller owns its lifetime unless `.OwnedByLifetimeScope()`/
`.ExternallyOwned()` is used to state that explicitly), and always returns the same reference.

```csharp
var config = new AppConfig { Retries = 3, TimeoutSeconds = 30 };
builder.RegisterInstance(config).As<IAppConfig>();
```

`RegisterInstance` is implicitly `SingleInstance()` scoped — there's only ever the one object you
handed the builder, so specifying a different lifetime doesn't make sense and Autofac will throw if
you try to override the scope on an instance registration.

## `Build()` / `IContainer`

`builder.Build()` compiles every registration into a component graph and returns an `IContainer` —
an immutable, thread-safe root that is itself an `ILifetimeScope` (the root scope). `IContainer`
implements `IDisposable`; disposing it disposes every `SingleInstance` and every resolved
`IDisposable` component still tracked by that scope (see `references/lifetime-scopes.md` for the
full disposal story).

```csharp
using IContainer container = builder.Build();
```

Apps typically build exactly one container at startup and hold onto it for the process lifetime;
building more than one container (e.g. per-request) defeats the purpose of lifetime scopes, which
exist precisely so a single container can service many short-lived units of work cheaply — see
`references/lifetime-scopes.md`.

## Resolving: `Resolve<T>` / `TryResolve<T>`

`Resolve<T>()` (an extension method on `IComponentContext`, which both `IContainer` and
`ILifetimeScope` implement) walks the component graph, constructing whatever isn't already cached
for the current scope's lifetime rules, and returns a fully-wired instance. It throws
`Autofac.Core.Registration.ComponentNotRegisteredException` if no registration satisfies the
requested type, and `Autofac.Core.DependencyResolutionException` (often wrapping an inner
exception) if construction itself fails partway through.

```csharp
var service = container.Resolve<IOrderService>();
```

`TryResolve<T>(out T instance)` returns `bool` instead of throwing, for call sites where "nothing
registered" is an expected, handled branch rather than a configuration bug:

```csharp
if (container.TryResolve<IAuditLogger>(out var auditLogger))
{
    auditLogger.Log("started");
}
```

Prefer `Resolve<T>` for anything that's a hard runtime dependency (let it fail loudly and early —
ideally at container-validation time, not first use) and reserve `TryResolve<T>` for genuinely
optional collaborators. Reaching for the container at all deep inside application code (rather than
at the composition root / a small number of well-known entry points) is itself a smell — see
`references/common-pitfalls.md`.

## Resolving without generics

`context.Resolve(typeof(IOrderService))` and `context.ResolveNamed("special", typeof(IOrderService))`
are the non-generic equivalents, used when the requested type is only known at runtime (e.g. plugin
loading, reflection-driven scenarios).
