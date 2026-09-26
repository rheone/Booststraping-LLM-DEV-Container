# Nested Objects and Collections

FluentValidation composes: a validator for a child type plugs directly into the parent's
validator, so you don't re-declare the child's rules inline.

## Validating a nested object with SetValidator

```csharp
public sealed class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

public sealed class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
    }
}

public sealed class Order
{
    public Address ShippingAddress { get; set; } = new();
}

public sealed class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.ShippingAddress).SetValidator(new AddressValidator());
    }
}
```

Failures from the nested validator surface with a dotted property path
(`ShippingAddress.Street`), so a caller reading `ValidationResult.Errors` can tell exactly which
nested field failed.

Inject the child validator through the parent's constructor (via DI) rather than `new`-ing it up
inline in real code — this keeps the child validator's own dependencies (if any, e.g. an async
uniqueness check) wired correctly and keeps the parent testable in isolation from the child's
concrete implementation:

```csharp
public sealed class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator(IValidator<Address> addressValidator)
    {
        RuleFor(x => x.ShippingAddress).SetValidator(addressValidator);
    }
}
```

## Validating a collection with RuleForEach

```csharp
public sealed class Order
{
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator(IValidator<OrderLine> lineValidator)
    {
        RuleForEach(x => x.Lines).SetValidator(lineValidator);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("Order must contain at least one line.");
    }
}
```

`RuleForEach` runs the given validator (or an inline `Must`/`Custom` chain) against every element,
reporting failures with an indexed path (`Lines[2].Quantity`). Pair it with a separate `RuleFor`
on the collection property itself (as above) when the collection's *shape* — not-empty, a maximum
count — needs its own check; `RuleForEach` alone says nothing about the collection being empty,
since an empty collection has no elements to fail against.

## Excluding specific indices or filtering elements

`RuleForEach` supports the same `.When(...)` conditional chaining as `RuleFor`, scoped to each
element, and a `.Where(...)` predicate to skip elements entirely before validating the rest:

```csharp
RuleForEach(x => x.Lines)
    .Where(line => !line.IsCancelled)
    .SetValidator(lineValidator);
```

## Overriding the indexed property name

By default, failure property names use square-bracket indices (`Lines[0].Sku`). Override the
placeholder via `.OverrideIndexer(...)` when a caller needs a different key format (e.g. matching
a client-side form's field-naming convention) — this is an edge case; leave the default indexed
format unless something downstream specifically depends on a different shape.
