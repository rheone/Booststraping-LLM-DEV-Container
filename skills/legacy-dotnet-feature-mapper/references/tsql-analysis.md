# TSQL Analysis

A large share of business logic in this class of application lives in the database, not the C#/VB code. Treat stored procedures, triggers, views, and functions with the same rigor as application code - don't stop at "calls `usp_ApproveOrder`."

## Source of truth

All DB object definitions live in the `.sql` scripts folder given at kickoff. Search that folder for `CREATE PROCEDURE`, `CREATE TRIGGER`, `CREATE VIEW`, `CREATE FUNCTION`, `CREATE TABLE` (and `ALTER` variants if the folder contains incremental scripts rather than a single current-state dump - check which pattern applies and note it if ambiguous). Never assume schema/logic that isn't found in these scripts. If the folder appears incomplete for an object you need (called from code but no definition found), flag it as an open item - don't guess at the proc's behavior from its name alone beyond an `inferred from naming` tag.

## Depth required

When a feature calls a stored procedure:

1. Read the full proc body.
2. For every table it touches, note read vs. write.
3. For every **trigger** defined on a table the proc writes to, read that trigger too - it fires as a side effect of the proc's write and often contains business logic the calling C# code has no visibility into (audit logging, cascading updates, computed field maintenance, business validation that can silently fail the write).
4. For every **view** the proc or app code queries, read the view definition - resolve it down to base tables so the doc reflects real data lineage, not just the view name.
5. For every **function** (scalar or table-valued) referenced, read its definition and document what it computes.
6. Note **computed columns** on any table involved, since they represent business logic embedded in schema rather than code.
7. Watch for **cursors, dynamic SQL (`EXEC`/`sp_executesql`), and cross-database/linked-server calls** - these are common sources of untraceable-by-name behavior. Dynamic SQL in particular should be flagged with `unverified assumption` unless the dynamically built statement can be fully resolved from static parameters.

## Documenting findings

- If the proc/trigger/view/function is used by only this one feature: document its business rules inline in the feature doc's Database Interactions section.
- If it's used by (or clearly reusable across) more than one feature, or is complex enough to be a first-class part of business logic on its own: create a `shared-components/` doc for it, per `shared-components.md`, and link it. Stored procedures containing significant business logic should usually get their own shared-component doc even if currently only called once - it's better documented once at the right rigor than inlined and lost.

## Side effects to always check for on writes

- Triggers firing on INSERT/UPDATE/DELETE (before treating a write as "simple", confirm no trigger exists on that table)
- Cascading FK actions (`ON DELETE CASCADE`, etc.) defined in the table's `CREATE TABLE`/constraint scripts
- Computed/persisted columns recalculating as a result of the write

Every DB-derived business rule gets the same citation format as code: SQL object name + relevant line(s) in its definition script, plus a confidence tag.
