# Phase 1: Discovery

Goal: build/update `index.md` with every discoverable "feature" in the in-scope projects, each tied to its entry point(s). This phase does NOT write full feature docs - it's a map of what exists and where to start, so deep-dive work can be planned and tracked.

## What counts as an entry point

Search the in-scope `.csproj` files' project directories (and their referenced local dependencies if those are also in scope - do not follow references into out-of-scope projects), plus the DB definitions folder for DB-side entry points. Look for:

- **WebForms pages**: `.aspx` files and their code-behind (`.aspx.cs`/`.aspx.vb`), especially `Page_Load`, button/control event handlers, and post-back logic
- **User controls**: `.ascx` with meaningful independent behavior (skip pure presentational controls with no logic)
- **MVC/Web API controllers**: public action methods (if the solution mixes WebForms with MVC/Web API, which is common in .NET 4.8/5 migrations)
- **HTTP handlers**: `.ashx` files, custom `IHttpHandler` implementations
- **Web services**: `.asmx`, WCF service contracts (`.svc`, `[ServiceContract]`/`[OperationContract]`)
- **Webhooks**: any controller action/handler that receives inbound calls from an external system. Tag these explicitly as `webhook` in the doc. Unlike normal entry points, their "predecessor" is external, not another feature - record it as `external system: <name/URL if known>`. Always document the authentication mechanism (API key, HMAC signature, IP allow-list, none found).
- **Console applications / batch jobs**: every in-scope `.exe`-type csproj's `Main()` method, treated as an entry point automatically, same as any web-facing one - even though it's invoked externally (Task Scheduler, a wrapper script, etc.) rather than by a user.
- **Scheduled/background jobs**: Windows Services, `HangFire`/`Quartz`-style jobs, and any other explicit scheduler present in scope
- **In-process scheduled work**: timers or background threads started from `Global.asax` (`Application_Start`) or similar app-lifecycle hooks - e.g. a `System.Timers.Timer` - which never appear as a real Windows Service but function as one
- **Message queue consumers**: MSMQ, RabbitMQ, Azure/AWS Service Bus listeners, or any other queue-polling/subscriber code
- **DB-side entry points (SQL Agent jobs)**: a stored procedure invoked purely on a schedule from SQL Server Agent, with no C# caller at all. These aren't discoverable from `.csproj` code or reliably from a typical schema-dump folder (job config usually lives in `msdb`, not the definitions folder). Do not assume a proc is SQL Agent-invoked. Instead: **if a stored procedure appears to have no traceable caller anywhere in the in-scope code**, flag it as a *candidate orphan/scheduled proc, unconfirmed* - its own index row, status noted as needing user confirmation - rather than guessing at its trigger.

For each entry point found, do a quick (not deep) read to understand its rough purpose - enough to name the feature and identify what business capability it serves. Do not trace into the database or fully resolve business rules yet - that's Phase 2.

## Grouping entry points into features

Remember: a feature is a business capability/use case, which may span multiple pages/entry points/workflows (e.g., "Order Approval Workflow" might involve a list page, a detail page, and an approve/reject post-back handler - that's one feature, not three). Use judgment based on:

- Shared navigation flow (pages that link to each other in sequence)
- Shared underlying data/business object
- Naming/folder conventions in the app (a `/Billing/` folder likely groups a related set)

When genuinely unsure whether two entry points are one feature or two, list them as separate index rows but note the possible relationship - don't force a merge you're not confident about. This can be resolved during deep-dive.

## Building/updating index.md

Format is defined in `index-schema.md`. For a first-time discovery run, create the file. For a subsequent run (more of the codebase now in scope, or re-scanning), add new rows and update entry-point info for existing ones - do not remove or overwrite `status` for features already `documented` or `in progress` just because they were re-discovered.

## Assigning feature IDs

Every new row added to `index.md` gets a stable `feat-XXXX` ID at the moment it's first added - this is the *only* time a feature ID is assigned. Rules:

- IDs are sequential (`feat-0001`, `feat-0002`, ...) based on the highest existing ID already in `index.md`, not on row position or alphabetical order.
- Never reassign, reuse, or renumber an ID - even if the feature is later renamed, merged with another, or split into two. If a feature is split during deep-dive, the original ID stays with whichever half is the closer match, and the new half gets the next unused ID.
- The ID belongs to the feature concept, not the doc file - if `features/<slug>.md` is later renamed (slug changes), the `id` field inside its frontmatter does not change.
- If you're unsure whether something newly found is a genuinely new feature or an already-indexed one under a different name, don't assign a new ID speculatively - flag it as a possible duplicate in the index Notes column instead, and resolve it before assigning.

## What NOT to do in this phase

- Don't write anything to `features/` or `shared-components/` yet.
- Don't trace into stored procedures or SQL definitions yet.
- Don't resolve business rules, permissions, or edge cases in depth - a one-line "what this looks like it does" is enough for the index; full rigor is Phase 2's job.

## Output of this phase

Report back to the user: how many entry points found, how many candidate features grouped, and the updated `index.md`. Suggest logical next batches for deep-dive if asked.
