---
name: audit-remediation-pipeline
description: Systematic code audit remediation using a multi-agent review pipeline. Resolve findings from audits, static analysis, or code reviews with specialized sub-agents (pedantic dev, tech writer, auditor) in a structured pipeline. Supports batching, parallelism, headless mode, and user-grilling for context. Use when fixing audit findings, remediating code quality issues, addressing code review comments, or systematically improving codebase standards from static analysis.
author: Robert Engelhardt <rheone@gmail.com>
version: 0.0.0
---

# Audit Remediation Pipeline

## Core Principles

- **Preserve existing markers**: Never remove TODOs, FIXMEs, HACKs, obsolete code, or commented-out code. If the finding relates to one, try to address the underlying issue. If context is insufficient, add a warning comment noting the gap and document it in the batch log for user follow-up.
- **No assumptions on ambiguity**: If a finding's intent, scope, or required approach is ambiguous, do not guess. Note the ambiguity in the batch log, flag the finding as `BLOCKED:ambiguous`, and proceed to the next finding. The user reviews blocked findings and either clarifies or removes them.
- **Minimal change**: Fix exactly what the finding describes. No scope creep. Reject proposed fixes that do more than necessary.

## Phase 1: Findings Input

Two formats accepted.

**Structured JSON** (preferred):
```json
{"findings": [{"id":"H2","severity":"high","type":"code-change","files":["Foo.cs"],"description":"..."}]}
```

**Free-form**: run `@findings-parser` to normalize into the same schema.

### Phase 1b: Context (grill-me)

Before starting, if available, load the `grill-me` skill and establish:

1. **Priorities**: Confirm severity ranking. Must-fix items?
2. **Constraints**: Language, framework, target versions, SDK compatibility
3. **Autonomy**: Headless (auto-approve) or checkpoint-after-each?
4. **Re-audit**: Verify via `@re-auditor` or an external tool?

If the user gives sparse answers, escalate each unresolved item with a targeted question. Do not proceed until scope and priorities are clear.

## Phase 2: Plan

1. **Analyze**: Group by file/area, split `type` groups (code changes stay, `test-gap`/`documentation`/`config` route to other handlers)
2. **Batch**: Non-overlapping → parallel; overlapping → sequential; order by severity
3. **Scope**: For each finding, identify file(s) and approach before coding
4. **Present**: Show batch breakdown, ask for confirmation

## Phase 3: Execute Batch

For each batch, process findings in the sub-agent pipeline:

### Per-finding pipeline (launch with sub-agents via `task`)

| Step | Agent (file) | Responsibility |
|------|-------------|----------------|
| 1 | **Research** (`@explore`) | Read affected files, understand context, identify all sites |
| 2 | **Pedantic dev** ([`pedantic-dev.md`](agents/pedantic-dev.md)) | Review code correctness, edge cases, security, performance |
| 3 | **Tech writer** ([`tech-writer.md`](agents/tech-writer.md)) | Review doc comments, code comments, typos, grammar, terminology |
| 4 | **Auditor** ([`auditor.md`](agents/auditor.md)) | Verify fix matches the finding; check for regressions; approve/reject |
| 5 | **Implementer** ([`implementer.md`](agents/implementer.md)) | Apply the approved fix using `edit` tool or equivalent |
| 6 | **Verifier** ([`verifier.md`](agents/verifier.md)) | Build and run targeted tests |

### Revision loop (auditor rejection)

If the auditor returns **REVISE** or **REJECT**, route back to `@pedantic-dev` with feedback. Max 3 retries per finding, then escalate to user. See [REFERENCE.md](REFERENCE.md#loop-patterns) for the full loop diagram.

### Non-code finding routing

The auditor can return `ROUTE: test-fix` / `ROUTE: documentation` / `ROUTE: config` to dispatch non-code findings to the appropriate handler instead of the standard pipeline. See [REFERENCE.md](REFERENCE.md#non-code-finding-routing) for the full dispatch table.

### State persistence (crash resume)

A `pipeline-state.json` tracks batch/finding/stage/retries. On resume, the orchestrator skips completed work and restarts at the exact pipeline stage. See [REFERENCE.md](REFERENCE.md#pipeline-state-schema-crash-resume) for the schema.

### Workflow rules

- Steps 2–4 run **sequentially** per finding (each previous output feeds next)
- Steps 1 can run **in parallel** across findings in the same batch
- If findings touch the same file, run their pipelines **sequentially**
- Update `batch-N-log.md` and `pipeline-state.json` after each fix

### Branch strategy

- Dedicated branch per batch (e.g., `pilot/batch-1`)
- Commit after each batch, not each finding
- For independent batches, use `git worktree` for parallel execution

### Agent files

See the [agents/](agents/) directory for all agent markdown files. See [REFERENCE.md](REFERENCE.md#agent-configuration-opencodejson) for the `opencode.json` equivalent.

## Phase 4: Re-audit

After all batches complete, verify everything is resolved:

1. Full build across all target frameworks
2. Run full test suite (or a representative subset if suite is large)
3. Run `@re-auditor` with the original findings list and current codebase scan. It classifies each finding as **RESOLVED**, **PARTIAL**, or **UNRESOLVED**, and checks for regressions.
4. If any findings remain unresolved, create a follow-up batch and loop back to Phase 3.

## Parallelism Strategy

| File overlap | Strategy |
|---|---|
| No overlap | Parallel `task` agents (one per finding) |
| Partial overlap | Parallel within file groups, sequential across groups |
| High overlap | Sequential pipeline within batch |

For truly independent batches (zero file overlap), use `git worktree add` for parallel execution with separate checkouts.

See [REFERENCE.md](REFERENCE.md) for agent specifications, checklists, and loop patterns.
