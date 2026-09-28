# Related and Nested Object Graphs

## Nesting one `Faker<T>` inside another

When a type has a property that's itself a complex object, generate it by invoking another
`Faker<T>` from within the outer type's `RuleFor` rule:

```csharp
public class AddressFaker : Faker<Address>
{
    public AddressFaker()
    {
        RuleFor(a => a.Street, f => f.Address.StreetAddress());
        RuleFor(a => a.City, f => f.Address.City());
        RuleFor(a => a.ZipCode, f => f.Address.ZipCode());
    }
}

public class CustomerFaker : Faker<Customer>
{
    private static readonly AddressFaker AddressFaker = new();

    public CustomerFaker()
    {
        RuleFor(c => c.FullName, f => f.Person.FullName);
        RuleFor(c => c.HomeAddress, f => AddressFaker.Generate());
    }
}
```

Reusing one `AddressFaker` instance across many `Customer` generations is fine — each `Generate()`
call produces an independently randomized `Address`, so sharing the *faker definition* does not
cause generated customers to share the *same* address instance or values.

## Generating a collection of related objects

For a one-to-many relationship (an order with several line items), generate the collection inside
the parent's `RuleFor` using the child `Faker<T>`'s `Generate(count)`:

```csharp
public class OrderFaker : Faker<Order>
{
    private static readonly OrderLineFaker LineFaker = new();

    public OrderFaker()
    {
        RuleFor(o => o.Id, f => f.Random.Guid());
        RuleFor(o => o.Lines, f => LineFaker.Generate(f.Random.Int(1, 5)));
    }
}
```

Passing a randomized count (`f.Random.Int(1, 5)` above) rather than a fixed number produces more
realistic variation in collection size across generated instances, which is usually closer to real
production data than every generated order having exactly the same number of line items.

## Deriving a child property from its parent

When a child object needs a value that depends on the parent being constructed (a line item's
`OrderId` matching the order it belongs to), generate the children *after* the parent object exists
rather than trying to inject the parent's ID into an independently-invoked child `Faker<T>`. The
`FinishWith` hook is the place to do this:

```csharp
public class OrderFaker : Faker<Order>
{
    private static readonly OrderLineFaker LineFaker = new();

    public OrderFaker()
    {
        RuleFor(o => o.Id, f => f.Random.Guid());
        FinishWith((f, order) =>
        {
            order.Lines = LineFaker.Generate(3);
            foreach (var line in order.Lines)
            {
                line.OrderId = order.Id;
            }
        });
    }
}
```

`FinishWith` runs after every `RuleFor` rule has populated the instance, so `order.Id` is guaranteed
already set by the time the callback runs — this is the reliable way to stamp a parent's generated
identity onto its children, rather than trying to coordinate two independent `Faker<T>` instances
around a value neither one owns until generation time.

## Keeping related fakers consistent

Define one `Faker<T>` per type and reuse that single definition everywhere that type needs
generating, rather than redefining ad hoc rules inline at each call site — this is what keeps a
"customer" generated for an order test and a "customer" generated for a billing test looking like
the same kind of realistic data, and it means a field added to `Customer` only needs a `RuleFor` rule
added in one place.
