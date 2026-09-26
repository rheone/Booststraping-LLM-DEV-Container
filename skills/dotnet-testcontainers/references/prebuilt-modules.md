# Prebuilt modules

## Why reach for a module first

A module is a dedicated NuGet package (`Testcontainers.<Technology>`) that wraps the generic
`ContainerBuilder` with sane defaults for one technology: the right base image, the right wait
strategy for that engine's actual startup signal (not just "the port is open"), and a typed
connection-string helper. Reaching for a module instead of hand-assembling a `ContainerBuilder`
saves you from re-deriving a wait strategy that the module's maintainers have already gotten right
for that engine's startup quirks.

Database and message-broker modules exist as separate packages so a project only pulls in what it
actually tests against — installing `Testcontainers` (the core package) alone gets you the generic
builder with no technology-specific extras.

## Shape of a database module

Every relational/document database module follows the same builder-and-container shape. A generic
example (substitute the actual module package and builder type for the engine under test):

```csharp
await using var dbContainer = new <Technology>Builder()
    .WithImage("<technology>:<tag>")
    .WithDatabase("app_test")
    .WithUsername("test")
    .WithPassword("test")
    .Build();

await dbContainer.StartAsync();

string connectionString = dbContainer.GetConnectionString();
```

- `GetConnectionString()` (or an equivalent typed accessor) returns a connection string already
  wired to the container's dynamically mapped host port — you never construct the connection
  string by hand from `GetMappedPublicPort`.
- The module's default wait strategy blocks until the engine reports itself ready to accept
  connections (its own readiness probe or log-line pattern), not just until the container process
  has started — a database can be "running" for several seconds before it will actually accept a
  connection.
- Pin an explicit image tag (`WithImage("<technology>:<tag>")`) rather than accepting whatever
  default tag a module version ships with; the default drifts across module releases and an
  explicit tag keeps a test suite's behavior reproducible independent of which module version is
  installed.

## Message broker and other service modules

The same builder-and-container shape applies to non-database service modules (a message broker, a
cache, a search engine) — a typed builder for constructor-time configuration specific to that
service (exchange/queue setup, cluster mode, auth), and a typed container exposing whatever
connection detail callers need (a connection string, a bootstrap endpoint, a client-usable URI).
Check the specific module's package for which accessor it exposes; the pattern (image, typed
builder, typed connection accessor, engine-aware wait strategy) is consistent across modules.

## When there's no module for what you need

Not every technology has a maintained module. Fall back to the generic `ContainerBuilder` from
`references/core-concepts.md`, and write your own wait strategy tuned to that image's actual
readiness signal (see `references/wait-strategies.md`) — the value a module adds is exactly that
tuning, not anything the generic builder is incapable of expressing.
