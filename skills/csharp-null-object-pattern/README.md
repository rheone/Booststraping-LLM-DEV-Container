# C# Null Object Pattern

Guidance on the Null Object design pattern in C# — providing a do-nothing implementation of an
interface so call sites never need a null check for missing behavior. The routing table (by
situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern/topic, not per package version — Null Object is a design
pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `core-concept-and-motivation.md` | the problem (repeated null checks for optional behavior), the pattern's shape, when it fits and when it doesn't |
| `singleton-vs-per-call-instances.md` | a shared stateless singleton instance vs. constructing a fresh null object per call |
| `generic-null-object-base.md` | a reusable, type-parameterized null-object base/factory for interfaces sharing a common shape |
| `nullable-reference-types-interaction.md` | how the pattern (no null check needed for absent behavior) relates to nullable reference types (the compiler flags a missing null check) |
| `testing.md` | using a null object as a trivial test double, and testing code that falls back to one |
| `extending.md` | adding a new no-op implementation or a new interface member without breaking existing callers |

## Scope

A design pattern, not a package — there is no version or license to pin, and no NuGet package this
skill tracks. Every example is generic and adapts directly to any interface a project defines with a
meaningful "do nothing" implementation.

Out of scope: general optional-value handling for data, and dependency-injection container
registration mechanics. See [SKILL.md](SKILL.md) for the full out-of-scope list.
