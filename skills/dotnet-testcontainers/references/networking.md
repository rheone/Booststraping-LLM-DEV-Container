# Networking between containers

## Why containers can't reach each other by default

Each container Testcontainers starts is, by default, only reachable from the test host via its
mapped ports (`GetMappedPublicPort`). Two containers started independently don't automatically
share a Docker network, so a service inside one container can't resolve or reach a service inside
another by container name — they need to be placed on the same user-defined network explicitly.

## Creating a shared network

```csharp
using Testcontainers.Builders;

var network = new NetworkBuilder()
    .WithName("app-test-network")
    .Build();

await network.CreateAsync();

var dbContainer = new ContainerBuilder()
    .WithImage("postgres:16")
    .WithNetwork(network)
    .WithNetworkAliases("db")
    .Build();

var appContainer = new ContainerBuilder()
    .WithImage("my-app:test")
    .WithNetwork(network)
    .WithEnvironment("DB_HOST", "db")
    .Build();

await dbContainer.StartAsync();
await appContainer.StartAsync();
```

- `WithNetwork(network)` attaches a container to the shared user-defined network created via
  `NetworkBuilder`.
- `WithNetworkAliases(...)` gives a container a DNS name other containers on the same network can
  resolve — `appContainer` reaches the database container at host name `db`, using the database's
  *internal* port (5432), not the mapped host port `GetMappedPublicPort` would return. The mapped
  host port is only meaningful from the test process itself, not from inside another container.
- Dispose the network (`await network.DeleteAsync()`, or wrap it in the same `IAsyncLifetime` that
  owns the containers using it) after every container attached to it has been disposed — a network
  still referenced by a running container can't be removed.

## When you don't need a custom network

A test that only talks to a single container from the test process itself (the overwhelmingly
common case — a repository test against one database container) never needs `NetworkBuilder`; the
mapped-port access pattern in `references/core-concepts.md` is sufficient. Reach for a shared
network only when the system under test genuinely spans multiple containers that must talk to each
other directly (an application container calling a database container, or a producer/consumer pair
around a message broker container) as part of the scenario being tested.
