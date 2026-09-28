# Fluent NHibernate's Convention API

Not to be confused with `mapping-conventions.md`, which documents this team's house *style* as prose a human has to remember and a reviewer has to check for. This file covers Fluent NHibernate's actual **Convention API** — code that enforces some of those same rules automatically at mapping-build time, so they can't drift silently the way a documented-but-unenforced rule can.

## Which of your documented conventions belong here — and which don't

Not everything in `mapping-conventions.md` should become a code-enforced convention. The dividing line:

| Rule | Enforce as a Convention? | Why |
|---|---|---|
| ID generation strategy (default `HiLo`) | **Yes** | Same rule for (almost) every entity — a good default that individual mappings can still override for the legacy-`Identity` exception case |
| Table naming (pluralized, PascalCase) | **Yes** | Purely mechanical, no domain judgment involved |
| FK column naming (`{Entity}Id`) | **Yes** | Same — mechanical, derivable from the property name |
| Batch size default on collections | **Yes** | A safe default (see `mapping-conventions.md`, `bulk-and-stateless.md`) that's fine to apply broadly, and easy to override per-collection when it isn't |
| Cascade / `Inverse()` choice | **No** | This is relationship-specific domain judgment (see `cascade-and-relationships.md`) — a global "always cascade this way" convention will eventually be wrong for some relationship and the mistake will be silent, since a convention firing correctly and a convention firing *wrongly* look identical from the mapping file |
| Nullability | **No** (mostly) | Whether a given field is required is a domain fact about that specific property, not a mechanical rule — a convention here would have to guess, and guessing at nullability is exactly the kind of silent, hard-to-notice mistake this skill exists to prevent elsewhere |

The rule of thumb: conventions are for **mechanical, name-derivable** rules. The moment enforcing something requires domain knowledge about a *specific* relationship or property, it belongs in a documented rule a human applies deliberately per case (and a reviewer/detector script checks), not a blanket convention.

## The convention interfaces

| Interface | Applies to |
|---|---|
| `IIdConvention` | The `Id(...)` mapping — generation strategy, column name |
| `IPropertyConvention` | Individual `Map(...)` scalar properties |
| `IReferenceConvention` | `References(...)` many-to-one relationships |
| `IHasManyConvention` | `HasMany(...)` one-to-many collections |
| `IHasManyToManyConvention` | Many-to-many collections |
| `IClassConvention` | Whole-class-level settings (table name, `DynamicUpdate`, etc.) |

## Example: enforcing this team's mechanical conventions

```csharp
public class HiLoIdConvention : IIdConvention
{
    public void Apply(IIdentityInstance instance)
    {
        instance.GeneratedBy.HiLo("hibernate_unique_key", "next_hi", "100");
    }
}

public class ForeignKeyNamingConvention : IReferenceConvention
{
    public void Apply(IManyToOneInstance instance)
    {
        instance.Column(instance.Property.Name + "Id"); // matches the {Entity}Id convention in mapping-conventions.md
    }
}

public class DefaultBatchSizeConvention : IHasManyConvention
{
    public void Apply(IOneToManyCollectionInstance instance)
    {
        instance.BatchSize(25); // see mapping-conventions.md — safe default, individual mappings can override
    }
}

public class TableNameConvention : IClassConvention
{
    public void Apply(IClassInstance instance)
    {
        instance.Table(Inflector.Pluralize(instance.EntityType.Name)); // PascalCase plural, per mapping-conventions.md
    }
}
```

Wire these into configuration alongside the mapping scan (see `templates/session-factory-di-setup.cs`):

```csharp
.Mappings(m => m
    .FluentMappings.AddFromAssemblyOf<OrderMap>()
    .Conventions.AddFromAssemblyOf<HiLoIdConvention>()) // scans the assembly for all IIdConvention/IPropertyConvention/etc. implementations
```

## Explicit mapping always wins over a convention

If a `ClassMap` explicitly sets something (`Table("LegacyOrderTable")`, a specific `.Column(...)` name, an explicit `GeneratedBy.Identity()` for a legacy table), that explicit setting overrides whatever the convention would have applied — conventions only fill in what wasn't explicitly specified. This is what makes it safe to apply a convention broadly (like `HiLoIdConvention` above) while still allowing the documented legacy-`Identity`-table exception (`mapping-conventions.md` § Identifier generation) to be handled per-mapping without a conflict.

## Restricting where a convention applies (`AcceptanceCriteria`)

A convention with no restriction applies to *every* mapping in the scanned assembly, which is occasionally too broad — e.g. an ID convention that shouldn't apply to a `Component`-mapped value object, or a table-naming convention that shouldn't touch a specific legacy entity. Restrict with `AcceptanceCriteria`:

```csharp
public class HiLoIdConvention : IIdConvention
{
    public void Accept(IAcceptanceCriteria<IIdentityInspector> criteria)
    {
        criteria.Expect(x => !x.EntityType.Namespace.Contains("Legacy"));
    }

    public void Apply(IIdentityInstance instance)
    {
        instance.GeneratedBy.HiLo("hibernate_unique_key", "next_hi", "100");
    }
}
```

**Flag a convention with no `AcceptanceCriteria` at all in review only if the mapped domain is heterogeneous enough that a blanket rule is actually risky** (e.g. a codebase mixing new entities with untouched legacy ones) — for a clean, uniform domain, no restriction is simpler and there's nothing to gain from adding one defensively.

## Testing conventions

Because a convention silently applies (or silently fails to apply, if its `AcceptanceCriteria` is wrong) across every mapping it touches, a broken convention is a broad, quiet failure mode — worse than a single wrong mapping, since it's wrong everywhere at once. Verify conventions with a startup-time or test-time assertion rather than trusting they're working from the mapping files looking right:

```csharp
[Test]
public void All_entities_use_HiLo_id_generation_except_explicitly_legacy_ones()
{
    var config = BuildConfiguration(); // however the test builds NHibernate configuration
    foreach (var classMetadata in config.ClassMappings)
    {
        if (classMetadata.EntityName.Contains("Legacy")) continue;
        Assert.That(classMetadata.IdentifierGenerator, Is.TypeOf<HiLoGenerator>(),
            $"{classMetadata.EntityName} is not using the expected HiLo generator — check for a mapping that explicitly overrides the convention, or a convention AcceptanceCriteria gap");
    }
}
```

This is a different check from the static `scripts/detect_*.py` heuristics elsewhere in this skill — those scan source text for patterns; this actually builds the NHibernate configuration and inspects the result, which is the only way to be sure a convention is genuinely taking effect versus merely present in the codebase.
