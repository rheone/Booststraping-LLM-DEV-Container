# Registration

MediatR's DI wiring lives in the core `MediatR` package itself (a separate
`MediatR.Extensions.Microsoft.DependencyInjection` package existed historically but its
functionality has been folded into `MediatR` proper on current versions) — `AddMediatR` is an
extension method on `IServiceCollection`.

## Basic registration and assembly scanning

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

`RegisterServicesFromAssembly`/`RegisterServicesFromAssemblies` scans the given assembly (or
assemblies) via reflection for every closed implementation of `IRequestHandler<,>`,
`IRequestHandler<>`, `INotificationHandler<>`, `IStreamRequestHandler<,>`,
`IRequestExceptionHandler<,,>`, `IRequestExceptionAction<,>`, `IRequestPreProcessor<>`, and
`IRequestPostProcessor<,>`, and registers each concrete implementation against its handler
interface. You do not manually register individual handler classes when using assembly scanning —
that's the entire point of the scan.

```csharp
cfg.RegisterServicesFromAssemblies(
    typeof(Program).Assembly,
    typeof(CreateOrder).Assembly, // e.g. a separate Application layer assembly
    typeof(SomeIntegrationEventHandler).Assembly);
```

A common pattern in layered solutions: point the scan at a marker type from the assembly that
actually contains the handlers (an `Application` or `UseCases` project), not the web/host
assembly, since handlers rarely live in the entry-point project itself.

## Service lifetime

By default, `AddMediatR` registers `IMediator`, `ISender`, `IPublisher`, and every discovered
handler as **transient** services. This is deliberate: request handlers are typically stateless
per-invocation classes, and transient avoids accidentally holding captured dependencies (like a
scoped `DbContext`) across requests they shouldn't outlive.

Handler lifetime can be overridden per-registration-call via the configuration object's
`Lifetime` property, which applies to the handlers discovered by that scan:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.Lifetime = ServiceLifetime.Scoped; // rarely needed; transient is correct for most handlers
});
```

Prefer leaving handlers transient unless a specific handler genuinely needs to share state within
a single logical operation — and even then, injecting a scoped dependency (like `DbContext`) into
a transient handler is fine and is the normal pattern; the handler itself doesn't need to be
scoped just because one of its dependencies is.

## Registering pipeline behaviors, pre/post processors, and exception handlers

These are **not** picked up automatically by assembly scanning for open generic behaviors — they
must be explicitly registered (concrete closed-generic exception handlers targeting a specific
request type *are* discovered by the scan, same as regular handlers). Open generic behaviors that
apply to every request need an explicit `AddOpenBehavior`/service registration call:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Open generic — applies to every IRequest<TResponse>, in the order added:
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
```

Equivalently, behaviors can be registered directly on `IServiceCollection` as open generics
(`services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`) — both
approaches produce the same registration; `AddOpenBehavior` is the more common, more readable
form because it keeps all MediatR-related registration inside the single `AddMediatR` call. See
[pipeline-behaviors.md](pipeline-behaviors.md) for why registration order here matters.

## Configuring the license key at registration time

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"];
});
```

See [licensing.md](licensing.md) for the full licensing picture — the key can also be supplied via
the `MEDIATR_LICENSE_KEY` or `LUCKYPENNY_LICENSE_KEY` environment variables instead of code, which
is often preferable for keeping the key out of source control.

## Multiple calls to AddMediatR

Calling `AddMediatR` more than once accumulates configuration (additional assemblies, additional
behaviors) rather than replacing a prior call outright, but the simplest and least surprising
pattern is a **single call** with every assembly and behavior listed together, so the full
registration is visible in one place rather than scattered across the startup code.
