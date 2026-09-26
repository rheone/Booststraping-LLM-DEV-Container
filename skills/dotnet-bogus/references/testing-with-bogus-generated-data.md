# Testing With Bogus-Generated Data

## The core risk: asserting on a value you didn't fix

Bogus-generated data is randomized by default, so a test that generates an object and then asserts
on one of its randomized field's exact value is asserting against a moving target — it passes by
coincidence today and can fail tomorrow for a reason that has nothing to do with a regression in the
code under test. This is the single most common way Bogus-driven tests become flaky.

## What to assert on instead

- **Shape, not value.** Assert that a generated `Email` is non-null and contains `@`, not that it
  equals a specific string. Assert that a generated `Total` is within the range you configured
  (`f.Finance.Amount(10, 500)` → assert `>= 10 && <= 500`), not a specific number.
- **Behavior derived from the generated input, not the input itself.** If the code under test
  computes a discount from a generated `Order.Total`, assert the relationship (`discountedTotal <
  order.Total`) rather than a hard-coded expected number that depends on knowing the exact generated
  `Total`.
- **Round-trip and invariant properties.** Generate an object, pass it through the code under test
  (serialize/deserialize, save/reload, map to a DTO and back), and assert the result still satisfies
  the same invariants the input did — this style of test benefits directly from Bogus's randomized
  variety, since it exercises many different input shapes across repeated runs rather than one
  hand-picked example.

## When you do need an exact, reproducible value

Seed the `Faker<T>` (see [seeding-and-determinism.md](seeding-and-determinism.md)) and assert
against the exact value that seed is known to produce, for the narrow set of tests that specifically
need a fixed, inspectable fixture rather than realistic variety — a snapshot-style test verifying
serialization output byte-for-byte, for instance. Keep this style of test to genuine special cases;
reaching for a fixed seed on every test defeats the reason to use Bogus over hand-written fixture
objects in the first place — the variety a fresh random generation exercises on every run.

## The three most common scenarios

1. **Seeding a database for an integration test.** Generate a batch with `Faker<T>.Generate(count)`,
   insert it, and assert the system-under-test's query/aggregation behavior against *counts and
   properties derived from the generated set* (e.g. "the report totals match the sum of the
   generated orders' `Total` values"), computed from the same in-memory objects you generated rather
   than hard-coded expected numbers.
2. **Supplying a valid, complete object to a unit test that doesn't care about specific field
   values.** Use a shared `Faker<T>` to build a "valid customer" once per test, instead of
   constructing one by hand with a dozen property initializers — the test reads cleaner because
   irrelevant fields are visibly delegated to the faker rather than cluttering the arrange step with
   values the test doesn't care about.
3. **Fuzzing an edge case by generating many instances and asserting an invariant holds across all
   of them.** Generate a large batch (`Generate(1000)`), run each through the code under test, and
   assert the invariant holds for every one — this catches edge cases a single hand-picked example
   would miss, at the cost of a test that takes slightly longer to run.
