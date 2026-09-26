# Common Pitfalls, and the "Should You Use AutoMapper At All" Debate

## Common pitfalls

- **Unmapped members discovered in production, not CI.** The single most common failure: a
  `CreateMap` with no `AssertConfigurationIsValid()` running anywhere, so a renamed or newly-added
  destination member silently fails (or throws) only when a real request first hits that code
  path. See [configuration-validation.md](configuration-validation.md) and
  [testing.md](testing.md).
- **Convention-based mapping hiding a wrong-but-plausible match.** Two same-named properties that
  mean different things (e.g. `Total` meaning subtotal on one type and grand total on another) map
  "successfully" by convention with no warning, because structurally nothing is wrong — only a
  behavioral test catches it.
- **Debugging a mapping failure means stepping into generated/compiled expression trees**, not
  ordinary C# call stacks. AutoMapper compiles mapping plans into expression trees for
  performance; when something goes wrong inside a complex map (deep nesting, several resolvers
  chained together), the exception and stack trace point into AutoMapper's internals rather than
  directly at "line 42 of your profile." This is the most frequently cited developer-experience
  complaint about AutoMapper at scale: a plain hand-written `dest.Name = src.Name` is trivially
  debuggable; a `CreateMap` chain producing the wrong value for a deeply nested member often isn't,
  without deliberately isolating the failing map in a small reproduction.
- **Logic creeping into profiles.** `Condition`, `PreCondition`, `BeforeMap`/`AfterMap`, and custom
  resolvers make it easy to smuggle real business logic into a mapping profile instead of the
  domain/service layer, because "it's just configuring how these two types relate" is an easy
  justification for adding "just one more" branch. Once a profile has several chained conditions
  and a resolver with injected services, it has stopped being a mapping and become an
  undocumented, hard-to-test transformation layer.
- **`Map(source, destination)` update-in-place semantics surprising people**, especially around
  collections (see [collections-and-nested-objects.md](collections-and-nested-objects.md)) —
  teams assume a merge where AutoMapper does a replace, or vice versa, and only notice when data
  is unexpectedly overwritten or duplicated.
- **`ProjectTo` silently falling back to client-side evaluation**, or throwing a translation
  exception, when a map configured for `ProjectTo` uses a resolver/expression the query provider
  can't translate — especially easy to introduce accidentally when a profile is shared between
  `Map` and `ProjectTo` use sites and someone adds a resolver only tested against the `Map` path.
- **Assuming licensing hasn't changed.** Upgrading an existing dependency (or copy-pasting setup
  code from an older tutorial) without checking the current license terms independently.

## The community debate: should you use AutoMapper at all?

This is a genuine, longstanding, still-active debate in the .NET community, not a settled
question — presented here neutrally, because the "right" answer depends on the team and codebase,
not on AutoMapper's technical merits in isolation.

**The case for AutoMapper (or a mapping library generally):**

- Eliminates large amounts of repetitive, low-value hand-written mapping code, especially for
  wide DTOs with dozens of properties.
- Centralizes mapping rules in one place (profiles) rather than scattered ad hoc across
  controllers/services.
- `ProjectTo` specifically has a real, hard-to-replicate benefit: generating provider-translated
  projections from declarative configuration is genuinely more convenient than hand-writing a
  `Select` expression for every query shape.

**The case against (or for hand-written mapping / a source generator instead):**

- Hand-written mapping code (`dest.Name = src.Name;` ...) is boring but trivially readable,
  debuggable with an ordinary breakpoint, and refactor-safe (the compiler flags a renamed
  property immediately, rather than only at `AssertConfigurationIsValid()` time or, worse, at
  runtime).
- Convention-based (reflection/expression-tree-based) mapping trades compile-time safety for
  runtime "magic" — the wrong-but-plausible-match failure mode above is a direct consequence, and
  is specific to convention-based mapping rather than something a hand-written mapper or a
  compile-time source generator (e.g. Mapperly) is prone to in the same way.
- At scale, in a large codebase with hundreds of profiles, AutoMapper mapping configuration itself
  becomes a maintenance surface that needs its own tests, its own conventions, and its own
  onboarding — some teams conclude the debugging/maintenance overhead outweighs the boilerplate it
  removes, especially once licensing is also a consideration rather than a settled non-issue.
- Source-generator-based alternatives (compile-time code generation rather than runtime
  reflection/expression trees) have gained traction specifically as a response to both concerns:
  they produce ordinary, debuggable generated C# and don't carry a runtime licensing model in the
  same way, at the cost of some of AutoMapper's more dynamic convention/resolver flexibility.

**Practical takeaway:** AutoMapper is a reasonable default for DTO-heavy CRUD-shaped mapping in a
codebase already using it, or for the specific `ProjectTo`-into-a-database-query use case. It is
worth reconsidering (either hand-written mapping, or a compile-time alternative) when: mapping
logic is trending toward real business logic (see the "logic creeping into profiles" pitfall
above), licensing terms don't fit the project, or debugging mapping failures has become a
recurring time sink. This is a judgment call to surface explicitly
to whoever owns the decision, not a call this skill makes on a team's behalf.
