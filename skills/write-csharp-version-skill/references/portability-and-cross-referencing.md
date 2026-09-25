# Portability and Cross-Referencing

A link from one skill to another — a sibling in the same repo, or an unrelated skill like
`write-a-skill` — is a **soft pointer**: worded as optional enrichment, never as something the
reader needs in order to finish understanding the current file. Skills get installed
individually, copied out of a repo on their own, or run in an environment that simply doesn't
have the target installed; a link that quietly carries essential meaning becomes a dead end for
anyone in that situation.

## The test

For every link to another skill, ask: **if this link resolved to nothing, would the current file
still make complete sense?** Two outcomes:

- **Yes — it's supplementary.** A "see also," a comparison to a related pattern, a pointer to
  more depth than the current file needs to provide. The soft pointer alone is fine:
  *"...if you also have the `csharp-union` skill installed, it covers generic case types in
  unions in full."*
- **No — it's essential.** The current file's own explanation genuinely depends on content that
  lives only in the other skill. Don't leave it purely behind the link: inline a short,
  self-contained version of the essential part — a paragraph, one example — so the file stands on
  its own, and keep the link only for whoever wants the fuller treatment.

This mirrors the ordinary rule against duplication, inverted for a specific reason: duplication
is wasteful when the original is reliably reachable. A link to a possibly-absent skill is not
reliably reachable, so the "single source of truth" it would otherwise point to needs a cached,
load-bearing copy of its essential content sitting locally instead — the same reasoning that
justifies caching a fact the agent "cannot find by looking."

## Applies both directions

- **Within the same repo**: sibling skills usually get installed together, but not always —
  someone may extract one skill's folder on its own, or a future install mechanism may support
  picking individual skills from a multi-skill repo. Don't assume co-installation just because
  the files currently sit side by side in the same `skills/` directory.
- **Outside the repo entirely**: global or marketplace skills (`write-a-skill`,
  `writing-for-agents`, a companion test-framework skill) may not exist in another person's
  install at all. Every reference to one of these needs the same soft-pointer treatment, and
  anything essential needs the same inline fallback.

## Wording

Prefer explicit conditionals over bare links: "if you also have the X skill installed" or
"X, if installed, covers this in more depth" — not a bare `[X](../x/SKILL.md)` link with no
framing, which reads as a required next step rather than an optional one.
