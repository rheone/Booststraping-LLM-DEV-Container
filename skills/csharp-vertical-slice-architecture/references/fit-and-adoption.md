# When VSA fits, when it doesn't, and how to adopt it incrementally

## Where VSA tends to shine

- **Complex, divergent business logic per use case.** When "create order" and "cancel order"
  genuinely need different validation, different side effects, and different data shapes, forcing
  them through a shared `OrderService` with a method each produces a service class that's really
  just two unrelated pieces of logic wearing one name. Slicing lets each use case's real complexity
  live in its own place, sized to what that use case actually needs.
- **Teams organized around features, not layers.** When different people/pairs own different
  product capabilities, feature folders map directly onto ownership boundaries — two people can
  work on `CreateOrder` and `CancelOrder` simultaneously with near-zero file overlap, where a
  layered structure would have both editing the same `OrderService.cs` and the same
  `OrderRepository.cs`.
- **Codebases where change frequency is uneven across features.** A feature that changes weekly
  and a feature that hasn't changed in a year don't need to share a service class just because they
  both touch orders; isolating them means the volatile one can be edited/tested/deployed without
  perturbing (or needing to fully understand) the stable one.
- **Reducing blast radius of change.** As covered in
  [philosophy-and-organization.md](philosophy-and-organization.md), a feature change touching one
  folder instead of four is the central practical payoff, and it compounds as the number of
  features grows — it's most valuable exactly where a layered codebase would otherwise be painful.

## Where VSA fits poorly

- **CRUD-heavy admin/back-office applications with little divergent logic.** When "create," "read,"
  "update," "delete" for an entity really are just create/read/update/delete — no special
  validation, no divergent side effects — a slice per CRUD operation produces four (or more)
  near-identical files that differ only in which property gets set. This is boilerplate VSA is
  usually praised for avoiding, inverted: a single generic CRUD service/controller pair, or a
  scaffolded admin framework, does the same job with far less repetition. Evaluate this
  per-entity, not application-wide — a system can reasonably mix simple CRUD entities (handled
  plainly) with a few entities whose use cases genuinely diverge (sliced).
- **Very small applications.** Below a certain feature count, the overhead of a `Features/` folder
  hierarchy with three files per slice is pure ceremony relative to a handful of straightforward
  service methods; the coupling VSA avoids never has a chance to become a real problem at that
  scale.
- **Applications with heavy, genuinely shared workflows.** If most "features" are really steps in
  one long shared workflow (e.g., a multi-stage approval pipeline where every stage must apply
  identical rules), forcing that workflow into independent slices risks the duplication described
  in [pitfalls.md](pitfalls.md) without the isolation payoff, since the steps were never
  independent in the first place.

## Evidence-based framing, not a verdict

Both directions above are informed tradeoffs, not a claim that VSA is strictly better or worse than
layered architecture. The deciding factor is almost always **how much the use cases for one entity
actually diverge from each other** — high divergence rewards slicing, low divergence rewards a
shared, generic implementation. A team that reflexively slices every CRUD entity is paying VSA's
ceremony cost without earning its isolation payoff; a team that keeps genuinely divergent use cases
crammed into one shared service class is paying layered coupling's cost without needing the
substitutability layered architecture is optimized for.

## Adopting VSA incrementally in an existing layered codebase

VSA does not require a rewrite. The pattern composes cleanly with an existing layered codebase
because "a slice" and "a service method" can coexist in the same solution while the migration
happens feature by feature:

1. **Pick one feature, not the whole application.** Choose a use case that's about to be touched
   anyway (a bug fix or a new requirement gives natural cover) rather than migrating for its own
   sake.
2. **Create its slice alongside the existing layered code**, in a `Features/<UseCase>/` folder, with
   its own request/handler/response as in [slice-anatomy.md](slice-anatomy.md). It can call into
   existing shared infrastructure (the existing `DbContext`, existing cross-cutting logging/
   validation) without waiting for those to be "VSA-ified" first — see
   [cross-cutting-concerns.md](cross-cutting-concerns.md).
3. **Redirect the entry point** (controller action, message handler) for that one use case to the
   new slice, and delete the old service method it replaces once nothing else calls it. Confirm
   nothing else genuinely does — a service method with several callers is a sign that method wasn't
   really one slice's logic and needs to be split at the callers first.
4. **Repeat per feature**, letting the `Services/`/`Repositories/` folders shrink over time as more
   of their methods are absorbed into slices, rather than attempting a big-bang cutover.
5. **Leave genuinely shared/CRUD-simple logic where it is.** Not every existing service method is a
   good migration candidate — per the fit discussion above, some existing code is already
   appropriately generic and migrating it to a slice would just be ceremony. Migrate where
   divergent use-case logic is currently forced through a shared abstraction that's straining
   under it (multiple special cases branching inside one service method is the clearest tell).

This incremental path also lets a team validate the tradeoff on real code before committing the
whole codebase to it — if the first few migrated slices don't pay off, the layered code that hasn't
been touched yet is unaffected and the migration can simply stop.
