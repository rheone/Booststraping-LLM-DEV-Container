# NSubstitute

Guidance on NSubstitute, a third-party mocking/test-double library for C#/.NET — the routing table
(by situation, not by NSubstitute version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per NSubstitute version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `Substitute.For<T>()` for interfaces, classes, and delegates |
| `configuring-return-values.md` | `Returns`, `ReturnsForAnyArgs`, multi-call sequences, throwing from a substitute |
| `argument-matchers.md` | `Arg.Any<T>`, `Arg.Is<T>`, `Arg.Do<T>`, compound matcher expressions |
| `verifying-calls.md` | `Received()`, `Received(n)`, `DidNotReceive()`, `ReceivedWithAnyArgs()`, `ClearReceivedCalls()` |
| `async-support.md` | Configuring and verifying `Task<T>`/`ValueTask<T>`-returning members |
| `partial-substitutes.md` | `Substitute.ForPartsOf<T>()`, `.When(...).CallBase()` |
| `common-pitfalls.md` | Argument matcher scope rules, non-virtual members, over-specified verifications |
| `testing-your-test-doubles.md` | Testing shared substitute-factory helpers and custom argument matchers |

## Scope

NSubstitute's core substitute-creation, configuration, and verification API (`NSubstitute`
package). Out of scope: other mocking libraries, general test-framework mechanics, and integration
testing against real infrastructure — see [SKILL.md](SKILL.md#out-of-scope) for the full list.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing: 6.2.0.
