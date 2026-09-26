# Core concepts

## What Testcontainers gives you

Testcontainers for .NET starts real Docker containers from test code and tears them down again,
so an integration test exercises the actual dependency (a real database engine, a real message
broker) instead of an in-memory substitute or a hand-rolled fake. The container's lifetime is tied
to the test that started it — you never point tests at a shared, long-lived environment.

## The generic builder: `ContainerBuilder`

`Testcontainers.Builders.ContainerBuilder` is the entry point for any image, prebuilt module or
not. It is a fluent, immutable builder: every call returns a new builder value, so you chain calls
rather than mutating one in place.

```csharp
using Testcontainers.Builders;

var container = new ContainerBuilder()
    .WithImage("redis:7.4")
    .WithPortBinding(6379, true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
    .Build();

await container.StartAsync();

var mappedPort = container.GetMappedPublicPort(6379);
```

- `WithImage` names the Docker image (registry/repository:tag) to pull if it isn't already local.
- `WithPortBinding(containerPort, assignHostPort: true)` publishes a container port to a random
  free host port rather than a fixed one — a fixed host port collides the moment two test runs (or
  two parallel test classes) try to bind it at the same time. Read the actual bound host port back
  with `GetMappedPublicPort(containerPort)` after the container starts; don't hard-code it.
- `WithWaitStrategy` gates `StartAsync()` on the container actually being ready to accept work, not
  merely "running" — see `references/wait-strategies.md`.
- `Build()` produces an `IContainer` (or a more specific type for prebuilt modules); it does not
  start anything by itself.

## Starting and stopping

`StartAsync()` pulls the image if needed, creates the container, applies the configured wait
strategy, and returns once the container is observably ready. `StopAsync()` stops it without
removing it; `DisposeAsync()` (every container is `IAsyncDisposable`) stops and removes it. Prefer
disposal over an explicit `StopAsync()` for cleanup — see `references/lifecycle-and-xunit.md` for
where that call belongs in a test class, and `references/cleanup-and-ryuk.md` for what happens if a
test process crashes before disposal runs at all.

```csharp
await using var container = new ContainerBuilder()
    .WithImage("redis:7.4")
    .Build();

await container.StartAsync();
// ... use container ...
// disposed automatically at the end of the `await using` block
```

## Environment variables, commands, and bind mounts

`WithEnvironment(name, value)`, `WithCommand(params string[] command)`, and
`WithBindMount(hostPath, containerPath)` configure the container the same way the equivalent
`docker run` flags would. Chain as many as the image needs:

```csharp
var container = new ContainerBuilder()
    .WithImage("postgres:16")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithPortBinding(5432, true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
    .Build();
```

A prebuilt module (see `references/prebuilt-modules.md`) wraps exactly this kind of configuration
for a specific technology, so most day-to-day tests reach for a module rather than assembling a
`ContainerBuilder` from scratch — but the generic builder is what a module itself is built from,
and it's the right tool the moment you need an image with no dedicated module.
