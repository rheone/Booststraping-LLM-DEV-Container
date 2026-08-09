# legacy-dotnet-feature-mapper

Reverse-engineers a legacy .NET Framework 4.8 / .NET 5 WebForms + TSQL
application into a faithful, citable, feature-by-feature map — business
rules, permissions, happy/failure paths, DB behavior, and earned Mermaid
diagrams — using **static analysis only**. Never runs, builds, or executes
the target app. Output is a reference for a future rewrite team so no
feature or business rule is silently lost.

## Structure

```
legacy-dotnet-feature-mapper/
├── SKILL.md                       # Entry point — required inputs, the two
│                                   #   phases, cross-cutting rules, AFK mode
├── README.md                      # This file
└── references/
    ├── kickoff-checklist.md       # Questions to ask when inputs are missing
    ├── discovery-phase.md         # Phase 1 — build/update index.md of features
    ├── deep-dive-phase.md         # Phase 2 — fully document one feature
    ├── feature-doc-template.md    # Structure for a features/<slug>.md doc
    ├── shared-components.md       # When/how to split out reused code or DB objects
    ├── tsql-analysis.md           # Tracing business logic through procs/views/triggers
    ├── citation-and-confidence.md # File:line citations + confidence tagging rules
    ├── index-schema.md            # index.md and system-overview.md formats
    └── afk-mode.md                # Unattended run loop, logging, stop conditions
```

## The two phases

1. **Discovery** — scans in-scope projects for entry points (aspx pages,
   code-behind, MVC/API controllers, `.ashx` handlers, WCF/web services) and
   produces/updates `index.md`, one row per feature. No full docs written.
2. **Deep-dive** — traces one feature (or a named/AFK batch) end-to-end,
   entry point → business logic → DB, and writes `features/<slug>.md` using
   the template, linking out to `shared-components/` docs as needed.

## Required inputs

Always ask for these three explicitly — never assume or auto-discover:

1. In-scope `.csproj` project paths
2. DB object definitions folder (`.sql` scripts)
3. Output docs path

## Output structure

```
<output-docs-path>/
├── index.md                  # master status index
├── system-overview.md        # app-wide map of how features relate
├── features/
│   └── <feature-slug>.md     # one per feature
└── shared-components/
    └── <component-slug>.md   # one per shared class/proc/view/etc.
```

## Quick start

Ask, in the host conversation, to document/map/reverse-engineer/inventory a
legacy WebForms codebase — this skill activates on that intent, or on
requests to run the process "AFK," unattended, or "until done." It will:

1. Ask for the three required inputs if not already given.
2. Run Discovery first if `index.md` doesn't exist yet.
3. Move to Deep-dive per feature (or per AFK endpoint condition), citing
   every claim as `verified in code`, `inferred from naming`, or
   `unverified assumption`.

No modernization advice, no "you should refactor this" — this is a map of
what the app does today, not what it should do.
