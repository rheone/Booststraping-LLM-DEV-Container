# Bogus

Guidance on Bogus, a third-party fake test-data generator for C#/.NET — the routing table (by
situation, not by Bogus version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per Bogus version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `Faker<T>`, `RuleFor`, `Generate()`/`Generate(count)` |
| `built-in-datasets.md` | `Person`, `Address`, `Commerce`, `Internet`, `Lorem`, `Date`, `Finance`, and other data sets |
| `seeding-and-determinism.md` | `Randomizer.Seed`, per-`Faker<T>` `.UseSeed(...)` |
| `related-object-graphs.md` | Nested `Faker<T>`, cross-property rules, `FinishWith`, generating collections |
| `testing-with-bogus-generated-data.md` | Avoiding flaky assertions against random values, shape-based assertions, snapshot-style tests |

## Scope

The core `Bogus` package's `Faker<T>`/`RuleFor` object-generation API and its built-in data sets.
Out of scope: the paid Bogus Premium add-on's extra datasets and tooling, persisting generated data
to a specific store, and general property-based/fuzz testing — see
[SKILL.md](SKILL.md#out-of-scope) for the full list.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
35.6.5.
