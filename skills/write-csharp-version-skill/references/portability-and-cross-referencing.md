# Portability and Cross-Referencing

**Rule:** a skill must not name another skill unless the current file has a direct, necessary
dependency on it — content it cannot produce or explain without that other skill's material
actually being present. Skills are portable: installed individually, copied out of a repo on
their own, or run in an environment that doesn't have any particular sibling installed. Naming
another skill creates a coupling the portability model doesn't allow, whether or not the mention
is framed as optional.

## The test

For every place you're about to name another skill, ask: **does this file's own job require that
skill's content to exist?**

- **No — drop the name.** Overlap, a related pattern, "you might also like" — none of that is a
  dependency. Either inline whatever's actually needed so the file stands alone, or say nothing.
  Do not soften a non-dependency into a "soft pointer" (*"if you also have X installed, it covers
  this in more depth"*) — that used to be this file's guidance and it is retired. A soft pointer
  still couples two skills by name, which is exactly what portability rules out for anything short
  of a real dependency.
- **Yes — it's a hard dependency.** Name it, and give the skill a presence check that runs before
  it relies on that dependency: state which file or skill name to look for (e.g. `skills/<name>/SKILL.md`,
  or however the host exposes installed skills), and instruct the skill to **halt and tell the
  user the dependency is missing** rather than proceeding without it, guessing at its content, or
  degrading silently. Don't inline a fallback copy of the dependency's content as a substitute for
  the check — that just reintroduces the coupling as stale duplication instead of removing it.

## Applies both directions

- **Within the same repo**: sibling skills don't ship together by assumption. A skill extracted on
  its own, or a host that installs skills individually, must not break because it was written
  assuming an unresolvable name would always be there.
- **Outside the repo entirely**: global or marketplace skills (`write-a-skill`, `writing-for-agents`,
  a companion test-framework skill) are never assumed present. Don't name one unless the current
  file has a real, checked dependency on it.

## Applying this when scaffolding output

Every reference file, specialized file, `SKILL.md`, and `README.md` you generate is subject to
this rule on its own — audit each file independently, not the skill as a whole. A file that
happens to sit beside content covering a related pattern (extension methods next to generics,
mocking libraries next to test-authoring patterns) does not thereby gain a dependency on it, and
must not name it.
