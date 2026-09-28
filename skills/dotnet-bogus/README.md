# Bogus

Bogus is a third-party library for generating realistic-looking fake data in .NET: names,
addresses, prices, dates, and more. This skill covers building a `Faker<T>` for your own types,
choosing the right built-in data set for a field, and making generated data reproducible when a test
needs it to be.

## When to reach for it

- Generating fake instances of a domain type for a test, a database seed, or a demo, instead of
  hand-writing literal values.
- Deciding which built-in generator fits a field: a name, an address, a price, a paragraph of text.
- A test needs the same "random" data every run, which means seeding the generator deterministically.
- Generating an object graph where one generated value needs to reference or stay consistent with
  another.
- Writing an assertion against Bogus-generated data without it becoming flaky because the value
  itself is random.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing code
that generates fake data with Bogus's `Faker<T>` API. You can also invoke it directly as
`/dotnet-bogus`.

## What it covers

| Topic | Reference |
| --- | --- |
| Faker, RuleFor, Generate/Generate(count) | [references/core-concepts.md](references/core-concepts.md) |
| Person, Address, Commerce, Internet, Lorem, Date, Finance, and other data sets | [references/built-in-datasets.md](references/built-in-datasets.md) |
| Randomizer.Seed and per-Faker UseSeed for reproducibility | [references/seeding-and-determinism.md](references/seeding-and-determinism.md) |
| Generating nested and related object graphs | [references/related-object-graphs.md](references/related-object-graphs.md) |
| Writing tests that consume Bogus-generated data without flakiness | [references/testing-with-bogus-generated-data.md](references/testing-with-bogus-generated-data.md) |

## Example prompts

- "Build a `Faker<Order>` that generates a realistic customer name, address, and total."
- "Seed this Faker so the generated test data is the same every time the suite runs."
- "Generate a customer with three related orders that reference the customer's own ID."
