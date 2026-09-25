# Pattern Matching as a Test-Authoring Tool

This file is about *using pattern matching to write clearer test assertions and conditionals* —
one-expression shape/content assertions on a returned object, list-pattern assertions on a
collection result, `is` as a cleaner alternative to multiple casts in test setup — not about
testing this skill's own pattern-matching syntax examples. It assumes the property, positional, and
list pattern syntax from
[csharp8-switch-expressions-and-recursive-patterns.md](../references/csharp8-switch-expressions-and-recursive-patterns.md)
and
[csharp11-list-and-slice-patterns.md](../references/csharp11-list-and-slice-patterns.md).

## Basic: asserting an object's shape in one expression

```csharp
[Fact]
public void CreateOrder_Test_ReturnsPendingOrderWithExpectedTotal()
{
    Order order = orderService.CreateOrder(customerId: 42, items: threeWidgets);

    Assert.True(
        order is { Status: OrderStatus.Pending, Total: > 0m, Customer.Id: 42 },
        $"Unexpected order shape: {order}");
}
```

A single property pattern checks status, a relational constraint on total, and a nested customer
ID in one `Assert.True`, instead of three separate `Assert.Equal`/`Assert.True` calls — the pattern
itself documents the expected shape, and a failure's assertion message still needs the `order`
dump (a boolean `Assert.True` doesn't show *which* part of the pattern failed the way per-field
assertions would, so keep the diagnostic message next to it).

## Basic: `is` replacing a chain of casts in test setup

```csharp
[Fact]
public void ProcessPayment_Test_RoutesCardPaymentsToCardProcessor()
{
    PaymentResult result = paymentService.Process(cardPaymentRequest);

    Assert.True(result is CardPaymentResult { AuthorizationCode: { Length: > 0 } });
}
```

```csharp
// Instead of:
Assert.IsType<CardPaymentResult>(result);
var cardResult = (CardPaymentResult)result;
Assert.False(string.IsNullOrEmpty(cardResult.AuthorizationCode));
```

`result is CardPaymentResult { AuthorizationCode: { Length: > 0 } }` folds a type check, a cast,
and a non-empty-string check into one boolean — useful anywhere a test would otherwise need
`Assert.IsType` followed by a cast to reach a derived type's members.

## Advanced: property-pattern assertions with an xUnit assertion library

```csharp
[Fact]
public void GetCustomer_Test_ReturnsExpectedCustomer()
{
    Customer customer = customerRepository.GetById(42);

    customer.Should().Match<Customer>(c => c is
    {
        Id: 42,
        Name: "Ada Lovelace",
        Address.State: "CA",
        Orders.Count: > 0,
    });
}
```

FluentAssertions' `Should().Match(predicate)` accepts any boolean-returning lambda, so a pattern
inside it reads as a declarative shape assertion rather than a chain of `.Should().Be(...)` calls —
particularly useful when several unrelated properties need checking together and per-property
assertions would otherwise scatter across several lines with less obvious grouping.

## Advanced: list-pattern assertions on collection results

```csharp
[Fact]
public void GetTopScores_Test_ReturnsExactlyThreeDescendingScores()
{
    int[] topScores = leaderboard.GetTopScores(count: 3);

    Assert.True(
        topScores is [var first, var second, var third] && first >= second && second >= third,
        $"Expected exactly 3 descending scores, got [{string.Join(", ", topScores)}]");
}

[Fact]
public void ParseLog_Test_FirstLineIsHeaderRestAreEntries()
{
    string[] lines = logParser.Parse(rawLog);

    Assert.True(lines is ["HEADER", .. { Length: > 0 } entries] && entries.All(e => e.StartsWith("ENTRY")));
}
```

A list pattern asserts both the *count* and the *per-element shape* of a collection result in one
expression — `[var first, var second, var third]` simultaneously asserts "exactly 3 elements" and
captures each one, which otherwise takes a separate `Assert.Equal(3, topScores.Length)` plus
indexed access per element. The slice-with-property-pattern form (`.. { Length: > 0 } entries`)
asserts "at least one entry after the header" without a separate length check.

## Advanced: `switch` expression building expected test data from input variations

```csharp
[Theory]
[InlineData(OrderStatus.Pending, 0.0)]
[InlineData(OrderStatus.Shipped, 5.99)]
[InlineData(OrderStatus.Cancelled, 0.0)]
public void GetShippingCost_Test_MatchesStatus(OrderStatus status, double expectedCost)
{
    decimal expected = status switch
    {
        OrderStatus.Pending or OrderStatus.Cancelled => 0m,
        OrderStatus.Shipped => 5.99m,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    Assert.Equal(expected, orderService.GetShippingCost(new Order { Status = status }));
}
```

Computing the *expected* value with a switch expression that mirrors the production logic's own
branching documents the business rule the test is verifying, the same way a LINQ-built expected
collection documents a filtering rule — the discard arm's `throw` also means an unhandled
`OrderStatus` added later fails the test loudly (a new `[InlineData]` case is still needed to
exercise it) instead of silently computing a wrong expected value.

## Fallback

Property and positional patterns need C# 8.0+; below that, replace a shape assertion with separate
`Assert.Equal`/`Assert.True` calls per field, as shown in the "instead of" block above — every
version of this skill's target frameworks supports that unconditionally. List-pattern assertions
need C# 11.0+ and a .NET Standard 2.1+/.NET Core 3.0+ test target; below that, assert `Length`/
`Count` and indexed elements separately, per
[csharp11-list-and-slice-patterns.md#fallback](../references/csharp11-list-and-slice-patterns.md#fallback).
Relational/logical patterns used inside these assertions need C# 9.0+; below that, use `&&`/`||`
inside the `Assert.True` predicate instead of inside the pattern.
