# Kickoff Checklist

Use this when required inputs are missing or ambiguous at the start of a session. Ask only what's actually missing - don't re-ask things already given.

## The three required inputs

1. **In-scope `.csproj` paths.** Ask: "Which project(s) should I look at for this session? Please give me the `.csproj` path(s)." Never guess scope from folder structure or "looks related" heuristics - an explicit list only. If the user gives a folder instead of specific csproj files, ask them to confirm which projects within it are actually in scope, or confirm "all csproj files directly under this folder."

2. **DB object definitions folder.** Ask: "Where are the `.sql` definition scripts for tables/procs/views/triggers/functions?" If the folder contains scripts for objects outside the app's actual schema (e.g., a shared enterprise DB dump), ask whether to filter by a naming pattern/schema prefix.

3. **Output docs path.** Ask: "Where should I write the documentation output?" Confirm whether an `index.md` already exists at that path - if so, this is a continuation of prior work, not a fresh start; read the existing index before doing anything else.

## Also confirm if ambiguous

- **Phase**: Discovery (build/update the feature inventory) vs. Deep-dive (fully document specific features)? If the output path has no `index.md` yet, Discovery must run first regardless of what the user asked for - explain this rather than skipping straight to deep-dive with nothing to deep-dive into.
- **Deep-dive target**: which feature(s) or entry point(s) from the index to document this session, if not already stated.
- **AFK mode**: if requested, get the explicit endpoint condition (see `afk-mode.md`) before starting - this is a hard requirement, not optional.
- **Regenerate vs. incremental** on an existing feature doc - always ask live (not in AFK mode; see `afk-mode.md` for the AFK default).

Do not proceed with analysis while any of the three required inputs are unresolved.
