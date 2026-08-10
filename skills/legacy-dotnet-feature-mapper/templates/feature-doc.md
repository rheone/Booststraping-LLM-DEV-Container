---
id: feat-XXXX
slug: <feature-slug>
title: <Feature Name>
status: in progress
trigger_type: user-initiated
domain: <domain>
entry_points:
  - "<file path> : <method or page>"
csprojs:
  - <csproj path>
last_updated: <date>
source_snapshot: "<git commit hash, or 'file mtimes as of <date>'>"
confidence_summary:
  verified: 0
  inferred: 0
  unverified: 0
db_objects:
  - <schema.object>
predecessors: []
successors: []
related_shared_components: []
has_diagram: false
open_questions_count: 0
---
# <Feature Name>

**Status:** in progress
**Last updated:** <date>
**Entry point(s):** `<file path> : <method or page>`
**Trigger type:** user-initiated

## Purpose

<2-4 sentences on the business capability this feature provides.>

## Predecessors / Successors

- **Comes from:** <feature(s) that lead here, or "none found / entry point"> - `<citation>` `(<confidence tag>)`
- **Leads to:** <feature(s) this triggers, or "none found / terminal"> - `<citation>` `(<confidence tag>)`

## Authentication

<!-- webhook trigger_type ONLY. Delete this whole section for every other
     trigger type - the validator fails a non-webhook doc that keeps it. -->

## Roles / Permissions

- <who can do this and how it is enforced> - `<citation>` `(<confidence tag>)`

## Happy Path

1. <step> - `<citation>` `(<confidence tag>)`
2. <step> - `<citation>` `(<confidence tag>)`

## Business Rules

<!-- Every bullet here MUST carry both a file:line citation and a confidence
     tag. The validator checks each bullet individually. -->

- <rule> - `<citation>` `(<confidence tag>)`

## Failure States

- <condition> -> <what happens> - `<citation>` `(<confidence tag>)`

## Database Interactions

| Object | Type | Read/Write | Notes | Citation | Confidence |
|---|---|---|---|---|---|
| `<schema.object>` | Table | Read/Write | <note> | `<file.sql:NN>` | verified in code |

## Diagram

<!-- Include a mermaid block ONLY if it adds clarity beyond the prose, and set
     has_diagram accordingly. If you omit it, replace this comment with a line
     starting "No diagram - " and the reason. -->

## Open Questions / Unverified Items

<!-- open_questions_count in the frontmatter must equal the number of bullets
     here. Write "- None." and set the count to 0 if there are none. -->

- <item> - <why static analysis could not resolve it>

## Related Shared Components

- [../shared-components/<slug>.md](../shared-components/<slug>.md) - <why this feature depends on it>
