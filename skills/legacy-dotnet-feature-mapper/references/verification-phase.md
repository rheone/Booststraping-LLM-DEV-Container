# Phase 3: Verification

The gate that makes completion claims real. Nothing reaches `documented`
without passing through here, and no completion is ever reported to the user
on the strength of a subagent's say-so.

## The rule this phase exists to enforce

> A feature is complete when a file exists on disk that passes the manifest,
> and its citations point at real lines in real files. Not when an agent says
> so.

The orchestrator verifies **its own reading of the filesystem**. A writer
subagent's report is a pointer, never evidence.

## Two independent checks

They catch different lies, so both run. Neither replaces the other.

### 1. Mechanical validation (`scripts/Test-MapperOutput.ps1`)

Cheap, exhaustive, runs over every citation in every doc.

```powershell
# per doc, immediately after it is written
./scripts/Test-MapperOutput.ps1 -Mode Doc `
    -Path <output>/features/<slug>.md `
    -SourceRoot <root1>,<root2>,<sql-definitions-path>

# at the end of every batch
./scripts/Test-MapperOutput.ps1 -Mode Batch `
    -Path <output> -SourceRoot <root1>,<root2>,<sql-definitions-path>
```

Catches: stub/empty docs, missing sections, unreplaced placeholders,
frontmatter that disagrees with the body, uncited business rules, invented file
paths, invented line numbers, and index rows claiming work that produced no
file.

Reads a JSON report on stdout. Exit codes: `0` pass, `1` failures found,
`2` the script itself could not run.

### 2. Semantic spot-check (`Explore` subagent)

The script proves a cited line *exists*. It cannot prove the line *says what
the doc claims*. So dispatch an `Explore` agent - deliberately read-only, so it
cannot quietly fix what it is grading - to sample **3 citations at random** from
the finished doc and answer, for each:

- Does the cited file:line contain what the doc says it contains?
- Is the confidence tag honest, or is an inference dressed up as `verified in code`?

Prompt it with the Session Contract (see `session-state.md`), the doc path, and
the three citations to check. It returns pass/fail per citation with a one-line
reason. Do not let it summarize the doc; it has exactly one job.

## If the script cannot run

If `pwsh` is unavailable, the execution policy blocks it, or the script exits
`2` for any reason:

1. **Do not treat it as a pass.** "Validation did not run" and "validation
   passed" are different facts, and conflating them recreates the exact bug
   this skill was rebuilt to kill.
2. Fall back to the manual checklist in `doc-manifest.md`, walked item by item.
3. Say so **loudly**: in the status line (`[verified: MANUAL - script exit 2]`),
   in the `run-log.md` entry, and in the batch rollup to the user.

## Failure handling

1. **First failure:** feed the failed items back to the writer agent
   *verbatim* - the JSON `failures[]` array, not a paraphrase - and let it fix
   the doc. Exactly one retry.
2. **Still failing:** set the index status to `verification failed`, record the
   failing items in `run-log.md`, and file a `qst-XXXX` describing what could
   not be satisfied. Move on to the next feature.
3. **Never** retry in a loop. Never downgrade a check to make a doc pass.

### Systemic-failure stop

If **3 consecutive features** fail verification, stop the run and report. That
pattern means something upstream is wrong - bad scope, an unreadable source
root, a misunderstood template - and grinding through 40 more features will
only produce 40 more bad docs.

## Recording the result

Every verification result is written to `run-log.md` (see
`status-reporting.md`) and summarized in the index `Verified` column as
`pass <ISO-8601>` / `fail <ISO-8601>` / `manual <ISO-8601>`.

An index row may only move to `documented` when a verification result exists
and passed. `documented (open questions)` is for a doc that passed verification
but has unresolved blocking questions filed against it.
