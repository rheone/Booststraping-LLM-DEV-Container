// TEMPLATE — projecting straight to a DTO shape instead of loading full entities.
// See reference/query-strategy.md § Projections and reference/lazy-loading-and-fetching.md
// § AutoMapper + Proxies for why this is the preferred path for read-heavy endpoints:
// it sidesteps lazy-loading/proxy concerns entirely since nothing is materialized as a
// tracked/proxied entity — the join happens in the generated SQL, not via a lazy property access.

using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;

public sealed class {Entity}SummaryDto
{
    public Guid Id { get; set; }
    public string SomeScalarField { get; set; }
    public string RelatedEntityName { get; set; } // pulled via join in the projection, not a lazy load
    public int ChildCount { get; set; }             // aggregate, not the full child collection
}

public static class {Entity}Projections
{
    // LINQ projection — default choice, see reference/query-strategy.md decision table.
    public static Task<List<{Entity}SummaryDto>> GetSummariesAsync(
        ISession session, {FilterType} filter, CancellationToken cancellationToken = default)
    {
        return session.Query<{Entity}>()
            .Where(e => e.SomeField == filter) // adapt the actual filter shape
            .Select(e => new {Entity}SummaryDto
            {
                Id = e.Id,
                SomeScalarField = e.SomeScalarField,
                RelatedEntityName = e.RelatedEntity.Name, // fine here: projection generates a join,
                                                           // this does NOT touch a proxy/lazy-load path
                ChildCount = e.Children.Count               // aggregate in SQL, not a loaded collection
            })
            .ToListAsync(cancellationToken);
    }

    // QueryOver equivalent — reach for this instead of LINQ when the query shape is built up
    // conditionally across several branches (see reference/query-strategy.md decision table).
    public static async Task<IList<{Entity}SummaryDto>> GetSummariesQueryOverAsync(
        ISession session, {FilterType} filter, CancellationToken cancellationToken = default)
    {
        {Entity} entityAlias = null;
        RelatedEntity relatedAlias = null;

        var query = session.QueryOver(() => entityAlias)
            .JoinAlias(() => entityAlias.RelatedEntity, () => relatedAlias)
            .Where(() => entityAlias.SomeField == filter)
            .SelectList(list => list
                .Select(() => entityAlias.Id).WithAlias(() => resultAlias.Id)
                .Select(() => relatedAlias.Name).WithAlias(() => resultAlias.RelatedEntityName))
            .TransformUsing(Transformers.AliasToBean<{Entity}SummaryDto>());

        return await query.ListAsync<{Entity}SummaryDto>(cancellationToken);
    }

    // Placeholder used only for the WithAlias(...) expressions above — QueryOver's AliasToBean
    // transformer needs a typed reference to build the alias expressions against.
    private static readonly {Entity}SummaryDto resultAlias = null;
}

// Reminder: if this projection is feeding what used to be an AutoMapper entity->DTO mapping,
// this REPLACES that mapping for the read path — don't also load the full entity and map it,
// that reintroduces the exact cost this template exists to avoid.
