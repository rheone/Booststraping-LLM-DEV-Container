# Doc Manifest - the enforced contract for every generated document

This is the machine-checkable definition of a valid doc. `scripts/Test-MapperOutput.ps1`
encodes the same rules; **the two must be kept in sync** - if you change one,
change the other, or the script silently grades against stale rules.

Templates are not suggestions. Copy the skeleton from `templates/`, then fill
it. Do not compose a doc freehand and hope it matches.

## Feature doc (`features/<slug>.md`)

Skeleton: `templates/feature-doc.md`

### Required frontmatter keys

`id`, `slug`, `title`, `status`, `trigger_type`, `domain`, `entry_points`,
`last_updated`, `source_snapshot`, `confidence_summary`, `has_diagram`,
`open_questions_count`

Also expected but not hard-failed: `csprojs`, `db_objects`, `predecessors`,
`successors`, `related_shared_components`.

### Required H2 headings, verbatim and in this order

1. `Purpose`
2. `Predecessors / Successors`
3. `Roles / Permissions`
4. `Happy Path`
5. `Business Rules`
6. `Failure States`
7. `Database Interactions`
8. `Diagram`
9. `Open Questions / Unverified Items`
10. `Related Shared Components`

### Conditional section

- `Authentication` - present **if and only if** `trigger_type: webhook`.
  Including it on a non-webhook doc fails, as does omitting it on a webhook.

## Shared-component doc (`shared-components/<slug>.md`)

Skeleton: `templates/shared-component-doc.md`

### Required frontmatter keys

`id`, `slug`, `name`, `component_type`, `location`, `domain`, `last_updated`,
`source_snapshot`, `confidence_summary`, `used_by`, `open_questions_count`

### Required H2 headings, verbatim

1. `What it does`
2. `Inputs / Outputs`
3. `Business rules embedded here`
4. `Side effects`
5. `Confidence / Open Questions`

## Rules applied to both doc types

| # | Rule | Why it exists |
|---|---|---|
| 1 | File exists on disk at the expected path and is at least 20 lines | A doc that only exists in an agent's report is not a deliverable |
| 2 | `id` matches `feat-NNNN` / `comp-NNNN` | Stable identity across renames |
| 3 | `slug` equals the filename without extension | Prevents index/doc drift |
| 4 | No template placeholder text survives (`<Feature Name>`, `feat-XXXX`, `Rule description`, ...) | A half-filled skeleton is the most common ghost-completion artifact |
| 5 | `confidence_summary` counts exactly equal the occurrences of each tag string in the body | Frontmatter must describe the doc as it is *now*, not as it was |
| 6 | At least one confidence tag appears in the body | An untagged doc has made no citable claims |
| 7 | `open_questions_count` equals the number of bullets in the open-questions section | Same reason as #5 |
| 8 | `has_diagram: true` iff a ```mermaid block is present; if absent, the Diagram section states `No diagram - <reason>` | Makes "diagrams are earned" auditable instead of silently skipped |
| 9 | At least 3 `file:line` citations in the doc | Below that, nothing was really traced |
| 10 | **Every** bullet under Business Rules carries both a `file:line` citation and a confidence tag | The core rule of this skill, checked per bullet rather than in aggregate |
| 11 | Every cited path resolves to a real file under a source root | Catches invented file paths |
| 12 | Every cited line number is within that file's actual line count | Catches invented line numbers - the most convincing kind of fabrication |
| 13 | `status`, where present, is one of the six valid values | Keeps the index parseable |

## index.md rules

| # | Rule |
|---|---|
| 1 | Frontmatter contains `last_updated`, `discovery_complete`, `feature_counts`, `scope_ledger`, `scan_ledger` |
| 2 | Every row's first column is a unique `feat-NNNN` |
| 3 | Every row's status is one of the six valid values |
| 4 | **Any row whose status asserts completion (`documented`, `documented (open questions)`, `verification failed`) links to a doc that exists on disk** |
| 5 | `feature_counts` match the actual counted rows |
| 6 | Every file in `features/` corresponds to the row that links to it |

Rule 4 is the ghost-completion check. It is the single most important line in
this file: it makes "I marked it done" and "a document exists" the same claim.

## Valid status values

```
not started
in progress
documented
documented (open questions)
verification failed
candidate orphan/scheduled proc, unconfirmed
```

## Manual fallback checklist

Use this **only** when `Test-MapperOutput.ps1` could not run (see
`verification-phase.md`). Walk it item by item, per doc, and record in
`run-log.md` that verification was manual and why.

- [ ] File exists at `features/<slug>.md` / `shared-components/<slug>.md`, opened and read back after writing
- [ ] All required frontmatter keys present and non-empty
- [ ] All required H2 headings present, spelled exactly as above
- [ ] `Authentication` present iff webhook
- [ ] No placeholder text left anywhere
- [ ] Counted the three confidence tags in the body; they match `confidence_summary`
- [ ] Counted open-question bullets; they match `open_questions_count`
- [ ] Diagram present with `has_diagram: true`, or an explicit `No diagram - <reason>`
- [ ] Every Business Rules bullet has a citation and a tag - checked bullet by bullet
- [ ] Opened at least 3 cited files at the cited lines and confirmed the line exists
- [ ] Index row status matches the doc, and the row links to the file just written
