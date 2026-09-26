# General Skill Workflow

For anything that isn't organized by version tier: a `dotnet-*` library/BCL/platform/tooling/
testing skill, or a `csharp-*` pattern, architecture, or convention skill. If the subject's
organizing axis is genuinely "what changed at each C# version," use
[version-gated-workflow.md](version-gated-workflow.md) instead.

## New skill

1. **Name it.** Apply [naming-decoder.md](naming-decoder.md). If it doesn't resolve cleanly, stop
   and ask before creating any file.

2. **Scope the subject and verify current facts.** List what the skill covers and what it
   explicitly excludes. For a library or platform API, pin down its current stable release
   version, license (and any dual/commercial licensing model), and minimum supported .NET/C#
   version — verified against a current search, per
   [fact-verification-checklist.md](fact-verification-checklist.md), not recalled from training
   data. Completion: every version/license claim you're about to write has a search result behind
   it from *this* session.

3. **Design the file tree**, organized by task or topic — not by version, since nothing here is
   version-gated:
   - `references/<topic>.md` — one file per coherent topic or task category (e.g. for a mapping
     library: core concepts, member mapping, custom resolvers, testing; for an architectural
     style: philosophy/organization, a worked example, testing).
   - `SKILL.md` and `README.md` at the skill root, each with a routing table by situation/task
     rather than by version.

   Completion: every topic named in Step 2's scope has exactly one reference file; no file mixes
   two unrelated topics just to avoid creating a new one.

4. **Write each reference file.** State the topic's API or technique, the common usage shape, and
   any pitfall or gotcha specific to it. Where a fact is version- or license-sensitive (a feature
   added in a specific release, a licensing threshold), name the version inline rather than
   treating the whole file as version-gated content.

5. **Write the testing reference.** Every skill gets one — per [testability.md](testability.md),
   cover how to test code built with/on this subject (or, per that file's guidance, how to verify
   the work for a convention/structural skill, or how to test the test infrastructure itself for a
   skill about testing tools), plus worked examples for the two or three most likely real-world
   scenarios.

6. **Write `SKILL.md`**: a quick-start example, and a routing table (situation/task → reference
   file). **Write `README.md`** per [readme-template.md](readme-template.md) — a human-facing page
   (summary, when to reach for it, how it's invoked, example prompts), not a mirror of `SKILL.md`'s
   routing table.

7. **Apply insularity.** Read [insularity.md](insularity.md) and apply it to every file: no
   comparing this skill to another, no naming another skill without an explicit, stated dependency.

8. **Apply the writing style.** Read [writing-style.md](writing-style.md) and check every file
   against it: present tense, active voice, second person, no delta-narrative.

## Extending an existing general skill

Repeat Steps 2–8 scoped to just the new topic: verify its facts fresh (a library's current release
may have moved since the skill was first written), add its reference file, update both routing
tables, and re-check any existing file whose license/version claim the new information might have
made stale — the same staleness risk [maintenance-workflow.md](maintenance-workflow.md) describes
for version-gated skills applies here too, just triggered by a new release rather than a new C#
version.
