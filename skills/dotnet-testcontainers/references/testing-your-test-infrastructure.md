# Testing your test infrastructure

Testcontainers' own job is test infrastructure — a fixture, a shared collection fixture, or a
custom builder wrapper you write on top of it is itself code that can be wrong. This file covers
verifying *that* code, not how to write ordinary application integration tests (the rest of this
skill's reference files cover building and using containers correctly; this file covers checking
that your own fixture/wrapper does what it claims).

## Verify a custom fixture actually starts and stops what it claims

If a fixture wraps container setup (a `DatabaseFixture` implementing `IAsyncLifetime`, as in
`references/lifecycle-and-xunit.md`), write a small smoke test against the fixture itself, separate
from any repository/business-logic test that consumes it:

```csharp
public class DatabaseFixtureTests
{
    [Fact]
    public async Task InitializeAsync_StartsAContainerThatAcceptsConnections()
    {
        var fixture = new DatabaseFixture();

        await fixture.InitializeAsync();

        try
        {
            Assert.Equal(TestcontainersStates.Running, fixture.Container.State);

            // A real round trip, not just a state check: confirms the wait strategy
            // actually gated startup on genuine readiness, not merely "port open".
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisposeAsync_RemovesTheContainer()
    {
        var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var containerId = fixture.Container.Id;

        await fixture.DisposeAsync();

        Assert.Equal(TestcontainersStates.Exited, fixture.Container.State);
    }
}
```

This kind of test is worth writing once per reusable fixture/wrapper class, not once per test suite
that consumes it — its job is to catch a broken wait strategy, a wrong image tag, or a
connection-string builder that doesn't match the container's actual configuration, before that
breakage surfaces as a confusing failure in every test that happens to use the fixture.

## Verify a custom wait strategy actually blocks until ready

For a hand-written `IWaitUntil` implementation (see `references/wait-strategies.md`), test it
against the specific readiness condition it claims to detect: start the target container without
the custom strategy, confirm an immediate operation against it fails or is unready, then start it
with the strategy applied and confirm the same operation now succeeds deterministically across
repeated runs. A wait strategy that "usually" works is exactly the kind of latent flakiness this
check exists to catch before it reaches the rest of the suite.

## Verify custom network wiring

If a helper composes a shared `INetwork` plus network aliases for multiple containers (see
`references/networking.md`), test that a container on the network can actually resolve and reach
another container's alias — not just that both containers report a `Running` state, which says
nothing about whether the network attachment or alias configuration is correct.

## What not to duplicate here

Don't use this file's patterns to re-test that Testcontainers itself works (that a `ContainerBuilder`
can start a container, that Ryuk removes resources) — that's the library's own correctness, verified
by its own test suite, not something this skill's guidance needs to re-prove. Scope these tests to
the wrapper/fixture code you wrote on top of it.
