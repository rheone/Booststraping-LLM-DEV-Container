# Testcontainers for .NET

Guidance on Testcontainers for .NET, a third-party library for starting real, disposable Docker
containers from test code — the routing table (by situation, not by version) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per category, not per version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `ContainerBuilder`, `WithImage`, `WithPortBinding`, `StartAsync`/`DisposeAsync`, environment/command/bind-mount configuration |
| `prebuilt-modules.md` | `Testcontainers.<Technology>` packages, typed builders, `GetConnectionString()`, when to fall back to the generic builder |
| `wait-strategies.md` | `Wait.ForUnixContainer()`, `UntilPortIsAvailable`, `UntilMessageIsLogged`, `UntilCommandIsCompleted`, `UntilHttpRequestIsSucceeded`, custom `IWaitUntil` |
| `lifecycle-and-xunit.md` | `IAsyncLifetime`, per-class fixtures, `ICollectionFixture` for sharing a container across test classes |
| `networking.md` | `NetworkBuilder`, `WithNetwork`, `WithNetworkAliases`, container-to-container reachability |
| `cleanup-and-ryuk.md` | Explicit disposal, the Ryuk resource reaper, `TESTCONTAINERS_RYUK_DISABLED`, diagnosing leaked containers |
| `performance.md` | Per-test vs. per-class vs. `.WithReuse(true)` container scoping, parallel-execution contention |
| `testing-your-test-infrastructure.md` | Verifying a custom fixture, wait strategy, or network helper you built on top of Testcontainers |

## Scope

Testcontainers' `Testcontainers` core package (`Testcontainers.Builders`, `IContainer`,
`INetwork`) and its prebuilt `Testcontainers.<Technology>` module packages. Out of scope: the
Docker daemon itself, non-.NET Testcontainers bindings, and choosing which technology to test
against.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
4.15.0.
