# Testcontainers for .NET

Testcontainers starts real, disposable Docker containers from test code, so integration tests run
against an actual database or broker instead of a mock or an in-memory stand-in. This skill covers
building a container with the generic builder or a prebuilt technology module, waiting until it's
actually ready, wiring its lifecycle into a test class, and cleaning it up afterward.

## When to reach for it

- Starting a container from an arbitrary image, or reaching for a prebuilt module for a well-known
  database or broker instead.
- A test hits the container before it's actually ready to accept connections.
- Wiring a container's startup and teardown into an xUnit-style test class or sharing one across
  multiple classes.
- Two containers in the same test need to reach each other over the network.
- Containers or networks aren't being removed after a run, or a test suite feels slow because of how
  broadly a container is shared.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing or
debugging integration tests that spin up containers via `ContainerBuilder`/`IContainer`. You can also
invoke it directly as `/dotnet-testcontainers`.

## What it covers

| Topic | Reference |
| --- | --- |
| ContainerBuilder, WithImage, WithPortBinding, StartAsync/DisposeAsync | [references/core-concepts.md](references/core-concepts.md) |
| Prebuilt `Testcontainers.<Technology>` modules and typed builders | [references/prebuilt-modules.md](references/prebuilt-modules.md) |
| Wait strategies for container readiness | [references/wait-strategies.md](references/wait-strategies.md) |
| IAsyncLifetime and ICollectionFixture wiring in xUnit-style tests | [references/lifecycle-and-xunit.md](references/lifecycle-and-xunit.md) |
| Networking two or more containers together | [references/networking.md](references/networking.md) |
| The Ryuk resource reaper and diagnosing leaked containers | [references/cleanup-and-ryuk.md](references/cleanup-and-ryuk.md) |
| Container scoping and reuse tradeoffs for suite performance | [references/performance.md](references/performance.md) |
| Verifying a custom fixture, wait strategy, or network helper behaves correctly | [references/testing-your-test-infrastructure.md](references/testing-your-test-infrastructure.md) |

## Example prompts

- "Set up a Postgres container for this integration test suite using the prebuilt module."
- "This test is hitting the container before it's ready: add a proper wait strategy."
- "Share one container across this whole test class instead of starting a new one per test."
