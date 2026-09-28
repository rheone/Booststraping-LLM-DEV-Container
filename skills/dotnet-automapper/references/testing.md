# Testing AutoMapper Configurations and Mappings

Testing AutoMapper is its own category, distinct from writing the profiles themselves: a
mapping profile can compile and even pass `AssertConfigurationIsValid()` while still producing
wrong values, so a real test suite needs both a structural check and behavioral (value-level)
checks.

## 1. Structural test: AssertConfigurationIsValid in a test

The highest-value single AutoMapper test in most codebases: build the exact same
`MapperConfiguration` the app uses (same profiles, same assemblies) and assert on it once, in CI.

```csharp
public sealed class MapperConfigurationTests
{
    [Fact]
    public void Configuration_Is_Valid()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(OrderProfile).Assembly); // scan the same assembly the app uses
        });

        config.AssertConfigurationIsValid();
    }
}
```

Keep this test scanning the *same* assembly/assemblies the application's `AddAutoMapper` call
scans — a test that only registers a hand-picked subset of profiles can pass while the real,
fully-scanned app configuration is broken.

## 2. Behavioral tests: assert on actual mapped values

`AssertConfigurationIsValid()` only proves every member has *some* configured source — it says
nothing about whether that source is the *correct* one, or whether custom logic (`MapFrom`,
resolvers, conditions) produces the right result for representative inputs. Write ordinary unit
tests against `IMapper.Map` for any mapping with non-trivial logic:

```csharp
public sealed class OrderProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>())
        .CreateMapper();

    [Fact]
    public void Maps_Total_As_Sum_Of_Line_Items()
    {
        var order = new Order
        {
            LineItems = [new LineItem { Price = 10m }, new LineItem { Price = 5m }],
        };

        var dto = _mapper.Map<OrderDto>(order);

        Assert.Equal(15m, dto.Total);
    }
}
```

Trivial one-to-one property mappings (name matches, type matches, no custom logic) generally don't
need a dedicated behavioral test per property — the structural `AssertConfigurationIsValid` test
already proves they're wired up, and a value-by-value test for every such property is low-value,
high-maintenance-burden busywork. Reserve behavioral tests for maps with a `MapFrom` expression,
a resolver/converter, a `Condition`, or a flattening/unflattening path non-obvious enough that a
future change could silently break it.

## 3. Testing custom resolvers and converters in isolation

An `IValueResolver`, `IValueConverter`, or `ITypeConverter` with real logic (especially one with
injected dependencies) is worth unit testing directly, independent of the full `IMapper` pipeline
— this isolates whether a mapping bug is in the resolver's own logic vs. in how the profile wires
it up:

```csharp
public sealed class AgeResolverTests
{
    [Fact]
    public void Resolves_Age_From_BirthDate()
    {
        var resolver = new AgeResolver();
        var person = new Person { BirthDate = new DateTime(DateTime.UtcNow.Year - 30, 1, 1) };

        var age = resolver.Resolve(person, new PersonDto(), 0, default!);

        Assert.Equal(30, age);
    }
}
```

For a resolver/converter with constructor dependencies, construct it directly with fakes/mocks in
the test rather than going through DI or the full `MapperConfiguration` — that keeps the test
focused on the resolver's own behavior.

## 4. Testing ProjectTo separately from Map

Because `ProjectTo` translates into a provider-specific query (see
[queryable-projection.md](queryable-projection.md)) rather than reusing the in-memory `Map`
pipeline, a mapping that works correctly via `Map` is not guaranteed to translate correctly via
`ProjectTo` — provider translation limits are a distinct failure mode. If a project relies on
`ProjectTo` against EF Core, cover it with a test that actually executes the query against a real
or in-memory-but-provider-faithful database (e.g. EF Core's SQLite in-memory provider, or a real
test database), not just against `Map` on already-materialized objects — EF Core's fully
in-memory (non-relational) provider in particular can mask translation problems that only surface
against a real relational provider.

## 5. Testing DI registration (optional, higher-level)

For catching "the app's actual startup wiring is broken" issues (wrong assembly passed to
`AddAutoMapper`, a profile not picked up by scanning), a lightweight integration test that builds
the real `IServiceCollection`/`IServiceProvider` the app uses and resolves `IMapper` from it,
then runs `AssertConfigurationIsValid()` against the resolved `IMapper`'s
`ConfigurationProvider`, closes the gap between "the profile in isolation is valid" and "the app
actually wires it up correctly." This overlaps with #1 above; it's worth adding specifically when
DI registration itself (assembly/profile scanning) has broken before.
