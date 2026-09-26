# Conditional Mapping, Null Substitution, Pre/Post-Processing

## Condition

Skip mapping a specific destination member when a condition on the source (and/or destination)
isn't met — the member is left at its existing/default value instead of being overwritten:

```csharp
CreateMap<OrderUpdateDto, Order>()
    .ForMember(dest => dest.Status, opt => opt.Condition(src => src.Status != null));
```

`Condition` receives the source object (and overloads add the destination object, the resolved
source member value, and the destination member value) so the check can depend on more than just
the immediate source member.

## PreCondition

`PreCondition` is evaluated *before* AutoMapper resolves the source value for that member (e.g.
before running a resolver or `MapFrom` expression), whereas `Condition` is evaluated *after* the
value is resolved. Use `PreCondition` to skip expensive/unsafe resolution work entirely rather
than resolving a value and then discarding it:

```csharp
CreateMap<Source, Dest>()
    .ForMember(dest => dest.ExpensiveField,
        opt => opt.PreCondition(src => src.ShouldCompute)
                  .MapFrom(src => ExpensiveComputation(src)));
```

## NullSubstitute

Supply a fallback value for a destination member when the resolved source value is null, instead
of propagating the null (or throwing, for a non-nullable value-type destination member):

```csharp
CreateMap<Person, PersonDto>()
    .ForMember(dest => dest.DisplayName, opt => opt.NullSubstitute("Unknown"));
```

`NullSubstitute` runs in place of the null value only — it does not change the type of the
member or otherwise alter the map's structure.

## BeforeMap / AfterMap (map-level pre/post-processing)

Run arbitrary code immediately before or after AutoMapper performs the member-by-member mapping
for a given `CreateMap`, with access to both source and destination objects:

```csharp
CreateMap<Order, OrderDto>()
    .BeforeMap((src, dest) => src.EnsureCalculated())
    .AfterMap((src, dest) => dest.MappedAtUtc = DateTime.UtcNow);
```

Typical uses: invariant/derived-state calculation on the source before mapping runs, or stamping
metadata (timestamps, computed totals that depend on the *fully mapped* destination) after member
mapping completes.

## BeforeMapAction / AfterMapAction (global pre/post-processing)

Configured once on the `MapperConfiguration` (not per `CreateMap`) and run for *every* map the
configuration performs — for genuinely cross-cutting behavior (e.g. logging every mapping
operation) rather than logic specific to one type pair:

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<OrderProfile>();
    cfg.Internal().ForAllMaps((typeMap, expression) => { /* global config */ });
});
```

Prefer per-`CreateMap` `BeforeMap`/`AfterMap` for anything specific to one type pair; reserve
configuration-wide hooks for behavior that genuinely applies to every map, since they're harder to
reason about locally when reading a single profile.

## When conditional logic gets heavy, stop

If a single member needs several chained `Condition`/`PreCondition`/`NullSubstitute` calls to
express what is really business logic (not a mapping concern), that's a signal the mapping is
doing too much. Move the logic into the domain/service layer and let the map be a straightforward
data-shape transformation — see
[pitfalls-and-alternatives.md](pitfalls-and-alternatives.md) for why heavily-conditional profiles
become a maintenance burden.
