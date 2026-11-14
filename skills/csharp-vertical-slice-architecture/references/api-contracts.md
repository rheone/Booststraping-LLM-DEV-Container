# API contracts

## Shared query contracts

Paging, sorting, and filtering repeat across query slices. Share the shape, not the query.

```csharp
// Infrastructure/Http/PageRequest.cs: shared infrastructure, no business meaning.
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int Skip => (Math.Max(Page, 1) - 1) * Math.Clamp(PageSize, 1, MaxPageSize);
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);
}

public sealed record Page<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class PagingExtensions
{
    public static async Task<Page<T>> ToPageAsync<T>(
        this IQueryable<T> query, PageRequest page, CancellationToken ct = default) =>
        new(
            await query.Skip(page.Skip).Take(page.Take).ToListAsync(ct).ConfigureAwait(false),
            page.Page,
            page.Take,
            await query.CountAsync(ct).ConfigureAwait(false));
}
```

- Each query slice keeps its own filter record (`ListOrdersRequest(string? Status, Guid? CustomerId)`) and
  its own item response. Only paging is shared.
- Sorting: accept a closed set of sort keys per slice (an enum or a string switch). Never pass a client
  string into dynamic ordering.
- Bind query-string records with `[AsParameters]` in Minimal APIs.
- Cap page size on the server.
- Cursor (keyset) paging is per slice when a list is large or changes often; offset paging is the default.

## OpenAPI per slice

- `.WithName("CreateOrder")`: a stable operation ID, the slice name.
- `.WithTags("Orders")` on the area group.
- `.WithSummary(...)`, `.WithDescription(...)` on the endpoint.
- `Results<T1, T2, ...>` return types document every status code without `Produces` attributes.
- A union response appears as `anyOf` in .NET 11 OpenAPI documents; a closed hierarchy with
  `InferClosedTypePolymorphism` appears with a `$type` discriminator. Prefer domain-named response
  records over exposing outcome unions in the contract.

## Versioning

A new version of an endpoint is a new slice when its behavior or contract differs
(`Features/Orders/CreateOrderV2/`), or a second edge over the same handler when only the transport shape
differs. The `Asp.Versioning.Http` package supports Minimal APIs (`app.NewVersionedApi()`,
`.HasApiVersion(1.0)`, `MapGroup(...).HasApiVersion(2.0)`)
(<https://github.com/dotnet/aspnet-api-versioning>). Use it when the repo already does; otherwise ask.

Deprecate by version, delete the old slice folder when its version retires. Nothing else changes, which is
the payoff of slicing.

## Multi-tenancy

- Resolve the tenant once at the edge (host header, claim, route) into a scoped `ITenantContext`.
- Enforce isolation in shared infrastructure, not in each slice: EF Core global query filters
  (`HasQueryFilter("Tenant", e => e.TenantId == tenant.Id)`), NHibernate filters, or a schema/database per
  tenant chosen by the connection factory.
- Slices never accept a tenant ID from the request body.
- Every slice gets one integration test that proves a second tenant's data is invisible.

## Contract stability

Request and response records are the public contract. Treat a change to them as a behavior change
(standing rule 6): add optional members, never rename or remove without a new version. Add a snapshot or
contract test for slices with external consumers.
