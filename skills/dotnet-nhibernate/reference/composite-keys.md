# Composite Keys

Not covered anywhere else in this skill until now — a real gap, and a common one specifically for legacy tables (see `schema-mapping-roundtrip.md` § Direction 2), where a composite primary key is often inherited from a schema design predating this team's conventions rather than a deliberate choice for new entities.

## Mapping a composite ID

```csharp
public class OrderLineItemMap : ClassMap<OrderLineItem>
{
    public OrderLineItemMap()
    {
        Table("OrderLineItems");

        CompositeId()
            .KeyProperty(x => x.OrderId)
            .KeyProperty(x => x.LineNumber);

        Map(x => x.ProductSku).Not.Nullable();
        Map(x => x.Quantity).Not.Nullable();
    }
}
```

For a composite key where one component is itself a reference to another entity (common: the "many" side of a relationship using the parent's ID plus a sequence number), use `KeyReference` instead of `KeyProperty` for that component:

```csharp
CompositeId()
    .KeyReference(x => x.Order, "OrderId") // FK component of the composite key
    .KeyProperty(x => x.LineNumber);
```

## The equality requirement — this is the part people get wrong

**A composite-keyed entity's class must implement `Equals`/`GetHashCode` based on the key components**, and NHibernate has no default fallback the way it does for surrogate-ID entities (which can rely on comparing the single ID value). Skipping this isn't a compile error or even a mapping-time error — it's a runtime correctness bug:

- The first-level cache's identity map relies on equality to recognize "this is the same row" — without correct `Equals`/`GetHashCode`, you can end up with two separate in-memory objects representing the same database row within one session, and updates to one won't be reflected in the other.
- Collections of composite-keyed entities (a `HashSet<T>` or an NHibernate `AsSet()` mapping) silently allow duplicates or fail to detect membership correctly with default reference-equality behavior.

```csharp
public class OrderLineItem
{
    public virtual int OrderId { get; set; }
    public virtual int LineNumber { get; set; }
    // ... other properties

    public override bool Equals(object obj)
    {
        if (obj is not OrderLineItem other) return false;
        if (ReferenceEquals(this, other)) return true;
        return OrderId == other.OrderId && LineNumber == other.LineNumber;
    }

    public override int GetHashCode() => HashCode.Combine(OrderId, LineNumber);
}
```

Flag any `CompositeId()` mapping in review where the corresponding entity class doesn't override both `Equals` and `GetHashCode` — this is the single most common composite-key mistake and it doesn't announce itself with an exception.

## Querying by composite key

`session.Get<T>(id)` doesn't work with a single scalar `id` for a composite key — you need an instance of the entity (or a matching composite-id-shaped object) populated with the key components:

```csharp
var key = new OrderLineItem { OrderId = 42, LineNumber = 3 };
var item = session.Get<OrderLineItem>(key); // NHibernate reads the key components off this instance
```

In QueryOver/LINQ, filter on the individual key properties as ordinary `Where` conditions — there's no special composite-key query syntax needed there, since the components are just regular mapped properties from the query API's perspective.

## Interaction with caching (`NaturalId`, second-level cache)

Composite-keyed entities can still be second-level cached (`caching-and-concurrency.md`) and can still have a `NaturalId()` (`mapping-conventions.md`) distinct from the composite technical key — but double-check the cache key generation isn't accidentally relying on a `ToString()` or default hashing that doesn't match your `Equals`/`GetHashCode` override, since a cache implementation that hashes differently than NHibernate's own equality check will produce cache entries that never hit correctly. This is worth an explicit integration test for any composite-keyed entity you enable second-level caching on, rather than assuming it works the same as a surrogate-keyed entity.

## Interaction with relationships

A `References(...)` pointing at a composite-keyed entity needs to map all the FK columns that correspond to the composite key components, in the same order — this is more mapping surface area than a normal single-column FK, and a column-order mismatch here fails the same way an `ICompositeUserType` column-order mismatch does (see `custom-user-types.md`): silently swapped values rather than an exception. If a new relationship needs to reference a composite-keyed entity, consider whether that entity should really have a composite key at all, or whether introducing a surrogate ID for it (even alongside the existing composite natural key, mapped via `NaturalId()`) would simplify every downstream relationship — this is a legitimate design conversation to have rather than a foregone conclusion, especially for a table that used to have no incoming references and now needs one.
