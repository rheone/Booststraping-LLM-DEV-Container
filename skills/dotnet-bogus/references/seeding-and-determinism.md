# Seeding and Determinism

## Why seed at all

Bogus generates a different value on every `Generate()` call by default, which is exactly what you
want for exploratory use (demos, prototypes, filling a dev database with varied-looking data) but is
the wrong behavior for a test that needs to assert on the exact generated values, or that needs a
failure to reproduce identically on a second run. Seeding fixes the underlying random sequence so
the same `Faker<T>` definition produces the same output every time.

## `Randomizer.Seed` — global seed

Setting the static `Randomizer.Seed` affects every `Faker`/`Faker<T>` instance created afterward in
the current process:

```csharp
Randomizer.Seed = new Random(12345);

var faker = new CustomerFaker();
var customer = faker.Generate(); // identical output every run, given the same seed
```

Set this once, early (a test assembly's global setup, or the top of a `Main`/test-collection fixture
for a seeding script) — setting it inconsistently partway through a run makes some generated data
deterministic and other data not, which defeats the purpose.

## `.UseSeed(...)` — per-instance seed

For a seed scoped to one specific `Faker<T>` instance rather than the whole process (so other
`Faker<T>` instances elsewhere in the same test run stay independently randomized), chain
`.UseSeed(...)` on that instance:

```csharp
var faker = new CustomerFaker().UseSeed(12345);
var customer = faker.Generate();
```

Prefer `.UseSeed(...)` over the global `Randomizer.Seed` whenever only one specific generator's
output needs to be reproducible — it avoids making an unrelated test's random data suddenly
deterministic as a side effect of a static field mutation that outlives the test that set it.

## What determinism guarantees, and what it doesn't

A fixed seed guarantees the *same sequence of generated values* for a *given, unchanged* `Faker<T>`
rule definition. It does not survive:

- **Adding, removing, or reordering `RuleFor` calls** — each rule consumes from the underlying
  random sequence in declaration order, so changing that order shifts every value generated after
  the change point, even though the seed itself didn't change.
- **A Bogus version upgrade that changes a data set's internal generation algorithm** — the seed
  determines *which* underlying random numbers are drawn, not what a given data set method does with
  them; an upgrade that changes, say, how `f.Person.FirstName` maps a random draw to a name can shift
  output even under an identical seed.

Treat a seeded `Faker<T>`'s output as reproducible *within* a stable version and rule definition, not
as a permanent contract you can hard-code exact expected values against across every future change —
prefer asserting on the *shape* of generated data (a non-null email, a name matching an expected
pattern) over the exact string/value a seed happens to currently produce, unless the test's specific
purpose is to pin that exact value down (see
[testing-with-bogus-generated-data.md](testing-with-bogus-generated-data.md)).

## Isolating tests from each other's seeding

Because `Randomizer.Seed` is a static, process-wide field, a test that sets it and doesn't reset it
afterward can make a *later, unrelated* test's "random" data unexpectedly deterministic (or
deterministic to a different value than that test's author assumed) if tests share a process and run
in an order where one runs after another. Reset `Randomizer.Seed` to `null` (or a fresh `new
Random()`) in teardown if a test sets it explicitly, or prefer `.UseSeed(...)` scoped to a
locally-constructed `Faker<T>` instead of touching the global field at all.
