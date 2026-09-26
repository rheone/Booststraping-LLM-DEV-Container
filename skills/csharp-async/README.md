# C# Async

Reference for C# asynchronous programming: pre-language-support APM/EAP/bare-TAP patterns
(.NET Framework 1.0+) through `async`/`await` itself (C# 5.0 / .NET Framework 4.5), `await` in
`catch`/`finally` (C# 6.0), generalized async return types enabling `ValueTask<T>` (C# 7.0),
`async Main` (C# 7.1), async streams and async disposal (C# 8.0 / .NET Core 3.0), per-method
`AsyncMethodBuilder` (C# 10 / .NET 6), and `ref`/`unsafe` in async methods (C# 13 / .NET 9). The
routing table is in [SKILL.md](SKILL.md).

**`references/`** — version-gated core syntax, oldest to newest

| File | Covers |
| --- | --- |
| `pre-csharp5-apm-eap-tap.md` | .NET Fx 1.0 – 4.0 (C# 1.0 – 4.0) — APM, EAP, bare Task/ContinueWith |
| `csharp5-async-await.md` | .NET Fx 4.5+ (C# 5.0+) — the universal baseline |
| `csharp6-await-in-catch-finally.md` | .NET Fx 4.6+ (C# 6.0+) — await inside catch/finally |
| `csharp7-task-like-types.md` | .NET Core 1.0+ / .NET Fx 4.6.2+ (C# 7.0+) — generalized async return types, `ValueTask<T>` |
| `csharp7.1-async-main.md` | .NET Core 2.0+ (C# 7.1+) — async Main |
| `csharp8-async-streams.md` | .NET Core 3.0+ (C# 8.0+) — `IAsyncEnumerable<T>`, await foreach, await using |
| `csharp10-async-method-builder-on-methods.md` | .NET 6+ (C# 10+) — [AsyncMethodBuilder] on methods |
| `csharp13-ref-unsafe-in-async.md` | .NET 9+ (C# 13+) — ref locals and unsafe contexts in async methods |

```text
specialized/                                   cross-cutting patterns, applicable across versions
  cancellation-with-cancellationtoken.md
  configureawait-and-synchronization-context.md
  generic-async-methods-and-task-of-t.md
  async-disposal-patterns.md
  exception-handling-in-async-code.md
  testing-async-code.md
  runtime-async-performance.md
```

## Version coverage

| .NET | C# | GA | Async-relevant additions |
| --- | --- | --- | --- |
| Framework 1.0 – 4.0 | 1.0 – 4.0 | 2002 – 2010 | no language async support: APM (`BeginX`/`EndX`, .NET Fx 1.0), EAP (`MethodAsync` + `MethodCompleted`, .NET Fx 2.0), bare `Task`/`Task<T>` composed by hand with `ContinueWith` (.NET Fx 4.0) |
| Framework 4.5 | 5.0 | Aug 2012 | `async`/`await` keywords; `Task`/`Task<T>` become directly awaitable |
| Framework 4.6 | 6.0 | Jul 2015 | `await` allowed inside `catch`/`finally` blocks |
| Core 1.0+ / Framework 4.6.2+ | 7.0 | Mar 2017 | generalized async return types (task-like types); enables `ValueTask<T>` (shipped as a BCL type in .NET Core 2.0, Aug 2017) |
| Core 2.0 | 7.1 | Aug 2017 | `async Main` entry point |
| Core 3.0 | 8.0 | Sep 2019 | `IAsyncEnumerable<T>`, `await foreach`, async streams; `IAsyncDisposable`, `await using` |
| 6 | 10 | Nov 2021 | `[AsyncMethodBuilder]` attribute allowed on individual methods (not just types) |
| 9 | 13 | Nov 2024 | `ref` locals and `unsafe` contexts permitted inside async methods (and iterators) |
| 10 | 14 | Nov 2025 | no new async-specific language syntax |
| 11 | 15 | RC1 Sep 2026, GA expected Nov 2026 | no new async-specific language syntax; ships Runtime Async as a separate, non-syntax runtime/codegen feature — see [specialized/runtime-async-performance.md](specialized/runtime-async-performance.md) |

Each reference file states its own fallback file, so a project pinned to an older `LangVersion`
than its target SDK supports can still find the right syntax tier.
