# C# Specification Pattern

Guidance on the Specification design pattern in C# — encapsulating a reusable, composable query or
business-rule predicate behind an `ISpecification<T>` interface instead of scattering the same
condition across multiple call sites. The routing table (by situation) is in [SKILL.md](SKILL.md).

**`references/`** — one file per concern/topic, not per package version — Specification is a design
pattern with nothing to version-pin

| File | Covers |
| --- | --- |
| `core-concept-and-interface.md` | the problem (duplicated/scattered predicates), the `ISpecification<T>` shape, `IsSatisfiedBy` |
| `generic-specification-base.md` | a reusable `Specification<T>` abstract base implementing composition operators |
| `composition-and-or-not.md` | `And`/`Or`/`Not` combinators producing composite specifications |
| `expression-conversion-and-iqueryable.md` | converting a specification to `Expression<Func<T, bool>>` for provider-translatable `IQueryable<T>` queries |
| `repository-integration.md` | narrowing a repository's method surface to specification-accepting methods instead of one bespoke method per query |
| `testing.md` | testing an individual specification and a composed tree of specifications |
| `extending.md` | adding a new specification without touching existing ones |

## Scope

A design pattern, not a package — there is no version or license to pin, and no NuGet package this
skill tracks. Every mechanism described (composition, expression conversion, repository
integration) is generic — apply it with whatever query provider or persistence technology a given
project already uses.

Out of scope: general validation-framework design beyond a specification's boolean predicate, and
repository concerns unrelated to accepting specifications. See [SKILL.md](SKILL.md) for the full
out-of-scope list.
