# README Template

Every skill's `README.md` is written for a human deciding whether to reach for the skill and how
to use it — not a mirror of `SKILL.md`'s routing table, and not a copy of the frontmatter
`description` (that field is dense and keyword-packed for the agent's own matching; a human reads
prose). Every skill in this repo's README follows this same shape:

```markdown
# <Human-readable title>

<One to three sentences, in your own words: what this skill helps with and when it applies.>

## When to reach for it

- <A concrete scenario or symptom that should bring this skill to mind>
- <Another one>
- <A third, if there's a genuinely distinct trigger — don't pad to hit a count>

## Using it

<One or two sentences: model-invoked skills fire automatically when the situation matches: name>
<a user-invoked skill by its `/skill-name` instead. If relevant, note the skill installs as part of
this repository's skill set.>

## What it covers

| Topic | Reference |
| --- | --- |
| <topic a reader would search for> | [references/<file>.md](references/<file>.md) |

## Example prompts

- "<a realistic thing someone would actually type to trigger or use this skill>"
- "<another>"
- "<a third>"
```

## Section-by-section guidance

- **Title**: the skill's subject in plain words, not its slug (`AutoMapper`, not `dotnet-automapper`).
- **Opening summary**: write it fresh. Reusing the frontmatter description verbatim fails a human
  reader — it's written for keyword matching, not comprehension, and duplicating it here means two
  places to keep in sync for no benefit.
- **When to reach for it**: scenario-shaped, not feature-shaped — "deciding whether a query is
  IEnumerable or IQueryable" reads better than "covers IEnumerable and IQueryable."
- **Using it**: state whether the skill is model-invoked or user-invoked (matches its frontmatter's
  `disable-model-invocation`), and give the install context only where it isn't already obvious.
- **What it covers**: a table is the right shape once a skill has enough reference files that a
  reader benefits from seeing them at a glance (roughly four or more); for a smaller skill, a short
  prose list is fine and a table would be padding.
- **Example prompts**: realistic, first-person phrasings a reader would actually type — not
  a restatement of the routing table's "you're doing this..." column.
- **Non-standard license flag**: if [fact-verification-checklist.md](fact-verification-checklist.md)'s
  licensing policy applies, put the one-line flag in a `> [!NOTE]` alert near the top of the README,
  after the opening summary — visible before a reader gets invested, not buried at the bottom.

## Writing style still applies

[writing-style.md](writing-style.md) governs here too: present tense, active voice, second person,
and — specifically for a README — describe the skill's *current* shape. A README is read by a human
who wasn't there for any previous version; a rename, a restructuring, or "this used to work
differently" belongs in a commit message, never in the page a reader opens today.
