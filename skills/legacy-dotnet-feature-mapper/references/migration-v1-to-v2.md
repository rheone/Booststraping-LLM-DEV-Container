# Migrating v1 output to v2

v2 adds structure that v1 documents do not have. Nothing already documented has
to be redone - migrate in place, preserve every id and status, and log it.

This path is **defensive**: it has not been exercised against real v1 output.
If a migration looks like it would lose information, stop and ask rather than
proceeding.

## Detecting v1

An `index.md` with no `schema_version: 2` in its frontmatter, or missing
`scope_ledger` / `scan_ledger` / `discovery_complete`.

## What changes

| Area | v1 | v2 |
|---|---|---|
| Index frontmatter | `in_scope_csprojs`, `feature_counts` | adds `schema_version`, `discovery_complete`, `output_root`, `roots`, `scope_ledger`, `scan_ledger`, `sql_definitions_path`; `feature_counts` gains `documented_open_questions` and `verification_failed` |
| Index columns | `Feature, Entry Point(s), Status, Doc, Last Updated, Notes` | `ID, Feature, Entry Point(s), Size, Status, Doc, Last Updated, Verified, Notes` |
| Statuses | 4 values | adds `documented (open questions)` and `verification failed` |
| Logs | `afk-log.md` | `run-log.md` (attended and unattended) |
| Questions | none | `questions-for-user.md` |
| Config | none | `.feature-mapper.json` at the repo root |

## Migration steps

1. **Back up** `index.md` to `index.md.v1.bak` in the output root before
   touching anything.
2. Add the new frontmatter keys. `discovery_complete: false` unless the v1 run
   is known to have finished - assume incomplete, since v1 had no way to record
   it. `scan_ledger` starts as `not-scanned` for every path; the next Discovery
   run fills it in.
3. Move each row's `feat-XXXX` id out of the doc frontmatter (or the Notes
   column, wherever v1 put it) into a new leading `ID` column. **IDs and
   statuses are preserved exactly** - never renumber.
4. Add a `Size` column. Leave `M` with a Notes marker for anything not yet
   sized; the next Discovery pass will size it properly.
5. Add a `Verified` column set to `- (pre-v2, unverified)` for every existing
   row. These docs were produced without a verification gate, so their status
   is a claim, not a verified fact - say so rather than back-dating a pass.
6. Rename `afk-log.md` to `run-log.md` and append a migration entry. Leave the
   old entries as they are; do not rewrite history into the new schema.
7. Create `questions-for-user.md` (empty) and `.feature-mapper.json` from the
   templates, filling in the paths from the v1 index and this session.
8. Run `Test-MapperOutput.ps1 -Mode Batch` over the migrated tree. Expect
   failures on pre-existing docs - that is information, not a problem. Report
   the list to the user and offer to re-derive the worst offenders.
9. Log the migration to `run-log.md` and report what changed.

## Existing feature docs

Do not rewrite them wholesale. On the next incremental update of each doc,
bring its frontmatter up to the v2 manifest as part of that work. A v1 doc that
is never revisited stays as it is, correctly marked `pre-v2, unverified` in the
index - which is an honest description of its status.
