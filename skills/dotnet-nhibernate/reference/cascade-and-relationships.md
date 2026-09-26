# Cascade, Inverse, Inheritance, and Collections

## Cascade decision table

| Cascade option | Meaning | Use when |
|---|---|---|
| `Cascade.None` (default if unset) | Child entities must be saved/deleted independently | Related entity has its own independent lifecycle (e.g. `Order.Customer` — deleting an order must never delete the customer) |
| `Cascade.All` | Save/update propagates to children; delete does NOT remove children not in the collection | Child lifecycle is *mostly* tied to the parent, but you don't want orphans silently deleted just for being removed from the collection in memory |
| `Cascade.AllDeleteOrphan` | Save/update propagates; removing a child from the collection **deletes it from the DB**, deleting the parent deletes all children | True ownership — child has no meaning without the parent (line items, addresses owned by a single customer). This is what "composition" in domain modeling terms should map to. |
| `Cascade.SaveUpdate` | Only save/update propagates, no delete cascade at all | Rare; typically when deletes are handled explicitly elsewhere (e.g. soft-delete via a separate service) |

**The dangerous mistake**: using `Cascade.AllDeleteOrphan` on a relationship that isn't true ownership. If `Product` has a `HasMany(x => x.Categories)` with `AllDeleteOrphan` and categories are actually shared across products, removing a product's association silently deletes the category row for everyone. This is a data-loss bug that won't surface in testing unless the test data has shared references. When reviewing, always ask "could this related entity plausibly be referenced from somewhere else?" before accepting `AllDeleteOrphan`.

## `Inverse()`

Exactly one side of a bidirectional relationship should be marked `.Inverse()` — the side that does *not* manage the foreign key in the database. Get this wrong and you get one of two failure modes:

- **Neither side has `Inverse()`**: both sides try to manage the FK, causing extra/duplicate UPDATE statements (NHibernate issues an insert with a null FK, then a separate update to set it, from both directions) and sometimes constraint violations.
- **Both sides have `Inverse()`**: nothing actually persists the FK — NHibernate has no owning side telling it to set the column, so the relationship silently fails to save.

**Convention**: the "many" side (the `References(...)` on the child) owns the FK; the "one" side (the `HasMany(...)` on the parent) gets `.Inverse()`. This matches the physical schema — the FK column lives on the child table regardless of which side "feels" like the owner conceptually.

```csharp
// Parent
HasMany(x => x.LineItems).KeyColumn("OrderId").Inverse();
// Child
References(x => x.Order).Column("OrderId"); // owns the FK, no .Inverse() here
```

`scripts/detect_cascade_misconfig.py` checks mapping file pairs for exactly this class of mismatch — both sides having or lacking `Inverse()`.

## Inheritance strategies

| Strategy | Fluent pattern | Tradeoff |
|---|---|---|
| Table-per-hierarchy (single table, discriminator column) | `ClassMap<Base>` with `DiscriminateSubClassesOnColumn(...)`, then `SubclassMap<Derived>` | Fastest queries (no joins), but the table accumulates nullable columns for every subtype's fields — gets messy past 2-3 subtypes with divergent fields |
| Table-per-subclass (joined) | `ClassMap<Base>`, then `JoinedSubClassMap<Derived>` | Normalized, no nullable-column sprawl, but every query against a derived type joins — worse for read-heavy hot paths |
| Table-per-concrete-class | Separate `ClassMap<T>` per concrete type, no shared base table | Avoids both above tradeoffs but polymorphic queries across the hierarchy (`session.Query<Base>()`) require a UNION under the hood — check this is actually needed before picking this strategy; if you never query polymorphically, this is often simplest |

Default recommendation for a genuinely small, stable hierarchy (2-3 subtypes, rarely changing): table-per-hierarchy. For a hierarchy expected to grow or with substantially divergent fields per subtype: table-per-subclass. Table-per-concrete-class is the right call more often than people assume when polymorphic querying isn't actually a requirement — don't default to it just because it "feels more normalized" if nothing ever queries the base type directly.

## Collection type semantics

| Fluent | Ordering | Duplicates | Notes |
|---|---|---|---|
| `HasMany` → `IList<T>` (bag) | Insertion order not guaranteed on reload without explicit `OrderBy` | Allowed | Default choice, simplest mapping, but re-fetching a bag for update-in-place can issue a delete-all-then-reinsert for reordering operations — watch for this on large collections |
| `AsSet()` | Unordered | Not allowed (requires correct `Equals`/`GetHashCode` on the element or a good business key) | Use when duplicates are genuinely meaningless domain-wise; the equality requirement trips people up if the entity uses default reference equality |
| `AsList()` (indexed) | Explicit index column, ordering preserved | Allowed | Needed when order is a real domain property (e.g. steps in a workflow), not just presentation sort — costs an extra indexed column and update overhead when reordering |
| `AsMap()` | Keyed | N/A | Rare; use when the domain concept genuinely is a dictionary (e.g. translations keyed by locale) |

Default to `HasMany` (bag) unless one of the other semantics is a genuine domain requirement — don't reach for `AsSet()` "for correctness" without checking the entity actually has meaningful equality, since that's a common source of confusing runtime behavior with default `Equals`.
