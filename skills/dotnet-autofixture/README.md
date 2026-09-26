# AutoFixture

Guidance on AutoFixture, a third-party library that generates test data automatically via
reflection-based object construction — the routing table (by situation, not by version) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per category, not per version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `Fixture`, `Create<T>`, `CreateMany<T>`, `Build<T>().With(...)`/`.Without(...)`, anonymous primitives |
| `customizations.md` | `ICustomization`, `Fixture.Customize<T>`, `FromFactory`, composing multiple customizations |
| `auto-mocking.md` | The general pattern for wiring a mocking library's auto-mock behavior into a `Fixture`, `Freeze<T>()` |
| `theory-attributes.md` | The `[AutoData]`/`[InlineAutoData]`-style data-attribute pattern, frozen parameters |
| `pitfalls.md` | Flaky/misleading assertions from uncontrolled randomness, circular reference handling |
| `testing-your-test-infrastructure.md` | Verifying a custom customization, auto-mock wiring, or data attribute you built on top of AutoFixture |

## Scope

AutoFixture's core `Fixture`/`ICustomization`/`Build<T>` API. Out of scope: any specific mocking
library's own configuration/verification API, any specific test framework's theory/attribute base
types, and generation-performance tuning beyond the recursion behaviors covered in
`references/pitfalls.md`.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
4.18.1.
