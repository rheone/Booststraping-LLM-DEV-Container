# C# Unit of Work Pattern

Guidance on the Unit of Work design pattern in C# — tracking a set of changes across multiple
repository/data-access operations and committing them to a store as one atomic operation. The
routing table (by situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern/topic, not per package version — Unit of Work is a design
pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `core-concept-and-motivation.md` | the problem (partial writes leaving a store inconsistent), the pattern's shape, when you need it vs. when a single repository call already suffices |
| `unit-of-work-interface-and-repositories.md` | an `IUnitOfWork` interface coordinating multiple `IRepository<T>` instances against one shared connection/session, a single `SaveChanges` |
| `generic-unit-of-work.md` | a reusable, type-parameterized `IUnitOfWork` base and repository factory |
| `transactions-ambient-vs-explicit.md` | `TransactionScope`-style ambient transactions vs. an explicit transaction the unit of work opens/commits itself; rollback on partial failure |
| `change-tracking-contexts-as-unit-of-work.md` | why a change-tracking data-access context (queued inserts/updates/deletes flushed by one save call) is already a unit of work |
| `testing.md` | testing code that depends on `IUnitOfWork` — fakes, verifying atomicity, the two or three most common test shapes |
| `extending.md` | adding a new repository or atomic operation without breaking existing callers |

## Scope

A design pattern, not a package — there is no version or license to pin, and no NuGet package this
skill tracks. Every mechanism described (repository coordination, transaction handling, change
tracking) is generic — apply it with whatever data-access technology a given project already uses.

Out of scope: distributed transactions across independent databases/services, and the internal
query-method design of an individual repository beyond what coordinating it under one save call
requires. See [SKILL.md](SKILL.md) for the full out-of-scope list.
