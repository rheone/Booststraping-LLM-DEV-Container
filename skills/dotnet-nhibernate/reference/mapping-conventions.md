# Fluent NHibernate Mapping Conventions

House style for `ClassMap<T>`, `SubclassMap<T>`, and `ComponentMap<T>`. Prefer this over ad-hoc formatting so mappings are skimmable across the codebase — a reviewer should be able to glance at any mapping file and know where to find the ID, the columns, and the relationships without reading top to bottom.

**This file documents rules as prose a human applies and a reviewer checks.** Several of the mechanical ones below (ID generation, table naming, FK naming, batch size) can instead be enforced automatically via Fluent NHibernate's actual Convention API — see `fluent-convention-api.md` for which of these rules are good candidates to stop relying on human memory for entirely.

## Ordering within a ClassMap

1. `Table(...)` / schema declaration
2. `Id(...)` — identifier and generation strategy
3. Scalar properties (`Map(...)`), grouped logically, not alphabetically — group by what they mean to the domain, not the wire format
4. Component mappings (`Component(...)`)
5. References (`References(...)`) — many-to-one
6. Collections (`HasMany`, `HasManyToMany`) — one-to-many / many-to-many, in the order they're likely to matter for review (usually: owned children first, then lookups)
7. `DynamicUpdate()`/`DynamicInsert()` and other cross-cutting directives last, with a comment explaining why if non-default

```csharp
public sealed class OrderMap : ClassMap<Order>
{
    public OrderMap()
    {
        Table("Orders");

        Id(x => x.Id).GeneratedBy.HiLo("hibernate_unique_key", "next_hi", "100");

        Map(x => x.OrderNumber).Not.Nullable().Length(32).Unique();
        Map(x => x.Status).CustomType<OrderStatus>().Not.Nullable(); // see custom-user-types.md for enum mapping convention
        Map(x => x.PlacedAtUtc).Not.Nullable(); // UTC convention — see below

        References(x => x.Customer).Column("CustomerId").Not.Nullable();

        HasMany(x => x.LineItems)
            .KeyColumn("OrderId")
            .Inverse()                 // Order does not own the FK management — OrderLineItem does
            .Cascade.AllDeleteOrphan() // line items have no independent lifecycle outside their order
            .BatchSize(25);
    }
}
```

## Identifier generation strategy

| Strategy | When to use |
|---|---|
| `HiLo` | Default for new entities in this codebase — avoids a round trip per insert, works well with batching |
| `GuidComb` | Entities where the ID needs to be client-generatable before insert (e.g. building an object graph in memory before any DB call) or exposed externally and you don't want sequential IDs leaking row-count information |
| `Identity` | Only when mapping onto a legacy table that already uses DB-generated identity columns — don't introduce new ones; identity columns disable certain batching optimizations |

Don't mix strategies for related entities without a specific reason — consistency here matters more than which one you pick, because mixed strategies make bulk insert/import code (`bulk-and-stateless.md`) harder to write generically.

## Naming conventions

- Table names: PascalCase, plural (`Orders`, `OrderLineItems`) — match existing schema conventions, don't introduce a new casing style into an established table set.
- FK columns: `{ReferencedEntity}Id` (`CustomerId`, not `Customer_Id` or `FK_Customer`).
- Mapping class names: `{Entity}Map` in a file named `{Entity}Map.cs`, colocated with or adjacent to the entity class depending on the project's existing layout — check the existing pattern in the target project rather than assuming.

## Nullability

Every `Map(...)` call should have an explicit `.Not.Nullable()` or be deliberately left nullable — don't rely on the Fluent default silently. This makes intent reviewable and catches accidental nullable columns on required domain fields.

## DateTime convention

Store all `DateTime` columns as UTC, suffix the C# property name with `Utc` (`PlacedAtUtc`, not `PlacedAt`) so it's unambiguous at every call site without needing to check the mapping. Do not map `DateTimeOffset` unless the column genuinely needs to preserve an offset (e.g. representing a user's local scheduling time) — for pure instant-in-time values, UTC `DateTime` is the convention. See `custom-user-types.md` if you need a `DateTimeOffset` column type, since it isn't mapped by default the same way across all dialects.

## Computed columns (`Formula`)

For a read-only value derived from other columns or a subquery, map it with `Formula(...)` instead of computing it in application code at every call site:

```csharp
Map(x => x.LineItemCount)
    .Formula("(SELECT COUNT(*) FROM OrderLineItems li WHERE li.OrderId = Id)")
    .ReadOnly();
```

This runs as part of the `SELECT` for the entity, so it's dialect-specific SQL embedded in the mapping — flag it in review the same way you'd flag any hand-written SQL (see `dialect-notes.md`), and don't reach for this if the same value is more simply expressed as an aggregate in a projection (`query-strategy.md`) for the specific read paths that need it; `Formula` is for a value that should behave like a normal property on every load of the entity, not a one-off report field.

## Natural keys (`NaturalId`)

If an entity has a stable business key distinct from its surrogate `Id` (an order number, an external system's identifier), mark it with `NaturalId()`:

```csharp
Map(x => x.OrderNumber).Not.Nullable().Unique();
// ...
NaturalId().Property(x => x.OrderNumber);
```

This enables `session.GetByNaturalId<Order>().Using(...)`, which — when the entity is also second-level cached (`caching-and-concurrency.md`) — can be cached by natural key rather than only by surrogate ID, useful when the business key is what calling code actually has on hand (e.g. an API receiving an external order number, not your internal GUID).

## Where this skill stops

Two related NHibernate features are real but deliberately not built out here, since they're either rarely the right call or a large separate concern:
- **`Any()` polymorphic associations** (a single FK column that can reference one of several different entity types) — usually a sign a cleaner domain model (a shared base type with a normal `References`, or a join table) would serve better. If you're considering `Any()`, treat that as a prompt to reconsider the design first, not a mapping problem to solve.
- **NHibernate.Envers** (full change-history/audit-trail tracking) — a separate NuGet package and a meaningfully larger feature than the audit-columns pattern in this file. If the team needs full history-of-changes auditing rather than just "who/when last touched this row," that's worth its own evaluation and possibly its own skill, not an extension of this one.

## When NOT to use Fluent mapping

If you're touching a `.hbm.xml` file that already exists in a legacy part of the codebase, don't unilaterally convert it to Fluent mid-change — that's a larger, separate refactor with its own review. See `query-strategy.md` § Named Queries for the equivalent guidance on the query side.
