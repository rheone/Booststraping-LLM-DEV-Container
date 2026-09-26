// TEMPLATE — adapt, don't copy-paste blindly. Ordering and conventions per reference/mapping-conventions.md.
// Delete this comment block and any sections that don't apply before committing.

using FluentNHibernate.Mapping;

public sealed class {Entity}Map : ClassMap<{Entity}>
{
    public {Entity}Map()
    {
        Table("{TableName}"); // PascalCase, plural — match existing schema conventions

        // 1. Identifier — default to HiLo unless mapping onto a legacy Identity-column table.
        //    See reference/mapping-conventions.md § Identifier generation before changing this.
        Id(x => x.Id).GeneratedBy.HiLo("hibernate_unique_key", "next_hi", "100");

        // 2. Scalar properties — always explicit about nullability, don't rely on the Fluent default.
        Map(x => x.SomeRequiredField).Not.Nullable().Length(100);
        Map(x => x.SomeOptionalField).Nullable();

        // DateTime convention: UTC, suffix property name with "Utc". See reference/mapping-conventions.md.
        // Map(x => x.CreatedAtUtc).Not.Nullable();

        // Enum convention: map by string name, not int, unless there's a stated reason not to.
        // See reference/custom-user-types.md § Enum mapping specifics.
        // Map(x => x.Status).CustomType<{EnumType}>().Not.Nullable();

        // 3. Components (value objects spanning multiple columns on this table)
        // Component(x => x.SomeValueObject, c => { c.Map(v => v.Field1); c.Map(v => v.Field2); });

        // 4. References (many-to-one) — this side owns the FK. Do NOT mark this side .Inverse().
        // References(x => x.Parent).Column("ParentId").Not.Nullable();

        // 5. Collections (one-to-many / many-to-many)
        //    - .Inverse() belongs on the "one" side (this side), NOT on the child's References(...).
        //    - Pick the cascade deliberately — see reference/cascade-and-relationships.md.
        //      AllDeleteOrphan means true ownership: removing from this collection DELETES the row.
        // HasMany(x => x.Children)
        //     .KeyColumn("{Entity}Id")
        //     .Inverse()
        //     .Cascade.AllDeleteOrphan()
        //     .BatchSize(25); // mitigates N+1 if this collection is accessed unpredictably elsewhere

        // 6. Cross-cutting directives last, with a comment if non-default.
        // DynamicUpdate(); // only if this entity has many columns and updates rarely touch all of them
    }
}
