# Workflow: migrate a layered project to slices

Move use cases out of `Controllers/` → `Services/` → `Repositories/` into slices, one at a time, without
changing what the app does from the outside. Every step ends on a completion criterion.

**Invariant for every step:** externally observable behavior stays the same: routes, HTTP methods,
request and response JSON, status codes, headers, side effects (rows written, messages sent, emails),
and authorization. If a step would change any of these, stop and ask the user.

## 1. Inventory

List every edge (controller action, page handler, consumer, job) with its route or trigger, the service
methods it calls, the repository methods those call, and the DTOs it returns. Note shared services with
many callers, existing tests, and the test framework. Note the libraries in use (standing rule 2: keep
them).

**Done when** every edge appears in the inventory with its full call chain, and you can name each service
method's callers.

## 2. Agree the migration plan with the user

Ask, one question at a time:

1. **Delivery**: one use case per change, one area per change, or staged PRs with a target per stage.
2. **Legacy coupling**: may migrated slices call legacy services and repositories temporarily (tracked as
   debt in a migration log and an architecture-test allow-list), or must each move inline the logic it
   needs?
3. **Target conventions**: layout, dispatch, data access, outcome style, validation, and isolation level,
   each following the current code where it already has a convention. Present the options from the
   reference files where it does not.
4. **Architecture tests**: offer them, in "new code only" mode while legacy code exists.
5. **Order**: suggest starting with a low-risk read slice in an area under active change.

**Done when** every question has an answer, recorded in a migration log file (`docs/vsa-migration.md` or
the user's choice) with the inventory from step 1.

## 3. Pin the behavior

Before moving anything, make sure tests cover the externally observable behavior of the edges in this
round: existing tests if they assert status codes and bodies; otherwise add HTTP-level characterization
tests (or response snapshots) for each edge, including its failure paths.

Record responses against the running original code; do not write expected values from reading the code.
Details that leak into responses and are easy to break:

- MVC action parameter names appear in model-state error keys (`"errors": { "dto": [...] }`). Renaming
  `dto` to `request` changes the 400 body.
- `decimal` scale: a value echoed from the request (`12.50`) can differ from the same value read back from
  the database (`12.5`).
- `DateTime` kind: a value set in memory as UTC serializes with `Z`; the same value read back from some
  providers has `Kind = Unspecified` and no `Z`.
- Framework-generated bodies for 400, 405 (with the `Allow` header), and 415 differ between MVC and
  Minimal APIs. Keep the edge technology unless the user accepts the change.
- Whether a write completes when the client disconnects. The slices pass the request's
  `CancellationToken` to every async call (standing rule 8), so a write the old code finished regardless
  can now roll back. Keep the token, and list the change in the migration report.

Keep the masking of volatile values (trace IDs, timestamps) as narrow as possible.

**Done when** every edge in this round has passing tests that would fail if its route, status code, or
response body changed, and you have watched at least one of them fail on a deliberate change.

## 4. Create the skeleton (first round only)

Add `Features/` beside the existing folders, plus the area folders for this round. Add the area
registration methods and call them from the host. Add architecture tests if agreed, with rule 6 (legacy
does not depend on Features) and, if legacy coupling is allowed, rule 13 (allow-list).

**Done when** the app builds and all existing tests pass with the empty skeleton in place.

## 5. Move one use case

For one edge from the inventory:

1. Create the slice folder and its request and response types. Keep the JSON contract identical: same
   property names, casing, nullability, and enum serialization as the old DTO.
2. Write the handler from the service method's logic. Inline the repository calls as direct data access,
   or call the legacy service if the plan allows it (and add the call to the allow-list and the log).
3. Point the edge at the handler: move the action into the slice as an endpoint with the same route, or
   keep the controller action and make it call the handler. Do not run old and new edges for the same
   route at once.
4. Map outcomes so status codes match the old behavior exactly. If the old code threw for expected
   outcomes and a global filter mapped them, return outcomes from the handler and map them to the same
   status codes and problem bodies.

**Done when** the pinned tests for this edge pass unchanged, the new slice references no other slice, and
legacy code does not reference it.

## 6. Delete what died

Remove the service and repository methods (and DTOs, mapper profiles, interfaces) that no caller uses any
more. Search the solution, including tests and reflection-based registration, before deleting. Leave a
method that still has other callers.

**Done when** the build succeeds, every pinned test passes, and the deleted members have no remaining
references.

## 7. Record and repeat

Update the migration log: edge moved, legacy calls still allowed (with an owner), members deleted. Repeat
steps 5–7 for the next edge in the round. At the end of a round, run [workflow-review.md](workflow-review.md)
on the migrated areas.

**Done when** every edge in the round is moved, the allow-list did not grow beyond what the log records,
and the review found no high-severity smell in the migrated areas.

## 8. Finish

When an old folder (`Services/`, `Repositories/`, `Controllers/`) is empty, delete it. Switch architecture
tests from "new code only" to the full rule set. Write or update `ARCHITECTURE.md` from
[ARCHITECTURE.md.tmpl](../assets/templates/ARCHITECTURE.md.tmpl).

**Done when** no legacy layer folder holds use-case logic, the allow-list is empty or each remaining entry
has an owner and a reason in the log, `dotnet test` passes, and `ARCHITECTURE.md` matches the code.

## Notes

- Shared services with many callers (pricing, permissions) usually hold real domain rules. Move them into
  the domain (entity methods or a domain service), not into one slice.
- Generic repositories die naturally as their last caller moves. Leave them until then.
- If the old code exposes EF entities in responses, the new slice still returns the same JSON. Introduce a
  response record whose serialized shape matches, and verify with the pinned test.
- Migrations that also upgrade .NET or swap libraries are separate projects. Do one at a time.
