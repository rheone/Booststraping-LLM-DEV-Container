---
name: dotnet-testcontainers
description: Guidance on Testcontainers for .NET, a third-party library for starting real, disposable Docker containers from test code (current stable release 4.15.0). Covers the generic Testcontainers.Builders.ContainerBuilder API, prebuilt technology modules (Testcontainers.<Technology> packages such as database or message-broker modules), container lifecycle via IAsyncLifetime in xUnit-style test classes and collection fixtures, wait strategies (Wait.ForUnixContainer, UntilPortIsAvailable, UntilMessageIsLogged, UntilHttpRequestIsSucceeded, custom IWaitUntil), networking between containers (NetworkBuilder, WithNetworkAliases), resource cleanup and the Ryuk resource reaper, performance tradeoffs between per-test/per-class/reused containers, and testing your own fixtures/wrappers built on top of it. Use when writing, reviewing, or debugging integration tests that spin up real containers via ContainerBuilder/IContainer, or when deciding how a container-backed test fixture should be scoped, shared, or cleaned up.
license: MIT
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Testcontainers for .NET

Guidance on Testcontainers for .NET, a third-party library that starts real, disposable Docker
containers from test code for integration testing. Current stable release as of this writing:
**4.15.0**. Organized by concern/topic, not by version — each reference file notes a
version-introduced fact inline where relevant.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Starting a container from an arbitrary image | `ContainerBuilder`, `WithImage`, `WithPortBinding`, `StartAsync`/`DisposeAsync` | [references/core-concepts.md](references/core-concepts.md) |
| Testing against a database, broker, or other well-known technology | `Testcontainers.<Technology>` modules, typed builders, `GetConnectionString()` | [references/prebuilt-modules.md](references/prebuilt-modules.md) |
| A test hits the container before it's actually ready | `Wait.ForUnixContainer()`, `UntilPortIsAvailable`, `UntilMessageIsLogged`, `UntilHttpRequestIsSucceeded`, custom `IWaitUntil` | [references/wait-strategies.md](references/wait-strategies.md) |
| Wiring a container into an xUnit-style test class or across classes | `IAsyncLifetime`, `ICollectionFixture`, class vs. collection fixtures | [references/lifecycle-and-xunit.md](references/lifecycle-and-xunit.md) |
| Two containers need to talk to each other | `NetworkBuilder`, `WithNetwork`, `WithNetworkAliases` | [references/networking.md](references/networking.md) |
| Containers/networks aren't getting removed, or a crashed run left orphans | Ryuk (the resource reaper), `TESTCONTAINERS_RYUK_DISABLED`, disposal ordering | [references/cleanup-and-ryuk.md](references/cleanup-and-ryuk.md) |
| Test suite is slow, or deciding how broadly to share a container | Per-test vs. per-class vs. `.WithReuse(true)`, parallel execution contention | [references/performance.md](references/performance.md) |
| Verifying a custom fixture, wait strategy, or network helper you wrote | Smoke-testing your own wrapper code, not application code | [references/testing-your-test-infrastructure.md](references/testing-your-test-infrastructure.md) |

## Quick start

```csharp
using Testcontainers.Builders;

public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly IContainer _dbContainer = new ContainerBuilder()
        .WithImage("postgres:16")
        .WithEnvironment("POSTGRES_PASSWORD", "postgres")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public Task InitializeAsync() => _dbContainer.StartAsync();

    public Task DisposeAsync() => _dbContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task Save_PersistsOrder()
    {
        var host = _dbContainer.Hostname;
        var port = _dbContainer.GetMappedPublicPort(5432);
        // build a connection string from host/port and exercise the repository
    }
}
```

A prebuilt module (see [references/prebuilt-modules.md](references/prebuilt-modules.md)) replaces
this hand-assembled setup with a typed builder and a ready-made connection-string accessor whenever
one exists for the technology under test.

## Out of scope

- Docker itself (installing/configuring the Docker daemon, image building, registry auth) — this
  skill assumes a working Docker environment and covers only the .NET client library that talks to
  it.
- Non-.NET Testcontainers language bindings (Java, Go, Node, etc.) — the API shapes differ across
  languages even where the underlying concepts (wait strategies, Ryuk) are shared.
- Choosing *which* database/broker/technology to test against — this skill covers how to run
  whichever technology you've already chosen inside a container for tests.
