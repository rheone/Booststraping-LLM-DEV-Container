# xUnit.net

Guidance on xUnit.net, a third-party unit testing framework for C#/.NET — the routing table (by
situation, not by xUnit version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per xUnit version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `[Fact]`, `[Theory]`, test discovery, project setup, xunit.v3 vs. the legacy v2 line |
| `test-lifecycle.md` | Constructor/`IDisposable` per-test setup and teardown, `IClassFixture<T>`, `ICollectionFixture<T>`, `IAsyncLifetime` |
| `data-driven-tests.md` | `[InlineData]`, `[MemberData]`, `[ClassData]` |
| `assertions.md` | `Assert.*` catalog, fluent assertion library options |
| `parallelization-and-collections.md` | Test collections, `[Collection]`, parallelization defaults and controls, `ITestOutputHelper` |
| `testing-your-test-infrastructure.md` | Testing custom `DataAttribute` sources and shared fixture setup/teardown |

## Scope

xUnit.net's attribute-based test-authoring model (`xunit.v3`/`xunit.v3.core`, and the legacy
`xunit`/`xunit.core` v2 line): test discovery, lifecycle, data-driven tests, assertions, and
collection/parallelization behavior. Out of scope: third-party fluent assertion libraries' full API
surface, mocking/substitution libraries, and database reset strategies between tests — see
[SKILL.md](SKILL.md#out-of-scope) for the full list.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
xunit.v3 4.0.1 (core framework 3.2.2).
