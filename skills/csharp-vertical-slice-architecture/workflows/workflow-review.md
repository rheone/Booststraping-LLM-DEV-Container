# Workflow: review a VSA codebase

Audit a project (or a set of slices) against the dependency rules and code smells, and report findings
ranked by severity. Change nothing unless the user asks. Every step ends on a completion criterion.

## 1. Establish the conventions under review

Read `ARCHITECTURE.md` / `AGENTS.md` / `CLAUDE.md`, the project files (target framework, language version,
packages), the host's `Program.cs`, and existing architecture tests or analyzer config. From these and
two or three slices, write down the repo's layout, isolation level, dispatch, data access, outcome style,
validation, and test style.

**Done when** each convention is written down with the file that shows it. Where the repo has no stated
convention, review against the suggested defaults in the reference files and say so in the report.

## 2. Map the slices

List every area and slice with its edge type and route or trigger. For a large codebase, the user may name
the areas to review; otherwise review all of them.

**Done when** every folder under `Features/` (or the layout's equivalent) appears in the list, and every
edge in the app maps to a slice or is listed as "outside slices".

## 3. Check dependencies

For each slice, list its `using` directives and type references to other slices, other areas, area
`Shared/`, domain, infrastructure, and the host. Check the domain and infrastructure for references to
Features. If architecture tests exist, run them; if they do not, a scratch file-based app with an
architecture-test library can compute the dependency graph (standing rule 5), or use a text search for
`using <Root>.Features.` across the tree.

**Done when** every slice has its outgoing references classified as allowed or a violation under the
repo's isolation level ([dependency-rules.md](../references/dependency-rules.md)).

## 4. Walk the smell catalog

Apply every row of [code-smells.md](../references/code-smells.md) to every slice in scope, in this order:
coupling, layering, slice shape, errors and validation, async, testing. Check the shared folders (area `Shared/`,
`Common/`, `Infrastructure/`) with the boundary test in
[shared-logic.md](../references/shared-logic.md#boundary-test-for-shared-code).

**Done when** every smell row has been checked against every slice in scope, and each hit is recorded
with file, line, and smell name.

## 5. Check the platform items

- Language version vs. use of `union` / `closed` (standing rule 3).
- `AddValidation` called from the assembly that holds the request types.
- Exception handler and ProblemDetails wired.
- Handlers and pipeline steps registered with lifetimes that match their dependencies (no scoped context
  in a singleton).
- AOT/trim settings vs. reflection-based dispatch or scanning ([platform-and-build.md](../references/platform-and-build.md)).
- Every slice has tests for each outcome ([testing.md](../references/testing.md)).

**Done when** each item is marked pass, fail (with location), or not applicable.

## 6. Report

Rank findings by the severity table in [code-smells.md](../references/code-smells.md). For each finding:
smell name, location, why it matters in this repo, and the specific refactor. Group by area. End with:
conventions found (step 1), what is healthy, and the three changes with the most value.

Use the repo's review format if it has one; otherwise:

```markdown
## VSA review: <scope>

Conventions: <layout, isolation, dispatch, data access, outcomes, validation, tests>

### High
- **<Smell>**: `<path>:<line>`: <why it matters here>. Refactor: <specific change>.

### Medium
...

### Low
...

### Healthy
- <what follows the rules well>

### Top three changes
1. ...
```

**Done when** every recorded hit from steps 3–5 appears in the report exactly once, with a refactor, and
nothing in the report lacks a location.
