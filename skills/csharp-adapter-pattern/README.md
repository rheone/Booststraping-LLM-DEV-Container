# C# Adapter Pattern

Reference for the Adapter design pattern in C#: converting a class's existing interface into the
interface a consumer expects, without modifying either side. The routing table (by situation) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per concern, not per package or version — Adapter is a structural
pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `philosophy-and-structure.md` | target interface, adaptee, adapter roles; when to reach for the pattern versus changing the adaptee directly |
| `object-adapter.md` | the composition-based adapter — the idiomatic C# form; wrapping an instance, delegating calls, translating shapes |
| `class-adapter.md` | the inheritance-based adapter; why C#'s single-inheritance rule makes it uncommon, and the narrow cases where it still applies |
| `generic-adapter.md` | a reusable `IAdapter<TSource, TTarget>` interface; when a generic adapter abstraction earns its keep versus one-off adapter classes |
| `adapting-third-party-apis.md` | wrapping a third-party library's API shape behind your application's own abstraction, described generically |
| `extending-with-new-adapters.md` | adding a new adapter for a new adaptee without touching existing adapters or consumers |
| `testing-adapters.md` | testing code that depends on the target interface; testing an adapter's translation logic directly |

## Scope

A structural design pattern, not a package — there is no version or license to track. Guidance
applies to any C# codebase regardless of target framework; the pattern has been expressible since
C# 1.0, and generics (C# 2.0 onward) add the generic adapter form covered in
`generic-adapter.md`.

Out of scope: the Facade pattern's subsystem-simplification goal, and any specific third-party
library's actual API surface. See [SKILL.md](SKILL.md) for the full out-of-scope list.
