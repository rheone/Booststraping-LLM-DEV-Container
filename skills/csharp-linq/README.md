# C# LINQ

Reference for C# LINQ: query syntax and method syntax over `IEnumerable<T>` (LINQ to Objects) and
`IQueryable<T>` (provider-translated, e.g. EF Core), deferred vs. immediate execution, and the
standard query operators (C# 3.0 / .NET Framework 3.5) through the BCL's later `System.Linq`
additions — `Chunk`/`MinBy`-`MaxBy`/`*By` set operators/`TryGetNonEnumeratedCount`/3-way `Zip`
(.NET 6), `Order`/`OrderDescending` (.NET 7), `Index`/`CountBy`/`AggregateBy` (.NET 9),
`LeftJoin`/`RightJoin`/`Shuffle` (.NET 10), and `FullJoin`/tuple-returning `Join` (.NET 11 RC). The
routing table is in [SKILL.md](SKILL.md).

```text
references/                                  version-gated core syntax, oldest to newest
  pre-csharp3-manual-filtering.md              .NET Fx 1.0 – 2.0 (C# 1.0 – 2.0) — no LINQ; manual-loop/Predicate<T> fallback
  csharp3-linq-fundamentals.md                 .NET Fx 3.5+ (C# 3.0+) — the universal baseline
  csharp10-by-operators-and-chunking.md        .NET 6+ (C# 10+, target framework) — Chunk, MinBy/MaxBy, *By set operators, TryGetNonEnumeratedCount, 3-way Zip
  csharp11-order-shorthand.md                  .NET 7+ (C# 11+, target framework) — Order/OrderDescending
  csharp13-index-countby-aggregateby.md        .NET 9+ (C# 13+, target framework) — Index, CountBy, AggregateBy
  csharp14-leftjoin-rightjoin-shuffle.md       .NET 10+ (C# 14+, target framework) — LeftJoin, RightJoin, Shuffle
  csharp15-fulljoin-tuple-joins.md             .NET 11 RC1 Sept 2026+ (C# 15, target framework) — FullJoin, tuple-returning Join/GroupJoin

specialized/                                 cross-cutting patterns, applicable across versions
  ienumerable-vs-iqueryable-execution.md
  deferred-execution-pitfalls.md
  ordering-grouping-joining.md
  testing-linq-for-test-data-and-assertions.md
```

## Version coverage

| .NET | C# | GA | LINQ-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 – 2.0 | 1.0 – 2.0 | 2002 – 2005 | no LINQ: `Predicate<T>`/`Comparison<T>` + manual loops |
| Framework 3.5 | 3.0 | Nov 19, 2007 | LINQ introduced: query syntax, method syntax, `IEnumerable<T>`/`IQueryable<T>`, deferred/immediate execution, all classic standard query operators, expression trees |
| 6 | 10 | Nov 8, 2021 | `Chunk`, `MinBy`/`MaxBy`, `DistinctBy`/`UnionBy`/`IntersectBy`/`ExceptBy`, `TryGetNonEnumeratedCount`, 3-way `Zip` — BCL additions, gated by target framework not `LangVersion` |
| 7 | 11 | Nov 8, 2022 | `Order`/`OrderDescending` shorthand — BCL addition, gated by target framework |
| 9 | 13 | Nov 12, 2024 | `Index`, `CountBy`, `AggregateBy` — BCL additions, gated by target framework |
| 10 | 14 | Nov 11, 2025 | `LeftJoin`, `RightJoin`, `Shuffle` — BCL additions, gated by target framework |
| 11 | 15 | RC1 Sept 2026, GA expected Nov 2026 | `FullJoin`, tuple-returning `Join`/`GroupJoin` overloads — BCL additions, gated by target framework |

Every tier from .NET 6 onward is a `System.Linq` library addition, not a C# language feature — the
table pairs each one with the C# version its SDK shipped alongside (matching this skill's
`csharpN-<slug>.md` file-naming convention), but availability actually depends on the project's
target framework, not its `<LangVersion>` setting. Each reference file states this explicitly and
names its own fallback file, so a project on an older target framework — regardless of its
`LangVersion` — can still find the right tier.
