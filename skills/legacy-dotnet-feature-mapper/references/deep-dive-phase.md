# Phase 2: Deep-dive

Goal: fully document one feature (or a small batch, per user instruction) to the standard in `feature-doc-template.md`. This is the rigorous pass - every business rule, permission check, failure state, and DB interaction should be found and cited.

## Before starting

- Confirm the feature exists in `index.md` (or add it if the user is pointing at a specific entry point not yet in the index - that's fine, just also add the index row).
- If a doc already exists for this feature at `features/<slug>.md`: **ask the user whether to fully regenerate or incrementally update** (unless running in AFK mode - see `afk-mode.md` for that default). Don't guess.
- Set the feature's index status to `in progress` before starting, `documented` when the doc passes the completeness check below.

## Tracing process

1. **Start at the entry point(s)** identified in Discovery. Read the actual code - `Page_Load`, event handlers, action methods - in full.
2. **Follow every call path** from the entry point through business/service-layer classes to wherever data access happens. Note every conditional branch that represents a business rule (validation, eligibility checks, calculated values, workflow gating).
3. **Identify DB touchpoints**: every stored proc call, inline SQL (parameterized or, if found, dynamically built - flag dynamic SQL explicitly since it limits static traceability), ORM query, or direct table access. Hand these off to the TSQL analysis process (`tsql-analysis.md`) using the DB definitions folder given at kickoff.
4. **Identify permission/role checks**: `User.IsInRole`, custom authorization attributes, membership/role provider calls, or DB-driven permission lookups. If no check is found guarding an action that looks like it should have one, say so explicitly as a finding - don't silently omit it.
5. **Identify predecessors/successors**: what feature(s) typically lead into this one (e.g., a list page before a detail page), and what this one leads to (e.g., approval triggers a notification feature). Base this on actual navigation/redirect code (`Response.Redirect`, post-back targets, workflow status transitions), not assumption - cite it.
6. **Identify failure states**: validation failures, exception handling, what the user sees/what happens to data when something goes wrong. Read `try`/`catch` blocks, validation control logic, and any custom error-handling middleware in scope.
7. **Note anything unresolvable by static analysis** (dynamic SQL, reflection-based dispatch, config values only known at runtime, feature flags) with the `unverified assumption` tag and a one-line reason.

## Shared code and DB objects

While tracing, if you hit a class, method, stored proc, view, function, or trigger that's clearly generic/reusable (not feature-specific) or that you can see is called from more than one place, don't inline its full behavior into the feature doc. Instead:

- Create or update its doc in `shared-components/<slug>.md` (see `shared-components.md`)
- Link to it from the feature doc with a one-line summary of what it does and why this feature depends on it
- **If this is the first time the component is being documented**, assign it the next unused `comp-XXXX` ID (same rule as `feat-XXXX` in `discovery-phase.md`: sequential, assigned once, never reused or renumbered even if the component is later renamed). If it already has a doc, reuse its existing `id` - don't assign a new one.
- Either way, add this feature's `id` to the component doc's `used_by` frontmatter list if it isn't already there.

## Maintaining frontmatter on every write

Whenever a feature or shared-component doc is created, fully regenerated, or incrementally updated, the frontmatter must be kept in sync with the body - this isn't a one-time setup step:

- `confidence_summary` counts (`verified`/`inferred`/`unverified`) must match the actual tags used in the body at the end of this session, not whatever they were before the update.
- `last_updated` and `source_snapshot` are refreshed to reflect this session's work.
- `db_objects`, `predecessors`, `successors`, `related_shared_components` (or `used_by`, for shared components) are updated to reflect anything added or removed this session - use IDs, not names.
- `open_questions_count` matches the actual number of items in the Open Questions section.
- `id` and `slug`-derived filename are the only fields that should *not* change on an update.

## Diagrams

Add a Mermaid diagram only where it adds real clarity beyond prose - typically:

- A multi-state workflow (state diagram) - e.g., an approval process with statuses and transitions
- A non-obvious multi-hop call chain across layers (flowchart)
- Time-ordered interaction across several components/DB calls where order matters (sequence diagram)

Skip diagrams for simple single-path CRUD features - a diagram of "load page → save to one table" adds nothing.

## Completeness check before marking `documented`

- [ ] Entry point(s) and purpose stated
- [ ] Happy path fully traced to its DB or file writes/reads
- [ ] Failure states documented (validation, exceptions, edge cases found in code)
- [ ] Business rules listed, each cited with file/line + confidence tag
- [ ] Permissions/roles documented (or explicitly noted as "none found")
- [ ] Predecessors/successors noted (or explicitly "none found / standalone")
- [ ] DB interactions documented, traced per `tsql-analysis.md`
- [ ] Shared components linked, not in-lined
- [ ] Open/unverifiable items listed in their own section, not buried
- [ ] Diagram included only if it adds value
- [ ] Frontmatter (`confidence_summary`, `last_updated`, `source_snapshot`, relationship lists, `open_questions_count`) matches the finished body

If any box can't be checked because static analysis genuinely couldn't resolve it, that's fine - document it as an open item rather than blocking completion indefinitely.
