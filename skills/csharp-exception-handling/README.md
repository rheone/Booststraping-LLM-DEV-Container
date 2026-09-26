# C# Exception Handling

The routing table is in [SKILL.md](SKILL.md).

**`references/`** — version-gated core syntax, oldest to newest

| File | Covers |
| --- | --- |
| `csharp1-try-catch-finally.md` | any target (C# 1.0) — try/catch/finally, Exception-derivation rule |
| `csharp2-runtimewrappedexception.md` | .NET Framework 2.0+ (C# 2.0) — RuntimeWrappedException auto-wrap |
| `csharp6-exception-filters.md` | VS 2015+ (C# 6.0) — the `when` exception filter clause |
| `csharp7-throw-expressions.md` | VS 2017+ (C# 7.0) — throw as an expression |

```text
specialized/                      cross-cutting patterns, applicable across versions
  exception-filters-in-depth.md
  custom-exception-design.md
  aggregateexception-and-flattening.md
  exceptiondispatchinfo-and-rethrow-patterns.md
  generic-exception-handling-helpers.md
  testing-with-exception-handling.md
```

## Version coverage

| .NET | C# | GA | Exception-handling-relevant additions |
| --- | --- | --- | --- |
| .NET Framework 1.0/1.1 | C# 1.0 | Jan 2002 | `try`/`catch`/`finally`; thrown/caught types must derive from `System.Exception` |
| .NET Framework 2.0 | C# 2.0 | Nov 2005 | CLR auto-wraps non-CLS-compliant throws in `RuntimeWrappedException`, so `catch (Exception ex)` observes them |
| .NET Framework 4.0 | — (BCL, not language) | 2010 | `AggregateException` (Task Parallel Library) |
| .NET Framework 4.5 | — (BCL, not language) | Aug 2012 | `ExceptionDispatchInfo.Capture`/`.Throw()` |
| .NET Framework 4.6 / .NET Core 1.x | C# 6.0 | Jul 2015 | Exception filters (`when`) |
| .NET Framework 4.6.2 / .NET Core 1.x | C# 7.0 | Mar 2017 | Throw expressions |
| — | C# 3.0–5.0, 8.0–15 | — | No exception-handling-specific language changes |

<!-- Keep this table's rows in sync with SKILL.md's routing table — same tiers, same order.
     On a maintenance pass, re-check the previously-newest row's GA/RC status before appending
     a new one. -->
