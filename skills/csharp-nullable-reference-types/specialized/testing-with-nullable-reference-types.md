# Nullable Reference Types as a Concern When Writing Tests

This file is about how nullable-reference-type warnings show up *while authoring tests* — whether
an assertion library's `NotNull`-style helper actually narrows the compiler's flow state the way a
plain `if (x != null)` does, and how to build valid non-null test fixtures without littering test
code in `!`. It is not a tutorial on testing this skill's own NRT syntax examples. See
[csharp8-nullable-reference-types.md](../references/csharp8-nullable-reference-types.md) for the
underlying flow-analysis rules these patterns build on, and
[null-forgiving-operator-pitfalls.md](null-forgiving-operator-pitfalls.md) for when reaching for `!`
in a test is (and isn't) reasonable.

## Basic: not every `Assert.NotNull`-style helper narrows the compiler's flow state

```csharp
#nullable enable

// xUnit: Assert.NotNull is annotated so the compiler treats its argument as non-null
// after a successful call -- no warning on the next line.
[Fact]
public void GetOrder_Test_ReturnsOrderWithTotal()
{
    Order? order = orderService.GetOrder("ORD-1001");

    Assert.NotNull(order);
    Assert.Equal(49.99m, order.Total); // no CS8602 warning: xUnit's NotNull is compiler-recognized
}
```

```csharp
#nullable enable

// NUnit's constraint-based Assert.That(x, Is.Not.Null) does NOT narrow -- the constraint
// object has no way to carry a [NotNull]-style annotation back to the compiler's flow state.
[Test]
public void GetOrder_Test_ReturnsOrderWithTotal_NUnit()
{
    Order? order = orderService.GetOrder("ORD-1001");

    Assert.That(order, Is.Not.Null);
    Assert.That(order!.Total, Is.EqualTo(49.99m)); // still needs ! (or NUnit's own analyzer suppression)
}
```

This is a real, version-dependent difference between assertion libraries, not a general rule about
"assertion frameworks" — xUnit's `Assert.NotNull(T? value)` overload carries a `[NotNull]` attribute
(from the same attribute family the C# 8.0 nullable-analysis release shipped) recognized by the
compiler's flow analysis, so a successful call narrows `order` for every line after it in the same
method, the same way `if (order is null) throw` would. NUnit's `Assert.That(actual, constraint)`
API can't carry that same per-call narrowing information through a general-purpose constraint
object, so it needs either `!` after the assertion or NUnit's own analyzer (which suppresses the
resulting warning rather than eliminating it through genuine narrowing) to avoid a false-positive
warning on the next line.

## Basic: builder/fixture patterns that construct valid non-null test objects without `!`

```csharp
#nullable enable

public class OrderBuilder
{
    private string _customerName = "Default Customer";
    private string _orderId = "ORD-DEFAULT";
    private decimal _total = 0m;

    public OrderBuilder WithCustomer(string customerName)
    {
        _customerName = customerName;
        return this;
    }

    public OrderBuilder WithTotal(decimal total)
    {
        _total = total;
        return this;
    }

    public Order Build() => new() { CustomerName = _customerName, OrderId = _orderId, Total = _total };
}

[Fact]
public void ApplyDiscount_Test_ReducesTotal()
{
    Order order = new OrderBuilder().WithTotal(100m).Build(); // every field has a real, non-null default

    Order discounted = pricingService.ApplyDiscount(order);

    Assert.Equal(90m, discounted.Total);
}
```

A builder with sensible non-null defaults for every field means test authors never need `!` to
satisfy the compiler while constructing a test object — every field the builder produces is a real
value, not a placeholder the author is asserting-away a warning about. This avoids the common
anti-pattern of a test fixture built with `new Order()!` or field-by-field `= null!;` assignments
purely to get past the compiler, which reintroduces exactly the runtime null risk NRT exists to
catch, inside the tests meant to guard against it.

## Advanced: a `TryXxx`-shaped test helper that narrows like the production pattern it mirrors

```csharp
#nullable enable

public static class AssertExtensions
{
    public static T AssertNotNull<T>([NotNull] T? value, string? because = null) where T : class
    {
        if (value is null)
        {
            throw new InvalidOperationException(because ?? $"Expected a non-null {typeof(T).Name}.");
        }

        return value;
    }
}

[Fact]
public void GetOrder_Test_ReturnsOrderWithTotal_CustomHelper()
{
    Order order = AssertExtensions.AssertNotNull(orderService.GetOrder("ORD-1001"), "order must exist");

    Assert.Equal(49.99m, order.Total); // order is Order, not Order? -- no narrowing needed at all
}
```

A custom `[NotNull]`-annotated helper (the same attribute a `TryXxx` production method would use)
both narrows for the compiler and returns a non-nullable `T` directly, so the caller's local
variable never has to be declared nullable in the first place — useful when a project uses an
assertion library whose own `NotNull`-style helper doesn't carry the annotation, standardizing one
narrowing helper across all test projects instead of relying on library-specific behavior that may
change between versions.

## Fallback

All of this needs [C# 8.0](../references/csharp8-nullable-reference-types.md) to have any warnings
to narrow away from. On an older target, or in test files under `#nullable disable`, there's no
nullable-warning friction to manage in the first place — the tradeoff is the ordinary
[pre-C#8](../references/pre-csharp8-nullable-oblivious.md) one: no false positives to suppress, but
also no compiler help catching a genuinely null test fixture value before it causes a confusing
test failure elsewhere in the same test run.
