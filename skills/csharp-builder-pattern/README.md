# C# Builder Pattern

Reference for the Builder design pattern in C#: classic GoF builder, fluent/chained builders,
generic self-typed (CRTP) builder bases, and step builders that enforce build order via the type
system. **Builder is a design pattern, not a language feature** — the tiers below track C#
language changes that altered how an idiomatic builder is written, or that compete with a builder
for simple cases (object initializers, init-only setters, records, required members). They are not
"the builder pattern shipped in C#N" claims; the pattern itself has been expressible since C# 1.0.
The routing table is in [SKILL.md](SKILL.md).

```text
references/                                              version-gated core syntax, oldest to newest
  pre-csharp2-classic-builder.md                            C# 1.0 — no generics; classic GoF builder + director, one non-generic class per product
  csharp2-generic-builders.md                               C# 2.0 — generics enable Builder<TProduct>/Builder<TSelf,TProduct>; CRTP self-typed chaining
  csharp3-object-initializers-and-fluent-extensions.md      C# 3.0 — object/collection initializers as an alternative; extension methods; lambda configuration
  csharp6-readonly-autoprops-and-expression-bodied-members.md  C# 6.0 — read-only auto-properties for the product; expression-bodied fluent methods
  csharp9-init-only-setters-and-records.md                  C# 9.0 — init-only setters and records/with narrow when a builder is needed; target-typed new
  csharp11-required-members.md                              C# 11.0 — required forces mandatory fields without a builder
  csharp12-primary-constructors.md                          C# 12.0 — primary constructors shrink product/builder boilerplate; collection expressions

specialized/                                              cross-cutting patterns, applicable across versions
  generic-self-typed-builder-base.md                        the CRTP Builder<TSelf,TProduct> pattern in depth
  step-builders-and-build-order-type-state.md               compile-time-enforced construction order via step interfaces
  builder-vs-modern-alternatives.md                         decision list: object initializer vs. required+init vs. with vs. builder
  testing-with-builders.md                                  test-data builders and object mothers for fixture setup
```

## Version coverage

| .NET | C# | GA | Builder-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 – 1.1 | 1.0 | 2002 | no generics, no object initializers; classic GoF builder + director is the only shape available |
| Framework 2.0 | 2.0 | Nov 7, 2005 | generics — a reusable, product-parameterized builder base becomes possible for the first time; the CRTP self-typed pattern (`where TSelf : Builder<TSelf, TProduct>`) for chains that return the derived builder type |
| Framework 3.5 | 3.0 | Nov 19, 2007 | object and collection initializers — a builder-free alternative for flat, all-public-settable product shapes; extension methods add fluent calls to a builder without owning its type; lambda expressions enable configuration-callback-style builder methods |
| — | 3.5 | Aug 2008 | no new builder-relevant capability |
| — | 4.0 | Apr 2010 | no new builder-relevant capability |
| — | 5.0 | Aug 2012 | no new builder-relevant capability |
| — | 6.0 | Jul 20, 2015 | read-only auto-properties (`{ get; }`) simplify the immutable product a builder's `Build()` targets; expression-bodied members collapse one-line fluent setters and `Build()` itself |
| — | 7.0 – 7.3 | 2017 – 2018 | no new builder-relevant capability |
| Core 3.0 | 8.0 | Sep 2019 | no new builder-relevant capability |
| 5 | 9.0 | Nov 10, 2020 | init-only setters (`{ get; init; }`) and records with `with`-expressions narrow when a builder is needed at all, for flat/no-validation and copy-with-changes cases respectively; target-typed `new` shortens every builder instantiation |
| 6 | 10.0 | Nov 8, 2021 | no new builder-relevant capability (record structs extend records to value types but add nothing builder-specific) |
| 7 | 11.0 | Nov 8, 2022 | `required` members force mandatory-field enforcement at compile time without a builder, for shapes with no cross-field validation |
| 8 | 12.0 | Nov 14, 2023 | primary constructors on ordinary classes/structs (not just records) shrink both the product type's and the builder's own constructor boilerplate; collection expressions (`[...]`, spread) simplify a builder's accumulated-list output |
| 9 | 13.0 | Nov 2024 | no new builder-relevant capability |
| 10 | 14.0 | Nov 2025 | no new builder-relevant capability |
| 11 (preview as of Sept 2026) | 15.0 | preview | no builder-relevant change documented in the Sept 2026 preview docs |

Every "no new builder-relevant capability" row is deliberate — a version genuinely adding nothing
to how a builder is written is informative on its own, not a gap to fill with an invented tier.
