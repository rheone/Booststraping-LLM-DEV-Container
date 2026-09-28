---
name: dotnet-bogus
description: Guidance on Bogus, a third-party fake test-data generator for C#/.NET (current stable release 35.6.5). Covers Faker<T> setup and RuleFor conventions, built-in data set methods (Person, Address, Commerce, Internet, Lorem, Date, Finance, and others), seeding via Faker.Seed/Randomizer.Seed for deterministic output, and generating realistic related/nested object graphs. Use when writing, reviewing, or debugging code that generates fake data for tests, database seeding, demos, or UI prototypes using Bogus's Faker<T> API.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Bogus

Guidance on Bogus, a third-party fake test-data generator for .NET. Current stable release as of
this writing: **35.6.5**. Organized by concern/topic, not by Bogus version — its core
`Faker<T>`/`RuleFor` API has been stable across its 30.x–35.x lines, so each reference file notes a
version-introduced fact inline rather than splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Generating fake instances of your own type | `Faker<T>`, `RuleFor`, `Generate()`/`Generate(count)` | [references/core-concepts.md](references/core-concepts.md) |
| Picking which built-in generator fits a field | `Person`, `Address`, `Commerce`, `Internet`, `Lorem`, `Date`, `Finance`, and other data sets | [references/built-in-datasets.md](references/built-in-datasets.md) |
| Making generated data reproducible across runs | `Randomizer.Seed`, per-`Faker<T>` `.UseSeed(...)` | [references/seeding-and-determinism.md](references/seeding-and-determinism.md) |
| Generating objects that reference or contain other generated objects | Nested `Faker<T>`, `RuleFor` referencing sibling properties, `FinishWith`, generating collections | [references/related-object-graphs.md](references/related-object-graphs.md) |
| Writing tests that consume Bogus-generated data | Avoiding flaky assertions against random values, snapshot-style tests, asserting on shape vs. exact values | [references/testing-with-bogus-generated-data.md](references/testing-with-bogus-generated-data.md) |

## Quick start

```csharp
public class OrderFaker : Faker<Order>
{
    public OrderFaker()
    {
        RuleFor(o => o.Id, f => f.Random.Guid());
        RuleFor(o => o.CustomerName, f => f.Person.FullName);
        RuleFor(o => o.Total, f => f.Finance.Amount(10, 500));
        RuleFor(o => o.PlacedOn, f => f.Date.Past(1));
    }
}

var faker = new OrderFaker();
var order = faker.Generate();
var orders = faker.Generate(50);
```

Each `RuleFor` call wires one property to a generator function receiving a `Faker` instance (`f`)
that exposes every built-in data set (`f.Person`, `f.Address`, `f.Commerce`, and the rest); calling
`Generate()` runs every configured rule and returns a fully populated instance.

## Out of scope

- Bogus Premium's additional datasets and the `Bogus.Tools.Analyzer` package — a separate product
  sold independently of the core `Bogus` package this skill covers.
- Persisting generated data to a specific database or ORM — Bogus produces in-memory object graphs;
  what you do with them afterward (bulk insert, `SaveChanges`, an HTTP request body) is outside its
  own API surface.
- General property-based/fuzz testing frameworks — Bogus generates realistic-looking data for known
  shapes, which is a different technique from generating adversarial or edge-case inputs to find
  bugs.
