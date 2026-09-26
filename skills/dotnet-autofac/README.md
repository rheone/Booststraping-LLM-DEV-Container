# Autofac

Guidance on Autofac, the third-party IoC/dependency-injection container for .NET that predates
`Microsoft.Extensions.DependencyInjection` and is still widely used for its module system, named
and keyed registrations, and richer lifetime-scope model.

## When to reach for it

- Wiring up a `ContainerBuilder` for the first time, or reviewing whether a container setup is
  sound.
- Deciding how long a registered service should live (`SingleInstance` vs.
  `InstancePerLifetimeScope` vs. a nested scope), and why a resolved instance is misbehaving.
- Tracking down a captive dependency, an over-scoped singleton, or a disposal surprise in code that
  resolves through Autofac.
- Organizing registrations across a large application or class library with `Autofac.Module`, or
  swapping Autofac in as the ASP.NET Core container.

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or debugging
code that registers or resolves services through Autofac's `ContainerBuilder`/`IContainer`/
`ILifetimeScope` API. You can also invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| ContainerBuilder, RegisterType, RegisterInstance, Build(), Resolve\<T>/TryResolve\<T> | [references/core-concepts.md](references/core-concepts.md) |
| As\<T>, AsSelf, AsImplementedInterfaces, named/keyed registrations, open generics | [references/registration-syntax.md](references/registration-syntax.md) |
| InstancePerDependency, SingleInstance, InstancePerLifetimeScope, nested scopes, disposal | [references/lifetime-scopes.md](references/lifetime-scopes.md) |
| Organizing registrations with Autofac.Module | [references/modules.md](references/modules.md) |
| Property and method injection instead of constructor injection | [references/property-method-injection.md](references/property-method-injection.md) |
| Decorators and interceptors for cross-cutting behavior | [references/decorators-interceptors.md](references/decorators-interceptors.md) |
| Replacing the built-in ASP.NET Core container with Autofac | [references/aspnetcore-integration.md](references/aspnetcore-integration.md) |
| Registering many types at once by convention | [references/assembly-scanning.md](references/assembly-scanning.md) |
| Lazy\<T>, Func\<T>, Owned\<T>, and circular dependency resolution | [references/relationship-types.md](references/relationship-types.md) |
| Captive dependencies, over-scoping, disposal surprises | [references/common-pitfalls.md](references/common-pitfalls.md) |
| Unit/integration testing code that depends on Autofac | [references/testing-with-autofac.md](references/testing-with-autofac.md) |

## Example prompts

- "Why is my `SingleInstance` service holding a stale scoped dependency?"
- "Show me how to register a module that groups all my repository types."
- "Swap ASP.NET Core's built-in container for Autofac in this project."
