---
name: csharp-specification-pattern
description: Guidance on the Specification design pattern in C# — an ISpecification<T> interface encapsulating a reusable, composable query or business-rule predicate, combining specifications with And/Or/Not composition into composite specifications, converting a specification to an Expression<Func<T, bool>> for use with IQueryable<T>-based queries versus evaluating it in memory against an already-loaded object with Func<T, bool>, and using specifications to narrow a repository's method surface to a small set of specification-accepting methods instead of one bespoke query method per use case. Use when designing reusable query or validation predicates, combining multiple business rules into one composite check, converting a business rule into a LINQ-provider-translatable expression, or reviewing a repository interface that has accumulated many single-purpose query methods. Described generically as a design concept — does not name or require any specific ORM or persistence library.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Specification Pattern

Specification is a **design pattern**, not a package — there is nothing to install and no version
to pin. This skill documents the pattern itself: an `ISpecification<T>` abstraction for a reusable
predicate, composing specifications with And/Or/Not, converting one to an
`Expression<Func<T, bool>>` a query provider can translate, and using specifications to keep a
repository's method surface small.

Everything below is described **generically**: "a query provider," "a repository" — never naming a
specific ORM or persistence library — because the pattern's shape doesn't depend on which one a
given project uses.

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Understanding what problem Specification solves and its core interface | [references/core-concept-and-interface.md](references/core-concept-and-interface.md) |
| Writing a reusable, type-parameterized specification base | [references/generic-specification-base.md](references/generic-specification-base.md) |
| Combining specifications with And/Or/Not into a composite | [references/composition-and-or-not.md](references/composition-and-or-not.md) |
| Converting a specification to `Expression<Func<T, bool>>` for `IQueryable<T>` | [references/expression-conversion-and-iqueryable.md](references/expression-conversion-and-iqueryable.md) |
| Narrowing a repository's method surface with specifications | [references/repository-integration.md](references/repository-integration.md) |
| Testing a specification or a composite of specifications | [references/testing.md](references/testing.md) |
| Adding a new specification without changing existing ones | [references/extending.md](references/extending.md) |

## Quick start

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}

public sealed class ActiveCustomerSpecification : ISpecification<Customer>
{
    public bool IsSatisfiedBy(Customer candidate) => candidate.Status == CustomerStatus.Active;
}
```

A caller uses a specification wherever a boolean rule is needed — filtering an in-memory sequence,
validating an entity, or (with the expression form) building a query:

```csharp
ISpecification<Customer> isActive = new ActiveCustomerSpecification();

bool eligible = isActive.IsSatisfiedBy(customer);
List<Customer> activeCustomers = allCustomers.Where(isActive.IsSatisfiedBy).ToList();
```

Start with [references/core-concept-and-interface.md](references/core-concept-and-interface.md) for
the interface's full shape, then
[references/composition-and-or-not.md](references/composition-and-or-not.md) to combine rules
instead of duplicating them.

## Out of scope

- Naming or depending on any specific ORM or LINQ provider. The expression-conversion guidance
  applies to any `IQueryable<T>` source; apply it with whatever query provider a given project
  already uses.
- General validation-framework design (rule severity levels, localized error messages, a fluent
  validation DSL) — this skill covers a specification as a reusable predicate, not a full
  validation-result reporting system built on top of one.
- General repository design beyond how it accepts specifications — the rest of a repository's
  contract (unit-of-work coordination, persistence mechanics) is a separate concern.
