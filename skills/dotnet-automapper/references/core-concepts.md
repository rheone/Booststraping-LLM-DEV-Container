# Core Concepts

## CreateMap and Profile

A `Profile` is a class where you declare type-pair mappings via `CreateMap<TSource, TDestination>`.
Profiles are the standard organizational unit — one per bounded area of the app (per aggregate,
per feature, per module), not one giant profile for the whole app.

```csharp
public sealed class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
        CreateMap<CustomerDto, Customer>(); // reverse map is a *separate* CreateMap call...
    }
}
```

For a two-way mapping, `ReverseMap()` generates the reverse `CreateMap` automatically, inferring a
reasonable configuration from the forward map (including reversing `ForMember` source/destination
member pairs where possible):

```csharp
CreateMap<Customer, CustomerDto>()
    .ReverseMap();
```

`ReverseMap` is a convenience, not a guarantee of full fidelity — configuration that isn't
naturally invertible (a computed/flattened member, a custom resolver) usually needs its own
explicit adjustment on the reverse side.

## MapperConfiguration and IMapper

- `MapperConfiguration` is built once from one or more `Profile` instances (or profile types) and
  holds the compiled mapping plans. Building it is relatively expensive — build it once per
  application lifetime, not per request.
- `IMapper` is the runtime interface used to actually perform mappings (`Map<TDest>(source)`,
  `Map(source, destination)`). It's created from a `MapperConfiguration` and is cheap/thread-safe
  to use per-call once built.

Manual (non-DI) setup, useful in a console app or a test:

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<CustomerProfile>();
});

IMapper mapper = config.CreateMapper();
var dto = mapper.Map<CustomerDto>(customer);
```

## Dependency injection registration: AddAutoMapper

Since **AutoMapper v13.0**, `AddAutoMapper` is part of the core `AutoMapper` NuGet package — the
formerly-separate `AutoMapper.Extensions.Microsoft.DependencyInjection` package is no longer
needed for new projects (it still exists for older AutoMapper major versions).

```csharp
// Scan one or more assemblies for Profile types
builder.Services.AddAutoMapper(cfg => { }, typeof(CustomerProfile).Assembly);

// Or pass explicit marker types (one per assembly to scan)
builder.Services.AddAutoMapper(cfg => { }, typeof(CustomerProfile), typeof(OrderProfile));

// The cfg delegate configures the MapperConfiguration itself, e.g. the license key (v15.0+)
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
}, typeof(CustomerProfile));
```

`AddAutoMapper` registers:

- `MapperConfiguration` — singleton.
- `IMapper` — transient (resolved fresh per injection, cheap to construct from the singleton
  configuration).
- Any `ITypeConverter`, `IValueConverter`, `IValueResolver`, `IMemberValueResolver`
  implementations found via assembly scanning — transient, so they can take constructor-injected
  dependencies (e.g. a scoped `DbContext` or a domain service) resolved per request. This is the
  main reason to prefer DI-resolved custom resolvers/converters over `new`-ing them up inline in
  `ForMember`.

Inject `IMapper` wherever mapping is needed:

```csharp
public sealed class CustomersController(IMapper mapper) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<CustomerDto> Get(int id) => mapper.Map<CustomerDto>(await _repo.GetAsync(id));
}
```

## Property mapping conventions (the default behavior)

By default, `CreateMap<TSource, TDestination>()` maps a source member to a destination member when
their **names match exactly** (case-sensitive by default) and the types are compatible (identical,
implicitly convertible, or themselves mappable via another registered `CreateMap`). No
`ForMember` calls are needed for members that already line up this way — that's the point of
convention-based mapping. Members that don't line up by name need explicit configuration; see
[member-mapping.md](member-mapping.md) for flattening conventions and `ForMember`.
