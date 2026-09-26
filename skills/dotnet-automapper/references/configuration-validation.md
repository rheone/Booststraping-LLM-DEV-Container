# Configuration Validation

## AssertConfigurationIsValid

`AssertConfigurationIsValid()` walks every registered `CreateMap` and checks that every
destination member is satisfiable — either matched by convention, explicitly configured
(`ForMember`, `MapFrom`, etc.), or explicitly `Ignore()`d. If any destination member on any
configured map has no way to be populated, it throws an
`AutoMapperConfigurationException` listing every offending map/member, not just the first one it
finds.

```csharp
var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
config.AssertConfigurationIsValid(); // throws AutoMapperConfigurationException if anything's unmapped
```

This is a **structural** check — it verifies every member *can* be mapped, not that the mapping
*produces correct values*. A member mapped to the wrong source property by convention (e.g. two
same-named-but-semantically-different properties) passes `AssertConfigurationIsValid()` without
complaint; only a behavioral test (see [testing.md](testing.md)) catches that class of bug.

## Why this must run somewhere before production

Because mapping configuration is normally built once at startup and then used for the lifetime of
the process, a misconfigured map (a renamed destination property with no corresponding source
member, a newly-added required DTO field nobody wired up) will not surface until the very first
request that happens to hit that particular map — potentially well after deployment, and
potentially only for a rarely-hit code path. Running `AssertConfigurationIsValid()` explicitly,
rather than only implicitly discovering breakage at first use, converts that into a fast,
deterministic failure.

The two common places to run it:

- **In a unit/integration test** that builds the full `MapperConfiguration` used by the app and
  asserts on it — this is the more common and generally preferred approach, since it fails a CI
  build rather than an application startup, and keeps the check version-controlled alongside the
  profiles it validates. See [testing.md](testing.md) for the concrete test shape.
- **At application startup**, immediately after building `MapperConfiguration` (e.g. in a
  bootstrapping method), so a misconfiguration fails fast on deploy instead of on first request.
  This is a reasonable belt-and-suspenders addition alongside the test, not a replacement for it —
  a startup-only check still means the failure surfaces at deploy time in every environment
  (including production) rather than at CI time.

## Scoping validation to specific profiles or type maps

`AssertConfigurationIsValid` has overloads/parameters to scope the check (e.g. by profile name),
which is occasionally useful when incrementally adopting AutoMapper in a large codebase where not
every map is expected to be fully configured yet. Prefer validating everything unscoped once the
codebase is past an incremental-adoption phase — a partially-scoped validation check that never
gets widened is an easy way to leave real gaps unguarded indefinitely.

## What it does not catch

- Runtime null-reference issues from a `MapFrom` expression that dereferences something that can
  actually be null at runtime (the expression compiles and is "configured," but can still throw).
- Logically wrong-but-structurally-valid mappings (member A mapped to the wrong member B, both of
  which exist and type-match).
- Anything about `ProjectTo` query-translation failures specifically — those surface at query
  execution time against the actual provider, not at `AssertConfigurationIsValid()` time.

For all of these, `AssertConfigurationIsValid()` is necessary but not sufficient — pair it with
behavioral tests on the mappings that matter (see [testing.md](testing.md)).
