# Questions and Deferral

An unattended run that stops on the first ambiguity wastes hours of available
work. An unattended run that silently guesses produces a map nobody can trust.
The resolution: **file the question, note the gap in the doc, and keep going.**

## When to file a question instead of stopping

File a `qst-XXXX` and continue whenever the run hits something only the user
can settle:

- A stored proc with no traceable caller (candidate SQL Agent job)
- A genuinely ambiguous feature boundary - one feature or two?
- A config/feature-flag value that changes behavior and is not in the repo
- A call into a project the user excluded from scope at kickoff
- Two entry points that may be duplicates of the same capability
- Business intent that the code cannot reveal ("is this dead code or seasonal?")

Do **not** file a question for something a confidence tag already handles.
Dynamic SQL that cannot be resolved statically is an `unverified assumption` in
the doc, not a question for the user.

## Blocking vs non-blocking

| | Blocking | Non-blocking |
|---|---|---|
| Meaning | The answer invalidates analysis already written | The answer adds detail to analysis that stands |
| Example | "Is this one feature or two?" | "Which team owns this webhook?" |
| Feature status | `documented (open questions)` | `documented` |
| On answer | Feature returns to `in progress` and is re-derived in the next batch | Answer is patched into the existing doc in place |

Mark blocking sparingly. Over-marking turns every answer into a re-run.

## The file

`<output-root>/questions-for-user.md`, from `templates/questions-for-user.md`.
IDs are `qst-XXXX`, sequential, assigned once, never reused - same rule as
`feat-XXXX`.

Every question carries: status, blocking flag, affected feature ids, when it
was raised, the context (with citation) so the user does not have to re-read
the code, the question itself, and **multiple-choice options wherever the
possibilities are enumerable** - answering by picking a letter is far faster
than composing prose.

The affected feature doc also gets a line in its Open Questions section
pointing at the `qst-XXXX`, so a reader of the doc alone still sees the gap.

## Reconciliation - the half that is easy to forget

**At the start of every session, before any other work**, read
`questions-for-user.md` and process every entry with `Status: answered`:

1. Apply the answer to the affected doc(s).
2. **Non-blocking:** patch in place, update `last_updated`, re-run the
   confidence and open-question counts, re-verify the doc.
3. **Blocking:** set the feature's index status back to `in progress`, and put
   it at the front of the next deep-dive batch for re-derivation. Note in the
   run log that the re-run was caused by `qst-XXXX`.
4. Set the question's `Status:` to `applied` and stamp `Applied:` with the
   timestamp.
5. Report what was reconciled in the opening status message of the session.

A questions file nobody folds back in is a graveyard. Reconciliation is what
makes deferral legitimate rather than an excuse.

## At the end of an unattended run

The final report must tell the user, plainly, that answers are needed:

```
Run finished. 34 of 41 features documented.
7 questions need your answers -> docs/legacy-map/questions-for-user.md
  3 are blocking (feat-0009, feat-0021, feat-0033 will be re-derived once answered)
  4 are non-blocking (answers will be patched into the existing docs)
Answer them in the file, then re-run the skill - reconciliation happens automatically.
```
