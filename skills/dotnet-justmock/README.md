# Telerik JustMock

Guidance on Telerik JustMock, a .NET mocking framework with a profiler-based Elevated Mocking mode
— the routing table (by task, not JustMock version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per JustMock version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `Mock.Create<T>()`, Arrange/Act/Assert, `Arg.IsAny<T>()`, matchers, occurrence checks |
| `elevated-mocking-setup.md` | Profiler environment variables, mocking static/sealed classes and non-virtual members |
| `mocking-async-methods.md` | `ReturnsAsync`/`ThrowsAsync`, `Task`/`Task<T>` mocks, `ConfigureAwait` behavior in mocked chains |
| `testing.md` | Structuring tests around JustMock-mocked dependencies |

## Scope

Telerik JustMock only — creating and arranging mocks for .NET unit tests, including its
profiler-based Elevated Mocking mode. Out of scope: other mocking frameworks, and Telerik's other
tooling products (Test Studio, Fiddler, UI component libraries), which are separate products from
JustMock.

Each reference file notes a version-sensitive fact inline (verified package versions); version is
not the file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).

JustMock carries a non-standard license — research current terms independently before adopting it.
