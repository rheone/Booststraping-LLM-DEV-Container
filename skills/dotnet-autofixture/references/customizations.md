# Customizations

## Why customize a `Fixture`

Reflection-based generation gets a plain data class right by default, but some types need help: an
interface or abstract class has nothing concrete to construct, a type has a constructor AutoFixture
shouldn't use, or a suite-wide convention (every `Email` must look like a real email address) needs
to apply everywhere that type appears, not just at one `Create<T>()` call site.

## `Fixture.Customize<T>` for a single type

```csharp
fixture.Customize<Order>(composer => composer
    .With(o => o.Status, OrderStatus.Draft)
    .Without(o => o.ShippedAt));
```

This is the same fluent composer `Build<T>()` exposes (see `references/core-concepts.md`), applied
as a standing rule for every subsequent `Create<Order>()`/`CreateMany<Order>()` call on this
`Fixture` instance, not just one call.

## `ICustomization` for a reusable, named unit of configuration

A single `.Customize<T>()` call is fine inline; a customization that's applied across many test
classes, or that bundles several related configuration steps together, belongs in its own
`ICustomization`:

```csharp
public class OrderCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Order>(composer => composer
            .With(o => o.Status, OrderStatus.Draft)
            .Without(o => o.ShippedAt));

        fixture.Customize<Money>(composer => composer
            .FromFactory(() => new Money(fixture.Create<decimal>() % 10_000m, "USD")));
    }
}
```

Apply it with `fixture.Customize(new OrderCustomization())` — `IFixture.Customize(ICustomization)`
(distinct from the generic `Customize<T>`) runs the customization's `Customize` method against the
fixture immediately.

```csharp
var fixture = new Fixture().Customize(new OrderCustomization());
```

## `FromFactory` for full construction control

When a type needs to be built by calling something other than its own constructor with generated
arguments (a static factory method, validation logic that would reject arbitrary generated values),
`FromFactory` replaces AutoFixture's normal construction strategy for that type entirely:

```csharp
fixture.Customize<EmailAddress>(composer => composer
    .FromFactory(() => EmailAddress.Parse($"{fixture.Create<string>()}@example.com")));
```

## Composing multiple customizations

`ICustomization` instances compose by applying each in sequence — later customizations can override
members earlier ones configured for the same type, so order matters when two customizations touch
the same type. Keep each `ICustomization` focused on one coherent piece of configuration (one type,
or one small related group) so composing several of them stays predictable rather than needing to
reason about a large monolithic customization's internal ordering.

## Suite-wide customization

For configuration that every test in a project should get, wrap fixture creation in a shared helper
(a base test class, or a static factory method) that applies the same set of customizations, rather
than repeating `.Customize(new XCustomization())` at every test's `new Fixture()` call site.
