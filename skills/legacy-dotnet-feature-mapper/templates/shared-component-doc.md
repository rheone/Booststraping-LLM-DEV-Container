---
id: comp-XXXX
slug: <component-slug>
name: <Component Name>
component_type: class
location: "<path/to/file.cs or sql/procs/usp_Something.sql>"
domain: <domain>
last_updated: <date>
source_snapshot: "<git commit hash, or 'file mtimes as of <date>'>"
confidence_summary:
  verified: 0
  inferred: 0
  unverified: 0
used_by:
  - feat-XXXX
open_questions_count: 0
---
# <Component Name>

**Type:** Class | Stored Procedure | View | Function | Trigger | Utility Method
**Location:** `<path>`
**Used by:** [<feature>](../features/<feature-slug>.md)

## What it does

<Cited description of behavior.>

## Inputs / Outputs

<Parameters, return values, or - for DB objects - result sets and side effects.>

## Business rules embedded here

<!-- Same rigor as a feature doc. This is often where the real logic lives,
     especially for stored procs and triggers. Every bullet needs a citation
     and a confidence tag. -->

- <rule> - `<citation>` `(<confidence tag>)`

## Side effects

<What else this touches that is not obvious from its name - triggers fired,
audit rows written, columns recalculated.>

## Confidence / Open Questions

<!-- open_questions_count must equal the number of bullets here. -->

- <item> - <why it could not be verified>
