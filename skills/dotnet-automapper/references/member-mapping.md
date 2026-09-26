# Member Mapping: Flattening, ForMember, ForPath, ForAllMembers

## Flattening convention

AutoMapper's signature convention: it can flatten a nested object graph into a flat DTO by
matching a destination property name of the form `<NestedPropertyName><MemberName>` against a
source path `Nested.MemberName`, and also matches method calls named `Get<MemberName>()`.

```csharp
public sealed class Order
{
    public Customer Customer { get; set; }
}

public sealed class Customer
{
    public string Name { get; set; }
}

public sealed class OrderDto
{
    public string CustomerName { get; set; } // matches Order.Customer.Name by flattening convention
}

CreateMap<Order, OrderDto>(); // no ForMember needed — CustomerName resolves via flattening
```

This works recursively through however many levels the naming convention lines up. It is
convention, not magic: if the destination name doesn't match `<Path><Member>` concatenation
exactly, fall back to explicit `ForMember`/`ForPath`.

## Unflattening

The reverse direction — mapping a flat source onto a nested destination — is **not** handled by
the same automatic convention. AutoMapper does support constructing nested destination objects
from flat sources, but it generally needs either matching constructor parameters on the nested
type or explicit `ForPath` configuration to say which flat source member feeds which nested
destination path. Don't assume `ReverseMap()` on a flattening `CreateMap` fully reconstructs the
original nested shape without checking — verify with `AssertConfigurationIsValid()` (see
[configuration-validation.md](configuration-validation.md)) rather than assuming symmetry.

## ForMember

Use `ForMember` to override how one destination member is populated when the naming/flattening
convention doesn't already produce the right result:

```csharp
CreateMap<Order, OrderDto>()
    .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.LineItems.Sum(l => l.Price)))
    .ForMember(dest => dest.LegacyCode, opt => opt.Ignore());
```

Common `ForMember` option calls:

- `.MapFrom(expression)` — supply an arbitrary expression/lambda as the source value, including
  computed values that don't correspond to any single source member.
- `.MapFrom<TResolver>()` / `.MapFrom<TResolver, TSourceMember>(...)` — delegate to a custom
  `IValueResolver`/`IMemberValueResolver` (see
  [custom-resolvers-converters.md](custom-resolvers-converters.md)).
- `.Ignore()` — exclude this destination member from mapping entirely; it's left at its default
  or constructor-assigned value. Use this for members set by other means (e.g. audit fields set by
  infrastructure) so `AssertConfigurationIsValid()` doesn't flag them as unmapped.
- `.Condition(expression)` — map this member only when the condition is true (see
  [conditional-and-null-handling.md](conditional-and-null-handling.md)).
- `.NullSubstitute(value)` — supply a fallback when the resolved source value is null.

## ForPath

`ForMember` targets a single destination *property*. `ForPath` is for when the *destination* side
is a nested path (mapping into a sub-object) rather than a top-level property — the mirror image
of the flattening scenario:

```csharp
CreateMap<OrderDto, Order>()
    .ForPath(dest => dest.Customer.Name, opt => opt.MapFrom(src => src.CustomerName));
```

`ForPath` is what makes unflattening explicit and reliable when the automatic convention doesn't
reconstruct the nested graph correctly on its own.

## ForAllMembers

Applies one configuration action to every destination member of the map at once — useful for
cross-cutting rules rather than per-member ones:

```csharp
CreateMap<Order, OrderDto>()
    .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
```

A common use: skip mapping any member whose *source* value is null, so an existing destination
value survives a `Map(source, destination)` update-in-place call instead of being overwritten with
null. `ForAllMembers` runs for every member but individual `ForMember` configuration for a
specific member still takes precedence/adds to it — it does not replace member-specific rules.

## Case sensitivity and naming

Default matching is by exact name (case-sensitive). If source/destination naming conventions
differ systematically (e.g. `snake_case` source data vs. PascalCase C# properties), configure a
global source/destination naming convention on the `MapperConfiguration` rather than adding
`ForMember` per property — check the current AutoMapper docs for the naming-convention API surface
in use, since this area has shifted across major versions; the per-member escape hatches
(`ForMember`, `ForPath`) above are stable regardless of which global convention is configured.
