# Data access within a slice

VSA doesn't mandate a specific data-access pattern, but the feature-first organizing principle
pushes naturally toward per-slice data access rather than a single shared repository interface
every slice is forced to use identically. This file lays out the two poles and the tradeoff
between them.

## Pattern A: a query object (or inline query) per slice

Each slice owns exactly the data access it needs, shaped exactly for its own request/response —
often as a single method or small class colocated with the rest of the slice, issuing a query
tuned for that one read or write.

```csharp
namespace MyApp.Features.Orders.GetOrderById;

public sealed class GetOrderByIdHandler
{
    private readonly OrderDbContext _db; // whatever persistence context the project uses

    public GetOrderByIdHandler(OrderDbContext db) => _db = db;

    public async Task<GetOrderByIdResponse?> HandleAsync(
        GetOrderByIdRequest request,
        CancellationToken cancellationToken)
    {
        // Query shaped exactly for this slice's response — selects only what's needed,
        // with no obligation to match any other slice's shape.
        return await _db.Orders
            .Where(o => o.Id == request.OrderId)
            .Select(o => new GetOrderByIdResponse(o.Id, o.CustomerId, o.Total, o.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
```

**Advantages:**

- The query is exactly as expensive as the slice needs — a list view can project only the columns
  it renders; a detail view can join in everything it needs; neither is constrained by a
  general-purpose repository method that has to serve every caller.
- No repository interface accumulates methods over time as new slices need slightly different data
  shapes (`GetById`, `GetByIdWithLineItems`, `GetByIdForExport`, ...) — each slice just writes the
  query it needs.
- A slice's data access is exactly as easy to test and reason about as the rest of the slice: it's
  right there, in the same file/folder.

**Disadvantages:**

- The same underlying query logic (e.g., "orders visible to this customer, excluding soft-deleted
  ones") can end up duplicated, slightly differently, across many slices if that filter isn't
  factored out somewhere all of them can call.
- No single place enforces that every code path touching an entity applies the same invariants —
  each slice's author has to know and reapply them.

## Pattern B: a shared repository per aggregate/entity

A conventional repository abstraction (`IOrderRepository` with `GetByIdAsync`, `AddAsync`, etc.)
sits behind every slice that touches orders, same as it would in a layered architecture.

**Advantages:**

- One place to enforce persistence-level invariants (soft-delete filters, tenant isolation,
  concurrency tokens) consistently across every slice.
- Familiar to teams coming from layered architectures; lower migration friction when adopting VSA
  incrementally (see [fit-and-adoption.md](fit-and-adoption.md)).

**Disadvantages:**

- Reintroduces exactly the kind of shared surface VSA is trying to avoid: the repository interface
  becomes a point every slice depends on and every change to it has to consider every caller,
  which is the coupling VSA's per-feature isolation is meant to eliminate.
- Repository methods tend toward a lowest common denominator (`GetByIdAsync` returning the full
  entity) that over-fetches for slices that only need a projection, or under-fetches for slices
  that need a join the generic method doesn't provide — leading to either N+1 queries or a
  proliferation of repository method overloads.

## Choosing between them

Neither pattern is universally correct; the choice tracks the same axis as
[fit-and-adoption.md](fit-and-adoption.md):

- **Favor query-object-per-slice** when read shapes genuinely differ across slices (a list view, a
  detail view, and an export each want different columns/joins), and when the team is comfortable
  factoring out a shared filter/spec (e.g., a reusable `IQueryable<Order>` base query with
  tenant/soft-delete filtering applied) rather than a full repository, to avoid the duplication
  downside above.
- **Favor a shared repository** when an entity has strict, non-negotiable invariants that must be
  enforced identically on every access path (e.g., multi-tenant row isolation, mandatory audit
  fields), and where the cost of one more level of indirection is worth the guarantee that no
  slice can accidentally bypass it.
- **A middle ground** many mature VSA codebases land on: a thin, generic persistence context (the
  ORM's unit-of-work/`DbContext`-equivalent) is shared as pure infrastructure — see
  [cross-cutting-concerns.md](cross-cutting-concerns.md) — while each slice writes its own query
  or command against that shared context directly, without an intermediate repository interface.
  This keeps the truly cross-cutting piece (connection/transaction management) shared while leaving
  each slice's actual query shape free to vary.

Whichever pattern is chosen, apply it consistently enough that a reader doesn't have to guess,
slice by slice, which convention is in play — see
[pitfalls.md](pitfalls.md#inconsistent-slice-granularity) for the cost of unpredictable
per-slice conventions.
