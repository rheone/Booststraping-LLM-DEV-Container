---
name: csharp-autofac
description: Guidance on Autofac, a third-party IoC/dependency-injection container for C#/.NET (current stable release 9.3.4, MIT licensed). Covers ContainerBuilder/RegisterType/RegisterInstance/Build/Resolve core usage, registration syntax (As/AsSelf/AsImplementedInterfaces, named/keyed registrations, open generics), lifetime scopes (InstancePerDependency, SingleInstance, InstancePerLifetimeScope, InstancePerMatchingLifetimeScope, InstancePerRequest, nested scopes and disposal), Autofac.Module-based registration organization, property/method injection, decorators and interceptors, ASP.NET Core integration via UseServiceProviderFactory, assembly scanning, Lazy<T>/Func<T>/Owned<T> relationship types, common pitfalls (captive dependencies, over-scoping, disposal surprises), and how to test Autofac-dependent code. Use when writing, reviewing, or debugging code that registers or resolves services through Autofac's ContainerBuilder/IContainer/ILifetimeScope API, or when deciding how to structure Autofac modules and lifetime scopes.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Autofac

Guidance on Autofac, a mature third-party IoC/dependency-injection container for .NET, distinct
from (and predating) `Microsoft.Extensions.DependencyInjection` (MEDI). Current stable release as
of this writing: **9.3.4**, MIT licensed (see [Licensing](#licensing) below). Organized by
concern/topic, not by C# or Autofac version — Autofac's core API has been stable across its 9.x
line, so each reference file notes a version-introduced fact inline rather than splitting files by
version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| First time wiring up a container, or reviewing basic setup | `ContainerBuilder`, `Build()`, `IContainer`, `Resolve<T>`/`TryResolve<T>` | [references/core-concepts.md](references/core-concepts.md) |
| Deciding how a service should be exposed or looked up | `As<T>`, `AsSelf`, `AsImplementedInterfaces`, named/keyed registrations, open generics | [references/registration-syntax.md](references/registration-syntax.md) |
| Deciding how long an instance should live | `InstancePerDependency`, `SingleInstance`, `InstancePerLifetimeScope`, `InstancePerMatchingLifetimeScope`, `InstancePerRequest`, nested scopes, disposal | [references/lifetime-scopes.md](references/lifetime-scopes.md) |
| Organizing registrations across a large app or class library | `Autofac.Module`, `Load(ContainerBuilder)`, module composition vs. plain registration calls | [references/modules.md](references/modules.md) |
| Wiring dependencies onto properties/methods instead of constructors | `PropertiesAutowired`, `InjectProperties`, `WithParameter`/method injection patterns | [references/property-method-injection.md](references/property-method-injection.md) |
| Wrapping a service with cross-cutting behavior | `RegisterDecorator`, interceptors (`EnableInterfaceInterceptors`/`EnableClassInterceptors`) | [references/decorators-interceptors.md](references/decorators-interceptors.md) |
| Replacing the built-in ASP.NET Core container | `UseServiceProviderFactory(new AutofacServiceProviderFactory())`, `ConfigureContainer` | [references/aspnetcore-integration.md](references/aspnetcore-integration.md) |
| Registering many types at once by convention | `RegisterAssemblyTypes`, `Where`/`As` conventions | [references/assembly-scanning.md](references/assembly-scanning.md) |
| A dependency needs to be deferred, optional-until-used, or explicitly disposed | `Lazy<T>`, `Func<T>`, `Owned<T>`, circular dependency resolution | [references/relationship-types.md](references/relationship-types.md) |
| Something resolves the "wrong" instance, leaks, or throws unexpectedly | Captive dependencies, over-scoping to `SingleInstance`, disposal surprises | [references/common-pitfalls.md](references/common-pitfalls.md) |
| Unit/integration testing code that depends on Autofac | Minimal test containers, verifying registrations resolve, testing modules in isolation, when to skip the container entirely | [references/testing-with-autofac.md](references/testing-with-autofac.md) |

## Quick start

The shape every Autofac setup starts from: build a container once at startup, resolve from it (or
a child scope) at the edges of the app, and never pass the container itself into application code.

```csharp
var builder = new ContainerBuilder();

builder.RegisterType<ConsoleLogger>().As<ILogger>().SingleInstance();
builder.RegisterType<OrderService>().AsSelf().InstancePerLifetimeScope();
builder.RegisterInstance(new AppConfig { Retries = 3 }).AsSelf();

using IContainer container = builder.Build();

using ILifetimeScope scope = container.BeginLifetimeScope();
var orderService = scope.Resolve<OrderService>();
```

`Resolve<T>` throws `Autofac.Core.Registration.ComponentNotRegisteredException` if nothing is
registered for `T`; use `TryResolve<T>(out var value)` when absence is an expected, handled case
rather than a bug.

## Licensing

Autofac (core `Autofac` package and the official `Autofac.*` extension packages, e.g.
`Autofac.Extensions.DependencyInjection`) is **MIT licensed** — a permissive, free, open-source
license with no commercial tier, no paid license requirement, and no usage restrictions by company
size or revenue. This has been Autofac's licensing model throughout its history and remains
current as of this writing (latest stable release 9.3.4). There is no dual-license or
"commercial after N users/revenue" model to be aware of here — unlike some other popular .NET
libraries that have moved to commercial licensing, Autofac's license situation is simple and
unchanged: verify the `LICENSE` file in whatever version you actually pull if this matters for
compliance tracking, but there is nothing unusual to design around.

## Out of scope

- `Microsoft.Extensions.DependencyInjection` (the built-in .NET container) API and behavior,
  except where directly relevant to bridging it to Autofac in ASP.NET Core hosting.
- Third-party Autofac integration packages beyond the ASP.NET Core hosting bridge (e.g. WCF, OWIN,
  MVC5-specific packages) — these are largely legacy and narrower in scope than the core container.
- General dependency-injection theory/patterns not specific to Autofac's API surface.
