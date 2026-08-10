# Shared Components

A shared component is any class, method, stored procedure, view, function, or
trigger used by more than one feature - or clearly generic/reusable even if
only one caller has been seen so far (a `DataAccessHelper` base class, say).
Document it once; link to it from every feature that depends on it.

## When to create one

- While tracing in Phase 2, you hit code or SQL that is clearly not
  feature-specific (`Utils`, `Helpers`, `Base*`, or a proc called from several
  distinct call sites)
- A stored procedure carrying significant business logic, even if currently
  called from one place - better documented once at the right rigor than
  inlined and lost
- The user says something is shared

Do not preemptively document everything in a `Common`/`Shared` folder just
because it exists. Create a doc when a feature you are actively documenting
actually depends on it, so effort stays proportional to what is in use.

## The template

Copy `templates/shared-component-doc.md` verbatim to
`<output-root>/shared-components/<component-slug>.md`, then fill it. The
enforced contract is in `doc-manifest.md`; it is validated in Phase 3 exactly
like a feature doc.

Required sections: `What it does`, `Inputs / Outputs`,
`Business rules embedded here`, `Side effects`, `Confidence / Open Questions`.

`Business rules embedded here` gets the same rigor as a feature doc - citation
plus confidence tag on every bullet. For procs and triggers this is usually
where the real logic lives, so it is not a footnote.

`Side effects` matters most for triggers and procs: what does this touch that
its name does not suggest? A trigger on `Orders` that also writes an audit row
to `OrderHistory` and updates `Customers.LastOrderDate` is exactly the kind of
behavior a rewrite silently loses.

## IDs

`comp-XXXX`, same rules as `feat-XXXX`: assigned once on first documentation,
sequential from the highest existing, never reused or renumbered even if the
component is renamed or moved. If a doc already exists, reuse its id - never
mint a second one for the same component.

## Maintaining `used_by`

Every time a feature links to a shared component, add that feature's id to the
component's `used_by` frontmatter list and its link list, if not already there.

This is what makes `shared-components/` doubly useful: it becomes a blast-radius
map for the rewrite team - change this proc, and here is every feature that
feels it.
