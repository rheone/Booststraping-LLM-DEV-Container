---
name: reverse-engineered-docs
description: Reverse-engineers an existing project from source code, producing structured markdown docs: project overview, DDD domains and update (diff-and-patch) modes. Use when a user asks to document, reverse-engineer, or analyze a codebase; generate technical docs from source; map domains or features of an existing project; or update existing reverse-engineered-docs docs. Trigger phrases: "document this project", "what does this codebase do", "reverse engineer this", "generate docs from source", "map the domains", "document features".
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.1
---

# Reverse Engineer

Produces structured technical documentation from source code. Audience: senior software developers. Documents *what* the system does — not why it was built that way.

When appropriate create diagrams with mermaid.js

## Quick start

1. Detect language, framework, and entry points from project root.
2. Write behavior overview → `docs/reverse-engineered-docs/overview.md`.
3. Propose a domain list — pause for user confirmation (skip in batch mode and document all domains).
4. Document each domain → `domains/<domain-slug>.md`. (if there exists more than 5 domains ask the users if they want to continue with all, or ask which domains they want providing examples)
5. Document each feature → `domains/<domain-slug>/features/<feature-slug>.md`.
6. Write meta files: `_meta/glossary.md`, `_meta/open-questions.md`, `_meta/confidence-summary.md`.

## Modes

**Interactive** (default) — pause after the behavior overview and the domain list for user to confirm, add, remove, or rename before continuing.

**Batch** — triggered by `--batch`, "run all", "walk away", or "do everything":
- Phase 1 Discovery: enumerate all domains and features; write `_meta/run-log.md` with the full plan.
- Phase 2 Generation: spawn one sub-agent per domain; each writes its domain doc and all child feature docs; compress context between domains.
- Phase 3 Meta: aggregate sub-agent outputs into glossary, open-questions, confidence-summary.
- No user input required after batch starts.

**Update** — triggered by `--update [domain]`, "update docs", "refresh", "re-document":
- Top-down: diff-and-patch overview → domains → features.
- Per-domain: re-document a single domain and its features only.
- Preserve High-confidence sections unless their source files changed; annotate changed sections `_Updated: YYYY-MM-DD_`.

## Output structure

```
docs/reverse-engineered/
  overview.md
  domains/
    <domain-slug>.md
    <domain-slug>/
      features/
        <feature-slug>.md
  _meta/
    glossary.md
    open-questions.md
    confidence-summary.md
    run-log.md
```

## Core rules

- **Audience**: senior devs — skip basics, use precise technical language.
- **What not why**: observable behavior only; no design rationale or intent.
- **Domain inference**: from behavior (shared entities, handler clusters, event flows, repository boundaries) — not folder names. A domain may span multiple packages or libraries.
- **Confidence per section**: every section ends with `Confidence: High | Medium | Low`. Below High, add a one-line evidence pointer on the same line. If High, you do not need to include a confidence score.
- **Tests as evidence**: mark clearly — `_Source: tests/orders/OrderService.test.ts:42_ (method name)`.
- **Low → open-questions**: every Low-confidence finding auto-generates an entry in `_meta/open-questions.md` with a specific, answerable question.
- **Sources header**: each doc lists primary source files at top: `_Sources: src/..., tests/..._`.

See [REFERENCE.md](REFERENCE.md) for document templates, confidence rules, and sub-agent instructions.
See [EXAMPLES.md](EXAMPLES.md) for sample output.
