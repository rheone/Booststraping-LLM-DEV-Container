# ProjectTo: Projecting IQueryable (EF Core and Other ORMs)

## Why ProjectTo exists

`Map`/`Map<TDest>` operates on already-materialized objects in memory. Against an `IQueryable<T>`
backed by a database provider (Entity Framework Core, etc.), calling `.ToList()` first and then
`Map`-ing each element would load **every column of every row** into memory before discarding
whatever the DTO doesn't need. `ProjectTo<TDestination>()` instead translates the configured
mapping into the query's expression tree, so the underlying provider generates a `SELECT` that
fetches only the columns actually needed by the destination shape — the projection happens in the
database, not after materialization.

```csharp
using AutoMapper.QueryableExtensions; // ProjectTo extension method

List<OrderDto> dtos = await dbContext.Orders
    .Where(o => o.CustomerId == customerId)
    .ProjectTo<OrderDto>(mapperConfiguration) // or mapper.ConfigurationProvider
    .ToListAsync();
```

`ProjectTo` needs either an `IConfigurationProvider` (typically `mapper.ConfigurationProvider`, or
the `MapperConfiguration` instance itself) or, for parameterized projections, an
`IMapper`-provided overload — check the overload set for the version in use, since exact overload
shapes have moved around across major versions.

## ProjectTo must be the last call in the chain

Apply all filtering, sorting, and paging (`Where`, `OrderBy`, `Skip`/`Take`) to the **entity**
query first, and call `ProjectTo` last, immediately before materializing (`ToList`, `ToListAsync`,
`FirstOrDefault`, etc.):

```csharp
// Correct: filter/sort on entities, project last
var page = await dbContext.Orders
    .Where(o => o.Status == OrderStatus.Open)
    .OrderByDescending(o => o.CreatedAt)
    .Skip(skip).Take(pageSize)
    .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
    .ToListAsync();
```

Calling `ProjectTo` earlier and then continuing to compose LINQ against the *projected* DTO shape
risks the query provider being unable to translate expressions that reference AutoMapper's
generated projection logic, producing either a translation exception or an unintended client-side
evaluation (silently pulling more data into memory than intended). Keep entity-shaped query
composition and DTO-shaped projection strictly separated, with projection last.

## What ProjectTo can and can't express

`ProjectTo` only works with mapping configuration that can be translated into an expression tree
the query provider understands — plain member access, `MapFrom` with translatable expressions
(arithmetic, simple method calls the provider supports), and nested/collection projections built
from other `CreateMap`s. Constructs that require actually running arbitrary .NET code per row —
most custom `IValueResolver`/`ITypeConverter` implementations, anything touching non-translatable
APIs — generally are **not** usable inside a `ProjectTo` projection, because there's no way to
push arbitrary C# execution into a SQL `SELECT`. When a member's mapping genuinely needs resolver
logic that can't translate, either compute it after materialization (accepting the extra data
transferred) or restructure the query so the untranslatable piece happens outside the
`ProjectTo`d shape.

## ProjectTo vs. Map — pick based on data source

| | `Map` | `ProjectTo` |
| --- | --- | --- |
| Operates on | already-materialized objects | `IQueryable<T>` (translated to the provider's query) |
| Where filtering happens | in memory (data already loaded) | in the database (before materialization) |
| Supports custom resolvers/converters freely | yes | only when provider-translatable |
| Typical use | mapping between layers on data you already have | reading a paged/filtered API/report result set from a database |

Reach for `ProjectTo` specifically for read paths against a queryable data source — it is not a
general replacement for `Map` on data already in memory.
