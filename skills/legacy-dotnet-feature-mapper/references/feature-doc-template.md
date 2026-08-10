# Feature Doc Template

The template is a **file to copy**, not a description to imitate:

> `templates/feature-doc.md`

Copy it verbatim to `<output-root>/features/<feature-slug>.md`, then fill it in.
Do not compose a doc freehand and hope the structure matches - the structure is
machine-checked, and a freehand doc fails.

Slugs are kebab-case, derived from the feature name, and must equal the
filename without its extension.

## The enforced contract

`references/doc-manifest.md` defines exactly what a valid doc contains -
required frontmatter, required headings, the webhook-only `Authentication`
section, citation rules, and the count-consistency rules. It is validated by
`scripts/Test-MapperOutput.ps1` during Phase 3.

Worth internalizing before writing, because these are the checks that fail most
often:

1. **Every** bullet under `## Business Rules` needs a `file:line` citation
   **and** a confidence tag. Checked per bullet, not in aggregate.
2. `confidence_summary` counts must equal the tag occurrences in the finished
   body - recount after editing, every time.
3. `open_questions_count` must equal the bullets in the open-questions section.
4. `has_diagram: true` if and only if a mermaid block is present; otherwise the
   Diagram section says `No diagram - <reason>`.
5. `Authentication` appears if and only if `trigger_type: webhook`.
6. Cited files must exist and cited line numbers must be within them. An
   invented line number is the failure mode this catches, and it is the one
   that reads most convincingly when nobody checks.
7. No placeholder text from the skeleton survives.

## Omitting a section

Never delete a required section silently. If it genuinely does not apply, keep
the heading and state why - "No diagram - single linear path with no
branching", "None found - no authorization check guards this action". An
explicit absence is a finding; a missing section is a gap.

## Shared-component docs

Same arrangement: copy `templates/shared-component-doc.md`, see
`references/shared-components.md` for when to create one, and the same section
of `doc-manifest.md` for its enforced contract.
