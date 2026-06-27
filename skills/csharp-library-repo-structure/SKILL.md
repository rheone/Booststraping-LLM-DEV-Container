---
name: csharp-library-repo-structure
description: Bootstraps, audits, and refactors the file/folder layout of a .NET (C#) library repo meant for NuGet distribution. Use whenever the user wants to scaffold a new .NET library repo (asks setup questions like project name, license, target framework); wants an existing .NET repo's structure reviewed, audited, or "cleaned up" to match a consistent layout; mentions stray/committed build artifacts (bin/, obj/, .vs/, TestResults/, crash dumps) cluttering a repo; is missing files or wants broken relative paths fixed in .csproj, sln, slnx, nuget.config. Also trigger for "set up a new C# library like my other repos" or making multiple .NET repos structurally consistent.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 0.0.0
---

# .NET library repo structure

This skill bootstraps new .NET library repos, audits/refactors existing ones
against a canonical layout, fills in missing standard files with working
stubs, and finds + fixes broken path references. It exists because these are
genuinely mechanical, repetitive tasks across many repos -- the value is in
doing them the *same* way every time, not in creative judgment per repo.

## Source of truth

`references/canonical-structure.json` is the manifest every script reads.
`references/canonical-structure.md` is the same information as prose with
the reasoning behind the less-obvious choices (casing rule, why `.sln`/`.slnx`/
`.csproj` are never templated, what "optional" means). **Read the `.md` file
once at the start of a session** to understand the shape; consult the `.json`
only when you need exact paths/groups programmatically.

If the user's situation calls for a structural choice that conflicts with
the manifest, that's a real signal the manifest should evolve -- don't
silently freelance a one-off layout. Discuss it with the user, and if they
confirm, update `canonical-structure.json`/`.md` together so the next run of
this skill reflects the decision instead of repeating the disagreement.

## Three entry points, one shared engine

| User wants | Workflow | Key script(s) |
|---|---|---|
| A brand-new repo from scratch | [Bootstrap](#bootstrap-a-new-project) | `apply_plan.py` |
| An existing repo brought in line | [Refactor](#refactor-an-existing-repo) | `scan_repo.py` → `apply_plan.py` → `audit_paths.py` |
| Just fill in what's absent, repo otherwise fine | [Fill gaps](#add-missing-files-only) | `scan_repo.py` → `apply_plan.py` |

All three ultimately produce a **plan** (a JSON list of actions) that
`apply_plan.py` executes. This indirection is deliberate: it gives you one
place to show the user *exactly* what will happen before anything is
written, moved, or deleted, and one safety mechanism (dry-run by default)
that covers every workflow instead of three bespoke ones.

**Never call `apply_plan.py --execute` without showing the dry-run output
(or the plan itself) to the user first.** A plan can move dozens of files
and rewrite `.gitignore`; that deserves a glance before it happens, the same
way you wouldn't `git push --force` on someone's repo unprompted.

---

## Bootstrap a new project

### 1. Gather inputs

Ask the user directly (use a quick-question/elicitation tool if your
environment has one) -- don't guess at these, they're cheap to ask and
expensive to get wrong after the fact:

- **Library name** (PascalCase, becomes the namespace, assembly name, and
  NuGet package ID unless they say otherwise)
- **License** (MIT / Apache-2.0 / BSD-3-Clause / proprietary / other)
- **Target framework(s)** -- single (`net9.0`) or multi-target
  (`net8.0;net9.0`)? Multi-targeting matters because it changes
  `Directory.Build.props` and the test/benchmark project setup.
- **Test framework** (xUnit / NUnit / MSTest)
- **Which optional components** to include -- map directly to the `group`
  values in the manifest: `benchmarks`, `smoketests`, `devcontainer`,
  `husky`, `agent_files`, `multi_project`. Don't default to "all of them" 
  -- a small internal utility library doesn't need
  BenchmarkDotNet or a public CODE_OF_CONDUCT process but should be offered.
- **GitHub org/repo name** and **author/maintainer name** (used in badges,
  CODEOWNERS, RepositoryUrl, contact emails in SECURITY.md)

Also determine the **.NET SDK version to pin** in `global.json`. Don't
hardcode a guessed version into the plan -- if you have shell access, run
`dotnet --list-sdks` and use what's actually installed; otherwise ask the
user. A stale or invented SDK version in `global.json` will silently break
every build until someone notices.

### 2. Generate the real .NET artifacts via the SDK, not text templates

`.slnx`, `.sln`, `.csproj`, and `packages.lock.json` are **not** in
`assets/templates/` on purpose (see `canonical-structure.md` for why).
Build a plan with `"action": "run"` steps that shell out to the `dotnet`
CLI, in this order:

```
dotnet new sln -n {{LibraryName}} -o src
dotnet new classlib -n {{LibraryName}} -o src/{{LibraryName}} -f {{TargetFrameworks}}
dotnet new <xunit|nunit|mstest> -n {{LibraryName}}.Tests -o src/{{LibraryName}}.Tests
dotnet sln src/{{LibraryName}}.sln add src/{{LibraryName}}/{{LibraryName}}.csproj src/{{LibraryName}}.Tests/{{LibraryName}}.Tests.csproj
dotnet add src/{{LibraryName}}.Tests/{{LibraryName}}.Tests.csproj reference src/{{LibraryName}}/{{LibraryName}}.csproj
```

Add `{{LibraryName}}.Benchmarks` (console + BenchmarkDotNet package
reference) and `{{LibraryName}}.Abstractions` the same way, only if those
groups were selected. After the projects exist, run
`dotnet restore --use-lock-file` from `src/` to generate
`packages.lock.json` per project rather than hand-authoring it.

### 3. Layer the templated files on top

Build `"create_from_template"` actions for every manifest entry whose
`template` field is non-null and whose `group` is either `null` or in the
selected groups. Pass a single `vars.json` (LibraryName, Namespace, Author,
Year, License, GitHubOrg, GitHubRepo, TargetFrameworks, DotnetSdkVersion,
DotnetRollForward) so every template substitutes consistently --
`apply_plan.py --vars-file vars.json` handles this in one pass.

### 4. Verify

Run `dotnet build` (and `dotnet test`) from `src/` against the freshly
bootstrapped repo. A bootstrap that doesn't compile is worse than no
bootstrap -- the user will hit the failure cold, with no context for why.
Fix and re-run rather than handing over a broken skeleton.

### 5. git init (ask first)

If the user wants version control started immediately, `git init`, add the
generated `.gitignore` before the first `git add -A` (order matters -- you
do not want to stage `bin/`/`obj/` on the very first commit), then commit.

---

## Refactor an existing repo

This is bootstrap's mirror image: instead of creating from nothing, you're
diffing reality against the manifest and closing the gap -- and reality
includes things the manifest doesn't want (committed artifacts, scratch
files, casing drift) as well as things it's missing.

### 1. Scan before touching anything

```
python3 scripts/scan_repo.py <repo-root> --library-name <Name>
```

This is read-only. It returns: `missing` (manifest entries absent),
`committed_artifacts` (tracked files matching `never_commit_patterns`),
`casing_issues` (wrapper/project folders violating the casing rule), and
`uncategorized_root_files` (loose files not in the manifest -- session logs,
`*.old.md`, handoff notes). `--library-name` is optional; the script tries
to detect it from `src/*.sln` or a project folder if omitted, but confirm
the result before relying on it for substitutions later.

### 2. Triage findings into a plan -- with the user, not for them

Map each finding category to plan actions, but treat `uncategorized_root_files`
differently from the rest: **ask the user what each one is** rather than
assuming. A `PRD-Draft.md` sitting at root might be exactly where the user
wants it, or might belong in `docs/design/`, or might be safe to delete --
you can't tell from the filename alone, and silently moving or deleting
someone's working file erodes trust fast. The other three categories
(committed artifacts, casing issues, missing entries) are mechanical enough
to propose a default plan for and let the user veto specific lines.

Typical mappings:
- `committed_artifacts` → `gitignore_add` (the pattern) + `git_rm_cached`
  (the specific tracked path) -- keeps the file on disk, just stops
  tracking it.
- `casing_issues` on a project-shaped folder at root → `git_mv` into the
  right wrapper folder, preserving the project's own casing.
- `missing` entries → `create_from_template` (or a `run` step for
  SDK-generated files, same as bootstrap).

### 3. Check the working tree is clean, then execute

```
python3 scripts/apply_plan.py <repo-root> <plan.json> --vars-file vars.json --execute --require-clean
```

`--require-clean` refuses to run if `git status --porcelain` isn't empty --
mixing the refactor's changes into someone's in-progress uncommitted work
makes it much harder to review or revert. If the tree genuinely needs to
stay dirty for some reason, that's a deliberate override, not a default.

`git_mv` is used for every move specifically because it preserves file
history -- a plain `mv` (or copy+delete) makes `git log --follow` and
`git blame` useless for that file going forward.

### 4. Fix path references the moves just broke

Any `git_mv` you executed is a potential path-reference break elsewhere in
the repo (a `.csproj` `ProjectReference`, a workflow's `working-directory`,
a shell script's `cd ../..`). Build a rewrite map from your plan's `git_mv`
actions (`{old_relative_path: new_relative_path}`) and run:

```
python3 scripts/audit_paths.py <repo-root> --rewrite-map moves.json --apply
```

This resolves every path reference relative to *the file that contains it*
(not the repo root -- that distinction is the most common source of
"works on my machine" path bugs) and rewrites only the ones whose broken
target matches something you just moved. Run it once without `--apply`
first if you want to see the count before committing to the rewrite.

### 5. Verify with the real toolchain

Static path-checking catches a lot, but `dotnet build` is the actual ground
truth. Run it from `src/` after the refactor. If it fails on something the
audit didn't catch (e.g. a `.sln`/`.slnx` nested-project GUID reference -- see note
below), fix that specific file and re-run rather than trusting the static
check alone.

> `.sln`/`.slnx` files store project paths alongside GUIDs in a format that's risky
> to hand-edit. If a `git_mv` moved a project referenced in the `.sln`/`.slnx`,
> prefer `dotnet sln <path> remove <old>` + `dotnet sln <path> add <new>`
> over text-editing the `.sln`/`.slnx` directly.

---

## Add missing files only

Same as step 2–3 of Refactor, skipped straight to: run `scan_repo.py`, take
only the `missing` list, confirm with the user which optional groups they
actually want (don't assume every absent group should be filled in -- absence
might be intentional), then build `create_from_template` /
`run` actions for the confirmed gaps and execute. No moves, no `.gitignore`
surgery, no path-rewriting needed since nothing relocates.

This is also the right mode when someone says "set this repo up the same
way as my other ones" but the repo already has a reasonable `src/`/tests
structure -- don't restructure something that isn't broken just because a
file is missing.

---

## Cross-cutting rules

- **Dry-run first, always.** `apply_plan.py` defaults to dry-run; treat
  `--execute` as something the user implicitly or explicitly approved, not
  a default you reach for to save a round-trip.
- **Idempotent by default.** `create_from_template`/`create_file` actions
  skip existing files (`skip_if_exists: true` is the default) so re-running
  this skill on a repo it already touched doesn't clobber customizations.
  If the user explicitly wants a file regenerated, set
  `"skip_if_exists": false` for that one action rather than turning it off
  globally.
- **Templates are starting points, not final copy.** Things like the
  SECURITY.md contact email, CODEOWNERS team handle, and CODE_OF_CONDUCT
  enforcement contact are placeholder-real (`security@{{GitHubOrg}}.example`)
  -- point this out to the user rather than letting a fake contact address
  ship silently.
- **Optional groups stay optional.** Re-check this every time: more
  components is not automatically "more consistent." Consistency means the
  same shape *when a component is present*.
- **Never reproduce a guessed version number as fact.** SDK versions, tool
  versions -- resolve these from the actual environment (`dotnet --list-sdks`,
  `dotnet tool search`) or ask, rather than hardcoding something that was
  true at some point during this skill's authoring and may not be now.

## Scripts

| Script | Mutates repo? | Purpose |
|---|---|---|
| `scripts/scan_repo.py` | No | Inventory + diff against the manifest |
| `scripts/audit_paths.py` | Only with `--apply` | Find/fix broken relative path references |
| `scripts/apply_plan.py` | Only with `--execute` | The single execution engine for both bootstrap and refactor plans |

Run any script with `-h` for full argument details; they're also documented
in each file's module docstring.

## Templates

`assets/templates/` holds one `.tmpl` file per manifest entry that has a
`template` value. Substitution is literal `{{Key}}` replacement (see
`apply_plan.py`'s `substitute()`), so when authoring new templates avoid
introducing `{{...}}` tokens that aren't meant to be replaced. GitHub Actions
`${{ ... }}` expressions are safe as-is since they always contain spaces
inside the braces (`${{ secrets.X }}`), which never collides with the
no-space `{{Key}}` tokens this skill substitutes.
