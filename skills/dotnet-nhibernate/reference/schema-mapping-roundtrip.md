# Mapping ↔ Schema Round-Trip

Fluent NHibernate has no first-class migrations story the way EF Core does. That makes the mapping-to-schema relationship something a human (or this skill) has to verify by hand rather than something the tooling guarantees — schema drift is silent until it throws in production.

## Direction 1: Given a mapping, what DDL does it imply?

Use this when writing a new mapping, to sanity-check what you're about to ask someone to create in the schema-change process (see the project's actual migration/DDL tooling — this skill doesn't prescribe one).

| Fluent mapping | Implied column |
|---|---|
| `Map(x => x.Name).Length(100).Not.Nullable()` | `NVARCHAR(100) NOT NULL` (or dialect equivalent) |
| `Map(x => x.Name)` (no `.Not.Nullable()`) | Nullable column — flag this if it's clearly meant to be required |
| `References(x => x.Customer).Not.Nullable()` | FK column `NOT NULL` + FK constraint |
| `HasMany(...)` with `Inverse()` | **No new column on this entity's table** — the FK lives on the child table, mapped from the child's `References(...)`. A common mistake is expecting a column here; there isn't one. |
| `Component(...)` | Columns flattened onto the parent table, not a separate table — check the component mapping's own column list |
| Custom `IUserType` | Column type depends entirely on `SqlTypes` in the `IUserType` implementation — don't assume based on the C# property type; check `custom-user-types.md` and the specific type |

## Direction 2: Given an existing table, what should the mapping look like?

Use this when mapping onto a legacy table that predates the current conventions.

1. **Don't assume the C# property type follows tidily from the SQL type** — legacy tables often have surprises (e.g. a `BIT` column represented as `int` for historical reasons, or a `VARCHAR` used for what's logically an enum).
2. Check nullability against the actual column, not against what "should" be nullable logically — map what's there, and separately flag if it looks wrong (that's a schema issue to raise, not something to silently paper over in the mapping).
3. For FK columns without a formal DB constraint (surprisingly common in older schemas), still map the relationship with `References(...)` — NHibernate doesn't require a DB-level FK constraint to enforce the relationship in the object model, but note the missing constraint in the PR description since it means the DB itself won't catch referential integrity violations from other code paths.
4. If a column's real-world usage doesn't match its name (a `LegacyStatus` column that's actually used for something else now), map it under its correct domain name via `.Column("LegacyStatus")` rather than propagating the misleading name into the domain model.

## Detecting drift between mapping and live schema

This is the failure mode this section exists to prevent: someone adds a column via a hand-run script or a migration that never got matched to a mapping update (or vice versa — a mapping gets changed but the DDL migration never shipped). Symptoms range from silent data loss (a mapped property that no longer has a backing column) to `GenericADOException` at runtime.

Run `scripts/detect_mapping_schema_drift.py` against a connection string or exported schema (see `scripts/README.md`) to diff mapped columns against actual table columns. Treat every flagged mismatch as a real finding to resolve, not noise — false positives here are rare (the check is a straightforward column-name/type diff), so don't dismiss results without checking.

## Safe schema evolution (expand/contract)

Because there's no migrations framework tying schema changes to mapping changes atomically, a naive rename (drop column, add column in one deploy) will break any instance still running the old mapping during a rolling deploy. Use expand/contract:

1. **Expand**: add the new column alongside the old one; deploy a mapping that writes to both (or writes to new, reads from either) so both old and new app instances can run simultaneously during rollout.
2. **Migrate data**: backfill the new column from the old one.
3. **Contract**: once all instances are on the new mapping and backfill is confirmed complete, drop the old column and the compatibility mapping code in a follow-up change — don't do this in the same deploy as step 1.

Flag any migration/mapping change in review that does a rename or type change as a single atomic step without this pattern, unless the team has explicitly accepted downtime for that deploy.
