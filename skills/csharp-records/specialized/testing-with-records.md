# Records as a Test-Authoring Tool

This file is about *using records to build test fixtures and assertions* — value equality that makes
assertions trivial, `with`-expressions that build test-case variations from a shared base, and
immutable test data that can't be accidentally mutated between arrange and assert — not about testing
this skill's own record syntax examples. See
[csharp9-record-fundamentals.md](../references/csharp9-record-fundamentals.md) for the underlying
syntax these patterns build on.

## Basic: value equality makes assertions trivial with no custom comparer

```csharp
public record OrderDto(string Id, decimal Total, string CustomerEmail);

[Fact]
public void GetOrder_Test_ReturnsExpectedOrder()
{
    OrderDto expected = new("ORD-001", 49.99m, "nancy@example.com");

    OrderDto actual = orderService.GetOrder("ORD-001");

    Assert.Equal(expected, actual); // structural equality -- no custom IEqualityComparer needed
}
```

Because a record's `Equals` is synthesized from its declared members, `Assert.Equal` (or a fluent
`actual.Should().Be(expected)` from an assertion library) works correctly out of the box for any DTO
or fixture declared as a record — no hand-written `IEquatable<T>` implementation, no reflection-based
deep-equality helper, and no risk of the comparer silently missing a field the way a
[hand-written `Equals`](../references/pre-csharp9-manual-value-types.md) can.

## Basic: `with`-expressions build test-case variations from one base fixture

```csharp
public record UserRegistration(string Email, string Password, int Age, bool AcceptedTerms);

public static class RegistrationFixtures
{
    public static readonly UserRegistration Valid =
        new("nancy@example.com", "Str0ngP@ss!", Age: 30, AcceptedTerms: true);
}

[Theory]
[MemberData(nameof(InvalidRegistrations))]
public void Register_Test_RejectsInvalidRegistration(UserRegistration registration, string expectedError)
{
    var result = registrationService.Register(registration);

    Assert.False(result.Success);
    Assert.Equal(expectedError, result.Error);
}

public static IEnumerable<object[]> InvalidRegistrations()
{
    yield return new object[] { RegistrationFixtures.Valid with { Email = "" }, "Email is required" };
    yield return new object[] { RegistrationFixtures.Valid with { Age = 12 }, "Must be 18 or older" };
    yield return new object[] { RegistrationFixtures.Valid with { AcceptedTerms = false }, "Terms must be accepted" };
}
```

Each invalid case is a one-line `with`-expression naming only the field that makes it invalid — the
reader sees exactly what varies per case without re-reading every field's value, and adding a new
field to `UserRegistration` later doesn't require touching every existing test case the way adding a
constructor parameter to a hand-written fixture class would.

## Advanced: immutable test data that can't drift between arrange and assert

```csharp
public record InventorySnapshot(string Sku, int QuantityOnHand);

[Fact]
public void ReserveStock_Test_DoesNotMutateOriginalSnapshot()
{
    InventorySnapshot before = new("SKU-100", QuantityOnHand: 50);

    ReservationResult result = inventoryService.Reserve(before, quantity: 10);

    // before is guaranteed unchanged -- it's an init-only record, not a mutable entity the
    // production code could have silently modified in place between arrange and assert.
    Assert.Equal(50, before.QuantityOnHand);
    Assert.Equal(40, result.RemainingSnapshot.QuantityOnHand);
}
```

A mutable fixture class risks a specific, hard-to-spot failure mode: production code that mutates the
object passed into it, which then makes the "before" value in the assert phase silently reflect
post-action state instead of the original arrange-phase value the test meant to check against. An
init-only record structurally rules this out — there is no setter for the production code under test
to call, so `before` is guaranteed to still hold what the test arranged, and a test that would have
passed for the wrong reason (comparing mutated `before` against itself) fails loudly instead.

## Advanced: a generic record wrapper for parameterized fixtures

```csharp
public record TestCase<TInput, TExpected>(string Description, TInput Input, TExpected Expected);

public static IEnumerable<object[]> DiscountCases() =>
[
    [new TestCase<decimal, decimal>("no discount under $50", 49.99m, 49.99m)],
    [new TestCase<decimal, decimal>("10% off at $50 exactly", 50.00m, 45.00m)],
    [new TestCase<decimal, decimal>("10% off above $50", 100.00m, 90.00m)],
];

[Theory]
[MemberData(nameof(DiscountCases))]
public void ApplyDiscount_Test_MatchesExpectedTotal(TestCase<decimal, decimal> testCase)
{
    decimal actual = pricingService.ApplyDiscount(testCase.Input);

    Assert.Equal(testCase.Expected, actual); // testCase.Description shows in failure output via ToString
}
```

A generic positional record reused across every parameterized test in a suite gives each case a
self-describing `Description` alongside its input/expected pair, and because it's a record, a failing
`Assert.Equal` or test-runner failure message that includes `testCase` prints its compiler-generated
`ToString()` — `TestCase { Description = ..., Input = ..., Expected = ... }` — with no extra work.

## Fallback

`record`-based fixtures and `with`-based case variation need at minimum
[C# 9](../references/csharp9-record-fundamentals.md). On an older target, fall back to a
[hand-written immutable class](../references/pre-csharp9-manual-value-types.md) with a hand-written
`Equals`/`GetHashCode` for the assertion-equality benefit, and a hand-written "copy with one field
changed" static method per fixture family in place of `with`, accepting that both will drift out of
sync with the type's member list unless maintained carefully by hand.
