# Modules

## `Autofac.Module`

A module packages a related group of registrations into a reusable, testable unit, instead of
scattering dozens of `builder.RegisterType(...)` calls across a `Program.cs`/`Startup.cs`. Subclass
`Autofac.Module` and override `Load(ContainerBuilder builder)`:

```csharp
public class OrderingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<OrderService>().As<IOrderService>().InstancePerLifetimeScope();
        builder.RegisterType<SqlOrderRepository>().As<IOrderRepository>().InstancePerLifetimeScope();
        builder.RegisterType<OrderValidator>().As<IOrderValidator>();
    }
}
```

Register the module itself on the builder rather than calling `Load` directly:

```csharp
var builder = new ContainerBuilder();
builder.RegisterModule<OrderingModule>();
builder.RegisterModule(new BillingModule(connectionString)); // modules can take constructor params
var container = builder.Build();
```

## Why modules over plain registration calls

- **Cohesion**: registrations for a feature/bounded-context live in one file next to the feature,
  instead of a monolithic composition-root method that grows without bound as the app grows.
- **Parameterization**: a module can take constructor arguments (e.g. a connection string, a
  feature flag) that vary per environment, letting `Program.cs` stay declarative
  (`RegisterModule(new BillingModule(config.ConnectionString))`) rather than branching inline.
- **Reuse across composition roots**: the same module can be registered from a web host, a worker
  service, and a test harness, keeping "what this feature needs registered" defined exactly once.
- **`AttachToComponentRegistration` / `AttachToRegistration` hooks**: modules can override these to
  observe or augment *every* registration made after them (e.g. attaching a common
  `PropertiesAutowired()` policy, or wiring logging decorators across the board) — something a flat
  sequence of `builder.RegisterType` calls has no equivalent hook for.

Plain registration calls directly on a `ContainerBuilder` remain perfectly fine for a small app or
a handful of registrations; reach for modules once registrations naturally group by feature, need
per-environment parameters, or are shared across more than one composition root/test project.

## Module composition order

Modules execute in the order they're registered, and later modules can see (and override defaults
for) services registered by earlier ones, same as plain sequential `RegisterType` calls would.
`RegisterModule` also accepts already-constructed module instances (not just `RegisterModule<T>()`
with a parameterless constructor), which is how constructor-parameterized modules get wired in.

## Nested modules

A module's `Load` can itself register other modules via
`builder.RegisterModule(new OtherModule())`, letting a top-level "app module" compose several
feature modules into one registration unit for a composition root to register with a single call.
