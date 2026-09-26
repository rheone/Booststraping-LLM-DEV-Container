# Schema Drift Auditor (specialized subagent)

Use this as instructions for a dedicated subagent/focused pass when the user asks for a full
audit of mapping-vs-schema drift across an entire codebase — not for a single-diff PR review
(that's covered by the "Full Review Workflow" section of SKILL.md directly). This is a bigger,
slower job: reading every mapping file, reconciling it against live schema, and producing a
report — worth spawning as its own focused task so it doesn't blow the context budget of
whatever the main conversation was doing.

## When to use this vs. the lightweight script

- **One file/PR, quick check**: just run `scripts/detect_mapping_schema_drift.py` directly (once
  implemented — see `scripts/README.md`) and report the result inline.
- **Whole codebase, periodic hygiene audit, or "we think there's drift somewhere but don't know
  where"**: use this agent process — it's slower and more thorough, and produces an artifact
  (a report) worth keeping rather than a transient inline answer.

## Process

1. **Enumerate every mapping file** in the target codebase (`*Map.cs` for Fluent, plus any
   remaining `*.hbm.xml` — see `reference/query-strategy.md` § Named Queries for why legacy XML
   mappings might still exist here).
2. **Enumerate every table the mappings claim to describe**, from `Table(...)` calls (or the
   XML equivalent).
3. **Obtain the live schema** — either a connection string to a non-production replica/read
   copy, or an exported `INFORMATION_SCHEMA` dump if a live connection isn't available or
   appropriate. If neither is available, stop and ask the user rather than guessing at schema
   from old migration scripts, which may be stale themselves — that defeats the point of a drift
   audit.
4. **For each mapped table, diff**:
   - Columns present in the mapping but missing from the live schema (mapping references a
     column that no longer exists — will throw at runtime the moment that property is touched)
   - Columns present in the live schema but not mapped (not necessarily a bug — could be
     intentionally unmapped legacy data — but worth flagging for a human to confirm intent)
   - Type mismatches (mapped as one SQL type, actual column is a different one)
   - Nullability mismatches (mapped `.Not.Nullable()` against an actually-nullable column, or
     vice versa)
5. **Cross-check `HasMany`/`References` pairs against actual foreign key constraints** in the
   schema — flag relationships mapped in code with no corresponding DB-level FK constraint
   (not necessarily wrong, per `reference/schema-mapping-roundtrip.md`, but worth surfacing).
6. **Produce a report**, not just a raw diff dump — group findings by severity:
   - **Will throw / already broken**: missing columns, clear type mismatches
   - **Silent risk**: nullability mismatches, missing FK constraints
   - **Informational**: unmapped columns that might be intentional

## Ground rules specific to this agent

- **Read-only.** This agent never proposes or runs schema changes or mapping edits as part of
  the audit itself — it produces findings for a human (or a follow-up, separately-scoped task)
  to act on. Mixing "audit" and "fix" in one pass risks rushing fixes without review, exactly the
  kind of unsupervised DDL risk `SKILL.md`'s ground rules warn against.
- **Don't guess at intent for ambiguous findings** (e.g. an unmapped column that might be
  intentionally legacy/unused) — list it and say it needs a human to confirm, rather than
  deciding it's fine or a bug.
- If the codebase is large enough that this would take many tool calls, say so up front and
  confirm scope (which projects/schemas) with the user before starting, rather than silently
  doing a partial audit and presenting it as complete.
