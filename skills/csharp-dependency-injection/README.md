# C# Dependency Injection

Guidance on `Microsoft.Extensions.DependencyInjection`, the built-in .NET dependency injection
container — the routing table (by situation) is in [SKILL.md](SKILL.md).

```text
references/                          one file per concern
  core-concepts.md                     IServiceCollection, ServiceDescriptor, BuildServiceProvider,
                                        IServiceProvider, IServiceScope / IServiceScopeFactory
  lifetimes.md                         AddSingleton / AddScoped / AddTransient — semantics and
                                        disposal behavior for each
  registration-patterns.md             instance registration, factory delegates, keyed services
                                        (AddKeyedSingleton/Scoped/Transient), TryAdd /
                                        TryAddEnumerable, open generics
  resolution-and-constructors.md       resolving IEnumerable<T>, constructor-injection conventions,
                                        multiple/ambiguous constructors
  validation-and-errors.md             ValidateOnBuild, ValidateScopes (ServiceProviderOptions),
                                        common startup DI exceptions and what they mean
  options-pattern.md                   IOptions<T> / IOptionsSnapshot<T> / IOptionsMonitor<T>
  extensibility-and-hosting.md         ASP.NET Core builder.Services vs. standalone generic host;
                                        IServiceProviderFactory<TContainerBuilder> extension point
  pitfalls.md                          captive dependencies, service-locator anti-pattern,
                                        over-registering as singleton
  testing.md                           minimal ServiceCollection for tests, ValidateOnBuild in test
                                        setup, preferring constructor injection over container
                                        resolution in unit tests
```

## Scope

The built-in `Microsoft.Extensions.DependencyInjection` container only (current stable release
**10.0.12**, shipping with **.NET 10**, MIT-licensed, part of the
[dotnet/runtime](https://github.com/dotnet/runtime) repository under the .NET Foundation). Covers
core types, all three lifetimes, every built-in registration pattern including keyed services and
open generics, resolution and constructor conventions, startup validation, the options pattern,
ASP.NET Core and standalone generic-host usage, the generic third-party-container extension point,
common pitfalls, and test-time usage.

Out of scope: any specific third-party DI container's API, AOP/interception, property injection,
XML-based configuration, and `IConfiguration`/configuration-provider mechanics beyond what's needed
to explain how `IOptions<T>` binds through DI. See [SKILL.md](SKILL.md) for the full out-of-scope
list and rationale.

This skill is self-contained: it does not assume any other skill is installed.
