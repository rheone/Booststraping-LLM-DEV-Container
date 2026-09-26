# Relationships and Configuration

## Fluent API vs. data annotations

You configure an entity's mapping either by attributes on the CLR type or by fluent calls inside
`OnModelCreating`. The two overlap for simple cases (`[Key]` vs. `HasKey`, `[Required]` vs.
`IsRequired()`) but the fluent API covers configuration data annotations cannot express at all
(composite keys spanning multiple properties, most relationship-shape decisions beyond a simple
foreign key, index configuration, owned-type mapping). Fluent configuration wins when both target
the same property — it's applied after conventions and after annotations during model building.

```csharp
// Data annotations
public sealed class Order
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = "";
}

// Equivalent fluent API
modelBuilder.Entity<Order>(builder =>
{
    builder.HasKey(o => o.Id);
    builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
});
```

Prefer the fluent API as the single place relationship and constraint configuration lives once a
model has more than a couple of entities — it keeps mapping concerns out of the entity class
itself and gives you access to configuration data annotations don't reach. An `IEntityTypeConfiguration<T>`
class per entity, applied via `modelBuilder.ApplyConfigurationsFromAssembly(...)`, keeps
`OnModelCreating` itself from growing into one large method as the model grows.

## One-to-many and many-to-many

```csharp
modelBuilder.Entity<Order>()
    .HasMany(o => o.Lines)
    .WithOne(l => l.Order)
    .HasForeignKey(l => l.OrderId);

modelBuilder.Entity<Post>()
    .HasMany(p => p.Tags)
    .WithMany(t => t.Posts);
```

A many-to-many relationship without an explicit join entity gets an EF Core-managed join table with
no CLR type of its own — sufficient when the relationship itself carries no extra data. The moment
the relationship needs its own properties (e.g. a timestamp on when a tag was applied), configure an
explicit join entity with two one-to-many relationships instead of relying on the implicit join
table.

## Owned types

An owned type models a value that only ever exists as part of its owner — no independent identity or
table of its own by default:

```csharp
public sealed class Order
{
    public int Id { get; set; }
    public Address ShippingAddress { get; set; } = null!;
}

public sealed class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress);
```

By default, an owned type's properties map to columns on the owner's own table (prefixed by the
navigation name where needed to avoid collisions). `OwnsMany` maps a collection of owned instances
to a separate table keyed back to the owner, since a single row can't hold a variable-length
collection inline. An owned type is not the same as a value converter (`HasConversion`) — reach for
an owned type when the value has multiple properties that belong together conceptually; reach for a
value converter when a single property needs a custom CLR-to-column-type mapping.

## Shadow properties and foreign keys

A foreign key property doesn't need to exist explicitly on the dependent CLR type — EF Core creates
a "shadow property" for it by convention when a navigation exists without a matching FK property.
Declaring the FK property explicitly (`public int OrderId { get; set; }` on `OrderLine`) makes it
visible to ordinary C# code and LINQ queries; leaving it as a shadow property is fine when nothing
outside EF Core's own mapping needs to read or set it directly.
