# Insularity

**Rule:** every skill you produce is insular. Unless the person commissioning it explicitly states
a dependency, the skill reasons about its own job only — it does not compare itself to another
skill, and it does not assume any other skill is present, absent, installed, or missing. Write
each file as if it is the only skill that will ever exist in the agent's context: name what the
file itself covers and what it excludes, never what "belongs to" or "is covered by" something
else.

## No comparison

Never frame this skill's scope in terms of another skill — not by name, not by category, not by
implication. Banned shapes, regardless of phrasing:

- *"unlike this repo's other X skills..."*
- *"...belongs to another skill"* / *"...belongs to that framework's own domain, not this skill"*
- *"if you also have Y installed, it covers this in more depth"*

State the scope boundary on its own terms instead: name what's out of scope and, if useful, *why*
(too deep a domain of its own, a different failure-mode class, orthogonal to this file's task) —
without naming or gesturing at whatever skill might or might not cover it elsewhere. "Out of
scope: P/Invoke and marshaling attributes — a narrow, deep domain of its own" is fine. "...covered
by the marshaling skill" is not, even if such a skill exists.

## No naming, unless it's a real dependency

For every place you're about to name another skill, ask: **does this file's own job require that
skill's content to exist?**

- **No — drop the name.** Overlap, a related pattern, "you might also like" — none of that is a
  dependency. Either inline whatever's actually needed so the file stands alone, or say nothing.
  Do not soften a non-dependency into a "soft pointer" — a soft pointer still couples two skills by
  name, which insularity rules out for anything short of a real, explicitly stated dependency.
- **Yes — the person commissioning this skill explicitly said so.** Name it, and give the skill a
  presence check that runs before it relies on that dependency: state which file or skill name to
  look for (e.g. `skills/<name>/SKILL.md`, or however the host exposes installed skills), and
  instruct the skill to **halt and tell the user the dependency is missing** rather than proceeding
  without it, guessing at its content, or degrading silently. Don't inline a fallback copy of the
  dependency's content as a substitute for the check — that just reintroduces the coupling as stale
  duplication instead of removing it.

## Applies both directions

- **Within the same repo**: sibling skills don't ship together by assumption. A skill extracted on
  its own, or a host that installs skills individually, must not break — or read strangely — because
  it was written assuming an unresolvable name or a sibling's absence would always hold true.
- **Outside the repo entirely**: global or marketplace skills are never assumed present or absent.
  Don't name one, and don't shape guidance around whether one might be installed, unless the person
  commissioning this skill stated a real, checked dependency on it.

## Applying this when scaffolding output

Every reference file, specialized file, `SKILL.md`, and `README.md` you generate is subject to this
rule on its own — audit each file independently, not the skill as a whole. A file that happens to
sit beside content covering a related pattern (extension methods next to generics, mocking
libraries next to test-authoring patterns) does not thereby gain a dependency on it, or license to
compare itself against it, and must not name it.
