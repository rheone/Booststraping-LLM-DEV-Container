# Specification Pattern

You encapsulate a reusable, composable query or business-rule predicate behind an
`ISpecification<T>` interface instead of scattering the same condition across multiple call sites.
It covers the core interface, And/Or/Not composition into composite specifications, converting a
specification into an `Expression<Func<T, bool>>` for `IQueryable<T>` use, and narrowing a
repository's method surface around specifications.

## When to reach for it

- The same business rule or query condition is duplicated across several places in the codebase
  and you want a single, named, testable object for it instead.
- You need to combine several rules into one composite check (all of these, any of these, none of
  these) without hand-rolling the boolean logic each time.
- You're converting a business rule into a form a LINQ provider can translate, rather than only
  evaluating it against objects already loaded into memory.
- A repository or query surface has accumulated many bespoke, single-purpose methods and you want
  to collapse them into a small set of specification-accepting methods.

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
designing a reusable predicate or combining business rules. You can also invoke it directly as
`/csharp-specification-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The problem and the core `ISpecification<T>` interface | [references/core-concept-and-interface.md](references/core-concept-and-interface.md) |
| A reusable, type-parameterized specification base | [references/generic-specification-base.md](references/generic-specification-base.md) |
| Combining specifications with And/Or/Not | [references/composition-and-or-not.md](references/composition-and-or-not.md) |
| Converting a specification to `Expression<Func<T, bool>>` | [references/expression-conversion-and-iqueryable.md](references/expression-conversion-and-iqueryable.md) |
| Narrowing a repository's method surface with specifications | [references/repository-integration.md](references/repository-integration.md) |
| Testing a specification or a composed tree of specifications | [references/testing.md](references/testing.md) |
| Adding a new specification without touching existing ones | [references/extending.md](references/extending.md) |

## Example prompts

- "I have the same `customer.Status == Active && customer.Orders.Any()` check copy-pasted in three
  places. How do I turn that into a specification?"
- "How do I combine an `IsActiveSpecification` and an `IsEligibleForDiscountSpecification` into one
  check?"
- "I need this specification to work as a `Where` clause against `IQueryable<Order>`, not just
  against an in-memory list. How do I convert it to an expression?"
