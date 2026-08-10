# Phase 1: Discovery

Goal: build/update `index.md` with every discoverable feature in scope, each
tied to its entry point(s) **and sized**, so deep-dive work can be batched,
tracked, and resumed. This phase writes no feature docs.

Discovery must **finish** before deep-dive work is trusted. An incomplete
discovery produces a false map, and a deep-dive over a false map is confidently
wrong - which is worse than being obviously unfinished.

## What counts as an entry point

Search everything at or below the roots, plus transitive first-party project
references per the scope ledger, plus the SQL definitions folder for DB-side
entry points. Look for:

- **WebForms pages**: `.aspx` and code-behind (`.aspx.cs`/`.aspx.vb`) -
  `Page_Load`, control event handlers, post-back logic
- **User controls**: `.ascx` with meaningful independent behavior (skip purely
  presentational ones)
- **MVC/Web API controllers**: public action methods
- **HTTP handlers**: `.ashx`, custom `IHttpHandler`
- **Web services**: `.asmx`, WCF (`.svc`, `[ServiceContract]`/`[OperationContract]`)
- **Webhooks**: any action/handler receiving inbound external calls. Tag as
  `webhook`; predecessor is `external system: <name/URL>`, not a feature.
  Always document the authentication mechanism (API key, HMAC, IP allow-list,
  or none found).
- **Console applications / batch jobs**: every in-scope `.exe`-type project's
  `Main()`, treated as an entry point like any other
- **Scheduled/background jobs**: Windows Services, HangFire/Quartz jobs
- **In-process scheduled work**: timers or background threads started from
  `Global.asax` (`Application_Start`) - never a real service, functions as one
- **Message queue consumers**: MSMQ, RabbitMQ, Service Bus listeners, any
  queue-polling or subscriber code
- **DB-side entry points**: a stored procedure with **no traceable caller
  anywhere in scope**. Do not assume it is SQL Agent-invoked - job config lives
  in `msdb`, not a definitions folder. Give it its own index row with status
  `candidate orphan/scheduled proc, unconfirmed` and file a `qst-XXXX` asking
  the user what triggers it.

Read each entry point enough to name the feature and state its rough purpose.
Do not trace into the database or resolve business rules - that is Phase 2.

## Parallelizing the sweep

Use `Explore` subagents to fan out - one per root or per major folder, each
prompted with the Session Contract and a specific area to sweep. `Explore` is
read-only, which is right for this phase: it returns locations, and cannot
write anything by accident.

The orchestrator merges the results and is the only writer of `index.md`.

## Sizing

Assign every feature an `S`/`M`/`L` in the `Size` column using the table in
`batching-and-agents.md`. Never leave it blank - an unsized feature cannot be
batched. If unsure, mark `M` and say why in Notes.

## Grouping entry points into features

A feature is a business capability, which may span several pages or handlers
("Order Approval" = a list page, a detail page, and an approve/reject
post-back - one feature, not three). Group on:

- Shared navigation flow
- Shared underlying data/business object
- Folder and naming conventions (`/Billing/` likely groups a set)

When genuinely unsure whether two entry points are one feature or two, keep
them as separate rows, note the possible relationship, and **file a
non-blocking question**. Do not force a merge you are not confident about.

## The scan ledger - tracking partial completeness

This is what makes an interrupted Discovery visible instead of invisible.

Maintain `scan_ledger` in the index frontmatter, one entry per project/folder:

```yaml
scan_ledger:
  - path: src/Web
    files_total: 412
    files_scanned: 412
    state: complete
  - path: src/Acme.Data
    files_total: 131
    files_scanned: 38
    state: partial
```

Rules:

- Update it **as you go**, not at the end. An interrupted run must leave an
  accurate ledger behind.
- `discovery_complete: true` only when every entry is `complete`. Anything else
  is `false`.
- If Discovery ends with any `partial` or `not-scanned` entry, **say so
  explicitly** in the report, name the areas, and give the user the choice to
  continue scanning or proceed anyway.
- Phase 2 warns and names the unscanned areas before starting when
  `discovery_complete: false`. AFK mode finishes Discovery first, always.

## Assigning feature IDs

Every new row gets a stable `feat-XXXX` at the moment it is first added - the
only time an ID is assigned.

- Sequential from the highest existing ID in `index.md`, not row position.
- Never reassigned, reused, or renumbered - even if the feature is later
  renamed, merged, or split. On a split, the original ID stays with the closer
  match; the new half takes the next unused ID.
- The ID belongs to the feature concept, not the file. Renaming the slug does
  not change the ID.
- Unsure whether something is new or an already-indexed feature under another
  name? Do not assign speculatively - flag it as a possible duplicate in Notes
  and resolve it first.

## Not in this phase

- No writes to `features/` or `shared-components/`
- No tracing into stored procedures
- No in-depth business rules, permissions, or edge cases - one line per feature
  is enough

## Output of this phase

Report: entry points found, features grouped, size distribution, scan ledger
state (**explicitly flagging anything partial**), and the suggested first
batches per `batching-and-agents.md`. Append a Discovery entry to `run-log.md`
and run `Test-MapperOutput.ps1 -Mode Index` before handing back.
