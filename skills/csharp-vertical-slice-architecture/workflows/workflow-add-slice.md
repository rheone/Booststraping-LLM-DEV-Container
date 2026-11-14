# Workflow: add a slice

Add one use case to a project that is already organized by slices (or partly migrated). Every step ends on
a completion criterion.

## 1. Learn the repo's conventions

Read `ARCHITECTURE.md` / `AGENTS.md` / `CLAUDE.md` if present. Then open two existing slices in the same
area (or the nearest area) and note:

- Layout (folder per slice or file per slice), namespaces, and naming (`CreateOrder` vs.
  `CreateOrderCommand`).
- Edge type and registration (`MapGroup` per area, scanning, controller per slice).
- Dispatch (direct injection, mediator, library).
- Data access (context, session, unit of work, repository) and transaction boundary.
- Outcome style (union, closed hierarchy, abstract record, `Result<T>`, exceptions) and language version.
- Validation mechanism.
- Test style, test project, and fixture.

**Done when** you can name the convention for every bullet from code you read. Where the repo has no
example (for example the first consumer slice), ask the user, offering the options from the matching
reference file.

## 2. Name the slice and place it

Name it verb + noun for what the caller achieves (`CancelOrder`). Choose the area. If the use case already
exists under another name, or one handler would serve it through a flag, stop and tell the user.

**Done when** the slice name, area, and folder path are fixed, and no existing slice covers the same use
case.

## 3. Write the request and outcome types

Request record with input only, and only the input the requirement names (standing rule 7): no extra
required fields because the use case "should" have them. Outcome cases named after the domain. A `union` or `closed` hierarchy only
when the project is on C# 15+; otherwise the repo's style or the abstract-record fallback
([results-and-errors.md](../references/results-and-errors.md)). Response records built for this slice only.

**Done when** the types compile, and every outcome the use case can produce has a case.

## 4. Write the validation rules

Shape rules on the request (attributes or the repo's validator type). Business rules that need data go to
the handler as outcomes.

**Done when** every input constraint from the requirement has a rule, and no business rule is expressed
as a shape rule.

## 5. Write the handler

Load exactly what the use case needs; change state through domain methods; save once at the repo's
transaction boundary; return an outcome. Queries project straight into the response with no tracking.
Promote nothing yet; duplicate small pieces from other slices if needed. `HandleAsync` takes
`CancellationToken ct = default` last, passes `ct` to every async call, and uses `.ConfigureAwait(false)`
on every await ([slice-anatomy.md](../references/slice-anatomy.md#async-and-cancellation)).

**Done when** the handler compiles, references no other slice, returns no transport type, throws only
for exceptional conditions, and has no `await` without `ConfigureAwait(false)` and no async call without
the token.

## 6. Write the edge and register it

Map the edge per the repo's convention, with an exhaustive switch from outcome to transport result (a
`null` arm for unions). Register the handler and map the endpoint in the area's registration files.
Apply area or endpoint metadata: name, summary, tags, authorization policy, filters.

**Done when** the app starts and the route (or consumer, job, command) is reachable.

## 7. Write the tests

In the mirrored test folder: one success test, one test per failure outcome, one invalid-input test if the
slice has shape rules, one authorization test if it has a policy. Seed data through the context or
session, never through another slice. Pass the test framework's token to every async call, with no
`ConfigureAwait`. See [testing.md](../references/testing.md).

**Done when** every outcome case has a test, and all of them pass.

## 8. Check boundaries and contracts

Run the full test suite, including architecture tests. Check the code-smell list in
[code-smells.md](../references/code-smells.md) against the new slice (coupling, slice shape, error and
validation sections).

**Done when** `dotnet build` has no new warnings, `dotnet test` passes, no smell from those sections
applies to the new slice, and no existing test changed its expectations.

## 9. Report

Tell the user: the files added, the registration lines changed, the outcomes and their status codes, any
code you duplicated rather than promoted (with the reason), and any input or behavior you think is missing
but did not add (standing rule 7).

**Done when** the report lists every file you added or changed.
