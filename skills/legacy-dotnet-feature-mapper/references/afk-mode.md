# AFK Mode

Runs Discovery and/or Deep-dive continuously without pausing for per-feature confirmation, until an explicit endpoint is reached or a hard blocker forces a stop.

## Starting an AFK run

Before doing any work, confirm with the user (ask if not already stated in the request):

1. **Endpoint condition** - exactly one of:
   - _Full completion_: every feature currently in (or discovered during) `index.md` reaches `documented` status.
   - _Target list_: a specific named/counted set given by the user (e.g., "these 12 entry points," "the first 30 features," "everything under `/Billing/`"). Confirm the exact set before starting so the endpoint is unambiguous.
2. Confirm the three required inputs from `kickoff-checklist.md` are already set (csproj scope, SQL folder, output path) - AFK mode does not relax this requirement.
3. Tell the user roughly what will happen (Discovery first if no index exists, then Deep-dive across the target set) before starting, but don't ask for step-by-step confirmation once running.

## The run loop

1. If no `index.md` exists yet at the output path, run Discovery first automatically.
2. Work through Deep-dive on features in the target set, one at a time, in a sensible order (e.g., index order, or grouping related features together if that reduces redundant shared-component tracing).
3. For each feature:
   - Set status to `in progress`, do the full deep-dive per `deep-dive-phase.md`, write the doc, update shared-components as needed.
   - If a doc already exists for this feature: **default to incremental update automatically** (do not ask). Log this decision in the AFK log (see below).
   - Set status to `documented` when the completeness check passes.
   - Update `index.md` and `system-overview.md` immediately - don't batch updates until the end, so a run can be inspected or safely resumed mid-way if interrupted.
4. Continue to the next feature without waiting for user input.
5. Check the endpoint condition after each feature completes.

## AFK log

Maintain `<output-docs-path>/afk-log.md`, appending an entry per feature processed:

```markdown
## <date/time> - <Feature Name> - <feature-slug>

- Mode: [full regenerate | incremental update | new doc]
- Result: documented | open items remain (see doc)
- Notable open questions: <brief, or "none">
```

This gives the user a fast way to review what happened during an unattended run without re-reading every doc.

## Stop conditions

**Stop and report back (successful completion) when:**

- The endpoint condition is met.

**Stop and report back (blocked) when:**

- A given `.csproj` path, SQL definitions folder, or output path doesn't exist or can't be read.
- Scope is ambiguous in a way that can't be resolved by convention already established elsewhere in the index/docs (e.g., a genuinely unclear feature boundary that materially changes the target set).
- Something suggests the in-scope/out-of-scope boundary itself might be wrong (e.g., a feature's core logic appears to live entirely in a csproj that wasn't included in scope) - flag this rather than either silently expanding scope or silently documenting an incomplete picture.

Do not stop merely because a claim couldn't be verified - that's handled inline via the `unverified assumption` tag, not a run-stopping event. Only stop for things that block continuing correctly, not things that just require noting a limitation.

## Reporting back

When an AFK run ends (either reason), summarize: how many features processed, how many reached `documented`, how many still open (if endpoint wasn't full completion), any hard blocker encountered, and point to `afk-log.md` and `index.md` for full detail.
