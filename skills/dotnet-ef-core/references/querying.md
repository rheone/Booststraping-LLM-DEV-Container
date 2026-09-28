# Querying

## LINQ-to-SQL translation

EF Core translates a LINQ query expression tree into SQL at the point you enumerate it (`ToList`,
`ToListAsync`, `foreach`, `First`, etc.) — the query itself, before that point, is just an
`IQueryable<T>` expression tree, not yet executed. A method or property access EF Core's provider
doesn't know how to translate throws at execution time (`InvalidOperationException` naming the
untranslatable part) rather than at compile time, since C# has no way to check SQL-translatability
statically. `.ToList()` (or any terminal operator) called earlier than intended — before you've
finished composing filters — forces the whole result set into memory and continues filtering
client-side in LINQ-to-Objects, silently, which is correct but defeats the purpose of pushing the
filter to the database.

## Include and ThenInclude

```csharp
var orders = await dbContext.Orders
    .Include(o => o.Customer)
    .Include(o => o.Lines).ThenInclude(l => l.Product)
    .ToListAsync();
```

`Include` eager-loads a navigation property in the same query (as a `JOIN`, by default);
`ThenInclude` chains onto the navigation `Include` just loaded to reach a second level. Omitting
`Include` for a navigation you then access lazily (if lazy loading is enabled) or not at all (if it
isn't) is the single most common source of an `N+1` query pattern or a `NullReferenceException` on
an unloaded reference navigation.

## Single query vs. split queries

By default, `Include`ing more than one collection navigation on the same root produces a single SQL
query with multiple `JOIN`s, which returns a cross-product-shaped result set — the root entity's
columns repeat once per combination of child rows across all included collections. For two included
collections of meaningful size, this multiplies row count and network payload. `AsSplitQuery()`
issues one SQL query per included collection instead:

```csharp
var orders = await dbContext.Orders
    .Include(o => o.Lines)
    .Include(o => o.Notes)
    .AsSplitQuery()
    .ToListAsync();
```

Split queries trade the cross-product cost for multiple round trips and lose the guarantee that all
the data reflects one consistent point in time if the database changes between the split queries
(rare in a single-request context, but a real consideration outside a serializable transaction).
Prefer a single query for collections that are small or that you're only including one of; reach
for `AsSplitQuery()` once you're including more than one non-trivial collection on the same query.

## AsNoTracking

```csharp
var summary = await dbContext.Orders
    .AsNoTracking()
    .Select(o => new OrderSummary(o.Id, o.Total))
    .ToListAsync();
```

`AsNoTracking()` tells EF Core not to register the returned entities with the change tracker — no
snapshot is kept, so mutating the returned instances has no effect on the database and no
`SaveChanges` call will ever pick them up. Use it for any query whose results you only read (a GET
endpoint, a report, a projection to a DTO) — it skips the per-entity identity resolution and
snapshotting cost.

`AsNoTrackingWithIdentityResolution()` is the middle ground: skips change tracking (like
`AsNoTracking`) but still deduplicates repeated appearances of the same entity within one result set
(e.g. the same `Customer` appearing on multiple `Order` rows via `Include`) into a single shared
instance — useful when a no-tracking query's result graph still needs reference equality between
repeated entities, without paying for full tracking.

## Filtering after Include

Pre-.NET-5/EF Core 5, filtering a collection navigation loaded via `Include` required loading the
whole collection and filtering client-side. Current EF Core resolves a `Where` (and `OrderBy`)
applied directly on the collection navigation inside `Include`/`ThenInclude` into the generated SQL:

```csharp
var orders = await dbContext.Orders
    .Include(o => o.Lines.Where(l => l.Quantity > 0))
    .ToListAsync();
```
