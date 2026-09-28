// TEMPLATE — AutoMapper profile pattern that avoids the #1 recurring NHibernate bug on this stack:
// silently triggering lazy loads (or worse, LazyInitializationException) via entity -> DTO mapping.
// See reference/lazy-loading-and-fetching.md § AutoMapper + Proxies before writing a new profile.

using AutoMapper;

public sealed class {Entity}Profile : Profile
{
    public {Entity}Profile()
    {
        // PREFERRED PATH: if this mapping only feeds a read endpoint, don't map from the loaded
        // entity at all — project straight to the DTO in the query (see reference/query-strategy.md
        // § Projections) and skip AutoMapper + the entity graph entirely for that path.

        // If you DO need entity -> DTO mapping (e.g. the entity is also used for a write elsewhere
        // in the same method), be explicit about every property that touches a lazy relationship:

        CreateMap<{Entity}, {Entity}Dto>()
            // Scalar, non-lazy properties: safe, no comment needed.
            // .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))

            // LAZY PROPERTY — requires the entity to have been eager-fetched (.Fetch(x => x.Customer))
            // in the query that loaded it, OR requires the session to still be open when this
            // mapping runs. Keep this comment in sync with the actual fetch strategy used upstream —
            // if the fetch strategy changes, this mapping's safety assumption changes too.
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer.Name))

            // LAZY COLLECTION — same caveat, and additionally: iterating a large lazy collection here
            // pulls the whole collection into memory just to map it. If only a count or aggregate is
            // needed, prefer projecting that directly in the query instead of mapping the full collection.
            .ForMember(d => d.LineItemCount, opt => opt.MapFrom(s => s.LineItems.Count));
    }
}

// Run scripts/detect_automapper_lazy_risk.py against this file and the corresponding entity
// mapping before merging — it cross-references which mapped properties correspond to lazy-mapped
// source members, catching the case where this comment drifts out of sync with reality.
