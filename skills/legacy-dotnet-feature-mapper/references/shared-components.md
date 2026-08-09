# Shared Components

A shared component is any class, method, stored procedure, view, function, or trigger used by more than one feature (or clearly generic/reusable even if you've only seen one caller so far - e.g. a `DataAccessHelper` base class). Document it once; link to it from every feature that depends on it.

## When to create one

- While tracing a feature in Phase 2, you hit code/SQL that's clearly not feature-specific (naming like `Utils`, `Helpers`, `Base*`, or a stored proc called from multiple distinct call sites you've observed)
- The user tells you something is shared

Don't preemptively document everything in a `Common`/`Shared` folder just because it exists - only create a doc when a feature you're actively documenting actually depends on it. This keeps effort proportional to what's in use.

## Template

Save as `shared-components/<component-slug>.md`:

```markdown
---
id: comp-0001
slug: <component-slug>
name: <Component Name>
component_type: class   # class | stored-procedure | view | function | trigger | utility-method
location: "<path/to/file.cs or sql/procs/usp_Something.sql>"
domain: <inferred from folder/namespace, e.g. billing>
last_updated: <date>
source_snapshot: "<git commit hash if available, otherwise 'file mtimes as of <date>'>"
confidence_summary:
  verified: <count>
  inferred: <count>
  unverified: <count>
used_by:
  - <feature id(s) that depend on this component>
open_questions_count: <count>
---
# <Component Name>

**Type:** Class | Stored Procedure | View | Function | Trigger | Utility Method
**Location:** `path/to/file.cs` or `sql/procs/usp_Something.sql`
**Used by:** [feature-a](../features/feature-a.md), [feature-b](../features/feature-b.md)  <!-- keep updated as more features reference it; also update the `used_by` id list above -->

## What it does

Plain description of behavior, cited.

## Inputs / Outputs

Parameters, return values, or (for DB objects) result sets / side effects (e.g. rows written, triggers fired downstream).

## Business rules embedded here

Same citation + confidence-tag format as feature docs - this is often where core logic actually lives (especially for stored procs/triggers), so treat it with the same rigor as a feature doc, not as a footnote.

## Side effects

For triggers/procs especially: what else does this touch that isn't obvious from its name (e.g. a trigger on `Orders` that also writes an audit row to `OrderHistory` and updates a `Customers.LastOrderDate` column).

## Confidence / Open Questions

Same format as feature docs.
```

## Maintaining the "Used by" list

Every time a feature deep-dive links to an existing shared component, add that feature to the component's "Used by" list if it's not already there. This makes shared-components/ doubly useful: a full map of blast radius if the rewrite team changes shared logic.

Note: comp-XXXX IDs follow the same rule as feat-XXXX - assigned once, never reused or renumbered.
