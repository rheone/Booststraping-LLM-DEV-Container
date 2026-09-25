# Collections and Nested Object Mapping

## Nested objects

If a `CreateMap<TSource, TDestination>` exists for a nested object's own type pair, AutoMapper
uses it automatically when mapping the containing type — no extra configuration needed at the
containing map's call site:

```csharp
CreateMap<Address, AddressDto>();
CreateMap<Customer, CustomerDto>(); // CustomerDto.Address (AddressDto) mapped via the map above
```

If the nested type pair has **no** registered `CreateMap`, AutoMapper throws at
`AssertConfigurationIsValid()` time (or at map time if validation wasn't run) rather than silently
skipping the member — register every nested type pair that needs mapping, including into and out
of DTOs used only as list items.

## Collections

AutoMapper maps collections element-by-element using whatever `CreateMap` is registered for the
element types, and supports converting between most standard collection shapes without extra
configuration once the element type pair is mapped:

```csharp
CreateMap<OrderLine, OrderLineDto>();
CreateMap<Order, OrderDto>(); // Order.Lines (List<OrderLine>) -> OrderDto.Lines (List<OrderLineDto>) "just works"
```

This works across `List<T>`, arrays, `IEnumerable<T>`, `ICollection<T>`, and similar BCL
collection interfaces/types on both sides, including converting between different collection
*kinds* (e.g. `List<OrderLine>` source to `OrderLineDto[]` destination) as long as the element
type pair itself is mapped.

### Custom/non-standard collection types

For a destination collection type that isn't one of the standard shapes AutoMapper recognizes
(e.g. a custom collection wrapper), either expose a constructor/property AutoMapper can populate
using a standard shape, or register an explicit `ITypeConverter` for that specific collection type
pair (see [custom-resolvers-converters.md](custom-resolvers-converters.md)) rather than relying on
convention.

### Updating an existing collection in place

`Map(source, destination)` (the two-argument overload, mapping onto an existing destination
object) does not, by default, do a fine-grained merge of collection *elements* — the typical
default behavior is to replace/repopulate the destination collection based on the source. If
"merge by key, don't just clear-and-repopulate" is a genuine requirement (e.g. so EF Core doesn't
delete-and-reinsert every child row), that's usually handled outside AutoMapper's own algorithm —
in the profile's `AfterMap`, or in the calling code — rather than through a built-in
AutoMapper collection-diffing feature. Verify the exact current behavior against the version in
use before relying on either assumption, since collection-update semantics are one of the more
frequently-revisited areas of the library.

## Polymorphic / inheritance mapping

`Include` and `IncludeBase` let a map declare inheritance relationships so that mapping a base type
reference at runtime dispatches to the correct derived map:

```csharp
CreateMap<Shape, ShapeDto>()
    .Include<Circle, CircleDto>()
    .Include<Square, SquareDto>();

CreateMap<Circle, CircleDto>();
CreateMap<Square, SquareDto>();
```

Mapping a `Shape` reference whose runtime type is actually `Circle` then produces a `CircleDto`,
not a bare `ShapeDto` missing circle-specific members — useful when mapping a polymorphic
collection (`List<Shape>` containing a mix of concrete subtypes) to a matching polymorphic DTO
hierarchy.

`IncludeBase<TSourceBase, TDestinationBase>` is the inverse direction — declared on the derived
map to say which base map it participates in, for cases where the derived maps are configured
before or independently of the base map.
