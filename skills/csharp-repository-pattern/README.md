# C# Repository Pattern

Reference for the Repository design pattern in C#: abstracting data access behind a collection-like
interface so application/domain code depends on a contract, not a data-access technology. The
routing table (by situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern, not per package or version — Repository is a structural
data-access pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `philosophy-and-structure.md` | the pattern's shape; the generic `IRepository<T>` base (Add/Get/Update/Delete/Query) |
| `generic-vs-specific-repositories.md` | one generic repository vs. a specific repository per aggregate — the tradeoffs on each side |
| `iqueryable-and-leaky-abstractions.md` | `IQueryable<T>`-returning methods and the risk of leaking ORM-specific query capability through the interface |
| `specification-based-queries.md` | a specification object as an alternative to accumulating many narrow query methods |
| `extending-with-new-repositories.md` | adding a repository for a new aggregate without touching existing repositories or consumers |
| `testing-with-fake-repositories.md` | testing code that depends on a repository via an in-memory/fake implementation |

## Scope

A structural data-access pattern, not a package — there is no version or license to track. Guidance
applies to any C# codebase regardless of the underlying data-access technology; the generic form
uses generics (C# 2.0 onward), and `IQueryable<T>` guidance applies from .NET Framework 3.5 onward.

Out of scope: any specific ORM's or data-access library's actual API, and cross-repository
transaction/unit-of-work coordination. See [SKILL.md](SKILL.md) for the full out-of-scope list.
