# C# Delegates and Lambdas

Reference for C# delegates and lambdas: the `delegate` keyword, multicast delegates, and method
group conversion (C# 1.0–2.0 / .NET Framework 1.0–2.0), anonymous methods and the first generic BCL
delegates (C# 2.0), lambda expression syntax and `Func<>`/`Action<>` (C# 3.0 / .NET Framework 3.5),
`in`/`out` variance and extended `Func<>`/`Action<>` arities up to 16 parameters (C# 4.0 / .NET
Framework 4.0), lambda parameter shadowing (C# 8.0), static lambdas and discard parameters (C# 9.0),
natural type inference and explicit return types (C# 10.0), default lambda parameters (C# 12.0), and
untyped modifier parameters (C# 14.0 / .NET 10). The routing table is in [SKILL.md](SKILL.md).

```text
references/                                            version-gated core syntax, oldest to newest
  csharp1-delegates-and-multicast.md                     .NET Fx 1.0+ (C# 1.0+) — the delegate keyword, multicast invocation lists; the universal baseline
  csharp2-anonymous-methods-and-generic-delegates.md     .NET Fx 2.0+ (C# 2.0+) — anonymous methods, Predicate<T>/Comparison<T>/Converter<TInput,TResult>, method group conversion, closures
  csharp3-lambdas-and-func-action.md                     .NET Fx 3.5+ (C# 3.0+) — lambda expression syntax, Func<>/Action<> (0-4 parameters)
  csharp4-variance-and-extended-func-action.md           .NET Fx 4.0+ (C# 4.0+) — in/out variance on generic delegates, Func<>/Action<> extended to 16 parameters
  csharp8-lambda-parameter-shadowing.md                  .NET Core 3.0+ (C# 8.0+) — a lambda parameter may shadow an enclosing-scope name
  csharp9-static-lambdas-and-discard-parameters.md       .NET 5+ (C# 9.0+) — static lambdas/anonymous methods, _ discard parameters
  csharp10-natural-type-and-lambda-annotations.md        .NET 6+ (C# 10.0+) — natural type for var, explicit return types, parameter attributes
  csharp12-default-lambda-parameters.md                  .NET 8+ (C# 12.0+) — default parameter values on lambdas
  csharp14-lambda-parameter-modifiers.md                 .NET 10+ (C# 14.0+) — scoped/ref/in/out/ref readonly on a lambda parameter without an explicit type

specialized/                                           cross-cutting patterns, applicable across versions
  closures-and-variable-capture.md
  multicast-delegate-invocation-semantics.md
  variance-in-generic-delegates.md
  generic-methods-as-lambdas-and-method-groups.md
  testing-delegates-and-lambdas.md
```

## Version coverage

| .NET | C# | GA | Delegate/lambda-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 | 1.0 | Jan 2002 | `delegate` keyword, multicast delegates (`+=`/`-=`); no generics yet |
| Framework 1.1 | 1.2 | 2003 | no delegate/lambda-relevant change |
| Framework 2.0 | 2.0 | Nov 2005 | anonymous methods, generics (`Predicate<T>`, `Comparison<T>`, `Converter<TInput,TResult>`), implicit method group conversion |
| Framework 3.5 | 3.0 | Nov 2007 | lambda expression syntax; `Func<>`/`Action<>` (0–4 parameters) |
| Framework 4.0 | 4.0 | Apr 2010 | `in`/`out` variance on generic delegates; `Func<>`/`Action<>` extended to 16 parameters |
| — | 5.0 – 7.3 | 2012 – 2018 | no delegate/lambda-specific change (C# 5.0's `foreach`-per-iteration variable scoping affects lambda closures but isn't a delegate/lambda syntax change itself; C# 6.0/7.0's expression-bodied *members* apply to methods/properties/etc., not lambdas) |
| Core 3.0 | 8.0 | Sep 2019 | lambda/anonymous-method parameters may shadow enclosing-scope names |
| 5 | 9.0 | Nov 2020 | `static` lambdas/anonymous methods; lambda discard parameters (`_`) |
| 6 | 10.0 | Nov 2021 | natural type for `var`-assigned lambdas/method groups; explicit lambda return types; attributes on lambda parameters |
| 7 | 11.0 | Nov 2022 | compiler caches the delegate for a `static`-method method-group conversion (performance only, no syntax change) |
| 8 | 12.0 | Nov 2023 | default parameter values on lambda expressions |
| 9 | 13.0 | Nov 2024 | no delegate/lambda-specific change |
| 10 | 14.0 | Nov 2025 | `scoped`/`ref`/`in`/`out`/`ref readonly` modifiers on lambda parameters without an explicit type |
| 11 (preview as of Sept 2026; GA expected Nov 2026) | 15.0 | preview | no delegate/lambda-specific change as of the Sept 2026 preview docs |

Every row's "no change" entry is deliberate, not an oversight — most C# versions touch delegates and
lambdas not at all, and a version missing from `references/` means exactly that: the previous tier's
file still applies unchanged.
