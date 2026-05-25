# Reverse Engineer — Reference

## Document templates

### overview.md

```md
# Project Overview

_Sources: <comma-separated primary source files>_

## Summary
[2–4 sentences: what the system does, who uses it, and its primary integration points]

## Technology stack
| Layer | Technology |
|-------|-----------|
| ...   | ...       |

## Entry points
[API routes / CLI commands / UI entry points that represent system boundaries]

## Domain map
| Domain | Responsibility |
|--------|---------------|
| ...    | ...           |

## Key data flows
[3–5 most important request/event flows, numbered, one per line]

Confidence: High | Medium | Low [— evidence pointer if below High]
```

---

### domains/\<domain-slug\>.md

```md
# <Domain Name>

_Sources: <files>_

## Responsibility
[One paragraph: what this domain owns and enforces; what invariants it maintains]

## Key entities
| Entity | Role |
|--------|------|

## Operations
| Operation | Trigger | Effect |
|-----------|---------|--------|

## Integrations
[Other domains or external systems this domain depends on or publishes to; direction of dependency]

## Features
- [feature-name](features/feature-slug.md) — one-line description

Confidence: High | Medium | Low [— evidence pointer if below High]
```

---

### domains/\<domain-slug\>/features/\<feature-slug\>.md

```md
# <Feature Name>

_Sources: <files>_

## What it does
[What input triggers it, what state it reads, what it produces or changes]

## Entry point
[HTTP route / command / event / UI action — be exact]

## Steps
1. ...
2. ...

## Outputs / side effects
[Return value, emitted events, DB writes, external calls — observable effects only]

## Edge cases and constraints
[Validation rules, error conditions, limits — only what is observable from code or tests]

Confidence: High | Medium | Low [— evidence pointer if below High]
```

---

### \_meta/glossary.md

```md
# Glossary

Project-specific, domain-specific, or non-obvious terms for a senior developer unfamiliar with this codebase.

| Term | Definition | Source |
|------|-----------|--------|
```

---

### \_meta/open-questions.md

```md
# Open Questions

Items the skill could not determine with High or Medium confidence.
Each question should be answerable by a human with access to original authors or product specs.

| # | Domain | Question | Confidence | Evidence |
|---|--------|---------|-----------|---------|
```

---

### \_meta/confidence-summary.md

```md
# Confidence Summary

## Roll-up by domain

| Domain | High | Medium | Low | Notes |
|--------|------|--------|-----|-------|

## Items requiring human review
[All Low-confidence items with links to their source documents]

## Coverage gaps
[Known areas of the codebase not documented in this run]
```

---

### \_meta/run-log.md

```md
# Run Log

## Last run
- Date: YYYY-MM-DD
- Mode: batch | interactive | update
- Domains discovered: N
- Features discovered: N

## Discovery plan
### Domains
- [ ] domain-slug — brief description of inferred responsibility

### Features per domain
- [ ] domain-slug/feature-slug — brief description

## Generation status
| Document | Status | Notes |
|----------|--------|-------|
```

---

## Confidence scoring

| Level | Meaning |
|-------|---------|
| **High** | Directly readable from code — types, signatures, explicit logic, documented constants, or covered by tests |
| **Medium** | Inferred from naming, structure, or test behavior — plausible but not directly confirmed |
| **Low** | Speculative — based on convention, partial evidence, or absence of contrary signals |

**Evidence pointer format** (required for Medium and Low, on the same line):

```
Confidence: Medium — inferred from handler naming; no tests exercise this path.
Confidence: Low — no explicit validation found; assumed from similar handlers in same module.
```

**Test-as-evidence format** — place before the affected statement or at section end:

```
_Source: tests/orders/OrderService.test.ts:142 — "should reject orders exceeding $50,000 limit"_
Confidence: High
```

**Auto-promotion rule**: every Low-confidence finding must generate an entry in `_meta/open-questions.md` with a specific question (not a generic "unclear"). Bad: "Unclear how this works." Good: "Does CancelOrder release the inventory hold synchronously or via a compensating event?"

---

## Domain inference heuristics

Infer domains from behavior, not folder structure. A domain may span multiple packages, libraries, or services.

**Signals to look for:**

1. **Shared entities** — types or classes referenced by multiple handlers, services, or repositories
2. **Handler clusters** — groups of controllers, resolvers, or command handlers acting on the same aggregate root
3. **Event ownership** — which module publishes vs. consumes domain events
4. **Repository / DB boundaries** — distinct table prefixes, separate schemas, or isolated repository classes
5. **API route groups** — URL prefix groupings or GraphQL type groupings that cluster around an entity
6. **Exclusive external integrations** — a module that exclusively owns calls to a third-party service

**Anti-patterns to avoid:**

- One domain per file or per folder — group by behavioral cohesion, not file proximity
- Naming domains after technical layers (e.g., "controllers", "services", "repositories")
- Splitting a bounded context because it lives in two packages

---

## Sub-agent delegation (batch mode)

After the discovery phase, spawn one sub-agent per domain. Brief each agent with:

- Domain name and inferred responsibility
- List of features to document
- Primary source file paths (not full file content — let the agent read)
- Output paths it must write

Each domain agent:
1. Reads source files and tests in its assigned scope.
2. Writes `domains/<domain-slug>.md` and all `domains/<domain-slug>/features/<feature-slug>.md`.
3. Appends Low-confidence items to `_meta/open-questions.md` (append; do not overwrite).
4. Returns a confidence roll-up table row for `_meta/confidence-summary.md`.

After all domain agents complete, the orchestrating agent:
1. Reads all confidence roll-up rows and writes `_meta/confidence-summary.md`.
2. Aggregates terms across domain docs into `_meta/glossary.md`.
3. Writes `_meta/run-log.md` with final generation status.

**Context management**: compress full source file content out of context between sub-agents. Pass file paths, not file content, in agent briefs.

---

## Update mode — diff-and-patch rules

When re-documenting an existing file:

1. Read the existing doc.
2. For each section, check if the underlying source files have changed since the doc was written.
3. If unchanged and confidence is High — keep the section verbatim.
4. If changed or confidence is Medium/Low — rewrite the section and append `_Updated: YYYY-MM-DD_`.
5. Never silently drop a section that previously existed — if it no longer applies, mark it `_Removed: YYYY-MM-DD — [reason]_`.

When run in per-domain update mode, do not touch documents outside the target domain's tree.
