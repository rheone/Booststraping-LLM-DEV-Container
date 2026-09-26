# Common pitfalls

## Uncontrolled randomness making assertions flaky or meaningless

Every value AutoFixture generates that a test doesn't explicitly pin is arbitrary — not
`Random`-seeded arbitrary in a way that's reproducible across runs by default, but "whatever the
generation strategy produced this time" arbitrary. Two failure shapes follow from this:

- **An assertion accidentally depends on a generated value's shape.** A test that asserts
  `result.Length == 32` because that happened to be the length of an auto-generated string the
  first time the test was written will pass by coincidence and fail the moment anything upstream
  changes how that string is generated — the assertion was never actually testing string length as
  a real invariant, just restating what `Create<string>()` happened to produce.
- **A test passes for the wrong reason.** If a test's arrangement auto-generates a value for a
  parameter the test *should* be pinning to something meaningful (a boundary value, a specific
  enum case the logic under test branches on), the test can pass without ever exercising the code
  path it was meant to cover, because the generated value never happened to land on that path.

The fix in both cases is the same: pin any value a test's assertions actually depend on via
`Build<T>().With(...)` or `[InlineAutoData]`-style explicit parameters (see
`references/theory-attributes.md`), and let AutoFixture generate only the values that are genuinely
irrelevant to what the test is checking. Treat "this test only passes because of what a generated
value happened to be" as a bug in the test, not a quirk to route around with a wider assertion.

## Circular references

A type whose object graph refers back to itself (a parent that holds a collection of children, each
of which holds a reference back to the parent) sends naive recursive generation into infinite
recursion. AutoFixture's default behavior throws an `ObjectCreationException` reporting the circular
path once recursion is detected, rather than looping forever or silently truncating the graph.

Handle it with an explicit customization for the type that closes the cycle deliberately:

```csharp
fixture.Customize<ParentEntity>(composer => composer
    .Without(p => p.Children)); // build children separately and wire the back-reference by hand
```

or, for a cycle that's acceptable to simply stop rather than needing manual wiring, configure a
recursion behavior on the fixture that omits the recursive member instead of throwing:

```csharp
fixture.Behaviors
    .OfType<ThrowingRecursionBehavior>()
    .ToList()
    .ForEach(b => fixture.Behaviors.Remove(b));
fixture.Behaviors.Add(new OmitOnRecursionBehavior());
```

`OmitOnRecursionBehavior` leaves the recursive member at its default (often `null` or empty) once a
cycle is detected instead of throwing — appropriate when the test genuinely doesn't need the
recursive member populated; inappropriate when the test's logic actually depends on that member
having a value, in which case building the cycle explicitly (as in the `Without` example) is the
correct fix instead of silently omitting data the test needs.

## Overusing `Fixture` for values a test should own explicitly

Not every value belongs behind `Create<T>()`. A value a test's name, arrangement, or assertion is
actually about (the specific quantity that should trigger a discount, the specific status a state
machine transition depends on) reads more clearly, and is more resistant to the pitfalls above, when
written as a literal or a pinned `Build<T>().With(...)` value rather than left to auto-generation —
reserve auto-generation for the surrounding object graph that exists only to satisfy a constructor,
not for the values the test is actually exercising.
