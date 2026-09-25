# C# Expression Trees

Reference for C# expression trees: `Expression<TDelegate>` and the `System.Linq.Expressions`
namespace, representing a lambda as an inspectable data structure instead of compiled IL, and how
`IQueryable<T>` providers like EF Core consume them (C# 3.0 / .NET Framework 3.5), plus the
DLR-era expansion of manually-buildable node types — `BlockExpression`, `LoopExpression`,
`TryExpression`, `ExpressionVisitor`, `DynamicExpression` — for hand-assembled trees (C# 4.0 /
.NET Framework 4.0). The routing table is in [SKILL.md](SKILL.md).

```text
references/                                       version-gated core syntax, oldest to newest
  pre-csharp3-no-expression-trees.md                 .NET Fx 1.0+ (C# 1.0+) — no Expression<TDelegate> yet; Reflection.Emit or a hand-rolled node/interpreter hierarchy fill the gap
  csharp3-expression-trees-fundamentals.md           .NET Fx 3.5+ (C# 3.0+) — Expression<TDelegate>, tree-vs-delegate target typing, IQueryable<T> provider translation, single-expression manual construction
  csharp4-dlr-expression-node-types.md               .NET Fx 4.0+ (C# 4.0+) — BlockExpression/LoopExpression/TryExpression/SwitchExpression/DynamicExpression, ExpressionVisitor

specialized/                                      cross-cutting patterns, applicable across versions
  building-expression-trees-manually.md
  expression-visitor-and-tree-rewriting.md
  expression-tree-limitations-and-pitfalls.md
  testing-expression-trees.md
```

## Version coverage

| .NET | C# | GA | Expression-tree-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 – 2.0 | 1.0 – 2.0 | 2002 – 2005 | no `Expression<TDelegate>` / `System.Linq.Expressions` at all |
| Framework 3.5 | 3.0 | Nov 19, 2007 | `Expression<TDelegate>` and `System.Linq.Expressions` introduced alongside LINQ; single-expression lambda-to-tree conversion; `IQueryable<T>`/`IQueryProvider` |
| Framework 4.0 | 4.0 | Apr 12, 2010 | DLR-driven expansion of `System.Linq.Expressions`: `BlockExpression`, `LoopExpression`, `TryExpression`, `SwitchExpression`, `GotoExpression`/`LabelExpression`, `DynamicExpression`, `RuntimeVariablesExpression`, `DebugInfoExpression`; `ExpressionVisitor` introduced. None of these are reachable via the compiler's own lambda-to-tree conversion, which stayed single-expression-only |
| — | 4.5 – 5.0 | 2012 – 2013 | no expression-tree-capability change |
| — | 6.0 | Jul 2015 | no new capability; several C# 6 syntax forms (null-propagating `?.`, dictionary initializers) are explicitly *disallowed* inside compiler-converted expression trees (`CS8072`, `CS8074`) |
| — | 7.0 – 7.3 | 2017 – 2018 | no new capability; several C# 7.x syntax forms (tuples, pattern matching, local functions, `throw` expressions, `ref` returns) are explicitly disallowed (`CS8143`, `CS8122`, `CS8110`, `CS8188`, `CS8153`) |
| Core 3.0 | 8.0 | Sep 2019 | no new capability; switch expressions and index/range operators explicitly disallowed (`CS8514`, `CS8790`–`CS8792`) |
| 5 | 9.0 | Nov 2020 | no new capability; `with`-expressions explicitly disallowed (`CS8849`) |
| 6 | 10.0 | Nov 2021 | no new capability; interpolated string handler conversions and lambda attributes explicitly disallowed (`CS8952`, `CS8972`) |
| 7 | 11.0 | Nov 2022 | no new capability; static abstract/virtual interface member access and `&` on a method group explicitly disallowed (`CS8927`, `CS8810`) |
| 8 | 12.0 | Nov 2023 | no new capability; inline array access and collection expressions explicitly disallowed (`CS9170`, `CS9175`) |
| 9 | 13.0 | Nov 2024 | no new capability; expanded (non-array) `params` collection forms explicitly disallowed (`CS9226`) |
| 10 | 14.0 | Nov 2025 | no new capability; extension property access as an extension explicitly disallowed (`CS9296`) |
| 11 (preview as of Sept 2026; GA expected Nov 2026) | 15.0 | preview | no expression-tree-relevant change documented in the Sept 2026 preview docs |

Every "no new capability" row is deliberate, not an oversight: nearly every C# release since 4.0
has added *another kind of syntax the compiler refuses to put in a tree*, not a new capability the
tree-building side gains. See
[specialized/expression-tree-limitations-and-pitfalls.md](specialized/expression-tree-limitations-and-pitfalls.md)
for the complete, current restriction table.
