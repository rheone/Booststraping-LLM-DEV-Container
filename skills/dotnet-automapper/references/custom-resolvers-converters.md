# Custom Value Resolvers, Value Converters, Type Converters

Three distinct extension points, easy to conflate. Pick based on what varies: the whole
destination *type's* mapping, one *member's* value, or a *value type* used repeatedly across many
maps.

## IValueResolver / IMemberValueResolver — resolve one destination member

Use when a single destination member needs logic AutoMapper's conventions/`MapFrom` lambda can't
express cleanly, especially when that logic needs injected dependencies (a repository, a service).

```csharp
public sealed class AgeResolver : IValueResolver<Person, PersonDto, int>
{
    public int Resolve(Person source, PersonDto destination, int destMember, ResolutionContext context)
        => DateTime.UtcNow.Year - source.BirthDate.Year;
}

CreateMap<Person, PersonDto>()
    .ForMember(dest => dest.Age, opt => opt.MapFrom<AgeResolver>());
```

`IMemberValueResolver<TSource, TDestination, TSourceMember, TDestMember>` is the same idea but
also receives the specific source member value (via a member-scoped `MapFrom` overload) rather
than the whole source object — useful when the resolver's logic is really about transforming one
already-identified source member.

Resolvers registered this way are constructed by AutoMapper's configured service provider when
using `AddAutoMapper` — they're resolved transiently from DI, so constructor-inject whatever
dependencies they need rather than `new`-ing them inline.

## IValueConverter<TSourceMember, TDestMember> — convert one member's value/type

A narrower, more reusable sibling of `IValueResolver`: it only receives the already-selected
source member's value (not the whole source object), which makes it a good fit for a small,
reusable conversion (e.g. `string` ↔ `Money`, a custom parsing rule) that isn't naturally an
implicit conversion.

```csharp
public sealed class MoneyConverter : IValueConverter<decimal, Money>
{
    public Money Convert(decimal sourceMember, ResolutionContext context) => Money.FromDecimal(sourceMember);
}

CreateMap<Order, OrderDto>()
    .ForMember(dest => dest.Total, opt => opt.ConvertUsing<MoneyConverter, decimal>(src => src.TotalAmount));
```

## ITypeConverter<TSource, TDestination> — override an entire type pair's mapping

Use when a source/destination *type pair* needs completely custom mapping logic that bypasses
AutoMapper's member-by-member convention entirely — effectively "for this type pair, don't use
AutoMapper's usual algorithm, run this code instead."

```csharp
public sealed class StringToUriConverter : ITypeConverter<string, Uri>
{
    public Uri Convert(string source, Uri destination, ResolutionContext context) => new Uri(source);
}

CreateMap<string, Uri>().ConvertUsing<StringToUriConverter>();
```

Because a type converter fully replaces the member-mapping algorithm for that type pair, it
applies globally everywhere that source/destination type pair is mapped (including as a nested
member of some other map), not just at one `ForMember` call site. Reach for `ITypeConverter` when
that global, type-pair-wide scope is actually what's wanted; reach for `IValueResolver`/
`IValueConverter` when the custom logic is specific to one member in one map.

## Choosing among the three

| Need | Use |
| --- | --- |
| One destination member, needs the whole source object + injected services | `IValueResolver` |
| One destination member, needs only the corresponding source member's value, reusable across maps | `IValueConverter` |
| An entire source/destination type pair should always convert the same custom way, everywhere it appears | `ITypeConverter` |

All three are picked up by DI scanning under `AddAutoMapper` and registered transiently, so
constructor injection works in each without extra wiring.
