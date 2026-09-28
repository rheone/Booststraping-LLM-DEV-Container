# Core Concepts

## `Faker<T>` and `RuleFor`

You define how to generate a type by subclassing `Faker<T>` (or, for a one-off, instantiating
`new Faker<T>()` directly) and wiring each property to a generator function via `RuleFor`:

```csharp
public class CustomerFaker : Faker<Customer>
{
    public CustomerFaker()
    {
        RuleFor(c => c.Id, f => f.Random.Guid());
        RuleFor(c => c.FirstName, f => f.Person.FirstName);
        RuleFor(c => c.LastName, f => f.Person.LastName);
        RuleFor(c => c.Email, f => f.Internet.Email());
    }
}
```

`RuleFor(propertyExpression, generatorFunction)` takes a property-access expression and a delegate
receiving a `Faker` instance (conventionally named `f`), which exposes every built-in data set as a
property (`f.Person`, `f.Internet`, `f.Commerce`, and the rest — see
[built-in-datasets.md](built-in-datasets.md)). The generator function runs once per generated
instance, so two separately generated `Customer` objects get independently randomized values for
each rule.

## `RuleFor` referencing already-generated properties

The generator function overload that also receives the instance under construction lets a later
rule depend on an earlier one:

```csharp
RuleFor(c => c.FirstName, f => f.Person.FirstName);
RuleFor(c => c.LastName, f => f.Person.LastName);
RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.FirstName, c.LastName));
```

`RuleFor` rules execute in the order you declare them, so a rule that reads another property's value
(the second argument, `c` above) must be declared after the rule that sets that property — declaring
it earlier reads that property's default (unset) value instead.

## `Generate()` and `Generate(count)`

```csharp
var customer = faker.Generate();        // one instance
var customers = faker.Generate(100);    // a List<Customer> of 100 independently generated instances
```

Each call to `Generate()` (or each element within a `Generate(count)` batch) runs every configured
`RuleFor` rule fresh, producing a new randomized value per property per instance — Bogus does not
cache or reuse values across separate `Generate()` calls unless you configure a seed (see
[seeding-and-determinism.md](seeding-and-determinism.md)).

## Rules that don't fit a single `RuleFor` call

For a property that needs conditional logic, multiple sub-values, or output that depends on state
outside the built-in data sets, pass a full lambda body:

```csharp
RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>());
RuleFor(o => o.ShippingCost, (f, o) => o.Status == OrderStatus.Cancelled ? 0m : f.Finance.Amount(5, 25));
```

`f.PickRandom<TEnum>()` picks a random defined value of an enum type — the common way to populate an
enum-typed property without hand-listing its members.

## Rules for properties the default rules don't cover

Any public settable property left without a `RuleFor` call keeps its type's default value
(`null`, `0`, `false`) after `Generate()` — Bogus does not attempt to guess a rule for every property
automatically. `StrictMode(true)` (set via `.StrictMode(true)` on the `Faker<T>`) makes `Generate()`
throw at generation time if any public settable property has no configured rule, which is useful for
catching a newly added property that a `Faker<T>` definition hasn't been updated to cover.
