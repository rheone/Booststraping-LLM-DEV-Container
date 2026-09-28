# Common pitfalls

## Code duplication across slices — and when it's actually fine

The most-repeated objection to VSA is that independent slices duplicate code: two slices that both
need "the customer's active orders" might each write their own version of that query. This is a
real cost, but VSA's foundational counter-argument (traced to Jimmy Bogard's original framing of
the pattern) is worth stating precisely: **duplication is cheaper than the wrong abstraction.**

The reasoning: a premature shared abstraction — a shared service method, a shared base class, a
shared repository method — extracted from two call sites that only *look* similar today locks
those two call sites together. The first time one of them needs to diverge (different filtering,
different performance characteristics, different error handling), the abstraction either grows a
parameter/flag to accommodate both cases (the start of a god method that branches on which caller
is calling it) or gets forcibly split back apart — often more expensively than if it had never been
merged, because now every other caller that accumulated in the meantime has to be re-examined.
Duplicated code, by contrast, costs something continuously (two places to update) but never costs a
large one-time untangling bill, and the two copies are free to diverge the moment they need to
without any coordination.

This does **not** mean never share code. It means shared code should be extracted **after**
duplication has proven itself real and stable, not in anticipation of it. A commonly cited
threshold: two occurrences of similar logic is a coincidence worth tolerating; a third occurrence,
still identical, is a real pattern worth naming and extracting. Extract when:

- The logic is identical (not merely similar) across the duplicates, and
- There's a specific reason to believe it will keep being identical (it encodes a genuine
  cross-cutting rule — see [cross-cutting-concerns.md](cross-cutting-concerns.md) — rather than a
  coincidence of two features currently wanting the same thing for unrelated reasons).

Don't extract when the duplication is small (a few lines) or when the two occurrences merely
resemble each other today but represent conceptually different rules that happen to compute the
same result right now.

## Inconsistent slice granularity

Because nothing forces every slice to be the same size, codebases that adopt VSA without any
shared convention can end up with wildly inconsistent granularity: some slices are a single
five-line method, others have grown to include multiple sub-steps, internal helper classes, and
several branches — effectively a mini-layered-architecture crammed into one folder. Two specific
failure directions:

- **Slices too coarse** ("fat" slices): one slice's handler accumulates logic for what are really
  several distinct use cases, often via a mode/type flag that branches the handler's behavior
  (`if (request.Mode == "Draft") { ... } else { ... }`). This is the layered "god service" problem
  recreated inside a single slice instead of across a service class. Fix: split into separate
  slices along the actual use-case boundary the flag was encoding.
- **Slices too fine**: a use case gets split across multiple "slices" that always change together
  and are never invoked independently (e.g., separate `ValidateOrderInput` and `PersistOrder`
  slices that only ever run back-to-back as one logical operation). This reintroduces the exact
  layer-hopping VSA is meant to eliminate, just with slice-shaped folders instead of layer-shaped
  ones. Fix: merge back into one slice representing the actual, independently-triggerable use case.

The practical guard against both: a slice boundary should line up with something a caller can
trigger independently and describe in one sentence ("create an order," not "validate order input,"
and not "manage orders").

## Shared-kernel creep back into a coupled layer

A "shared kernel" or `Common/`/`Shared/` folder for genuinely cross-cutting infrastructure (see
[cross-cutting-concerns.md](cross-cutting-concerns.md)) is legitimate and necessary. The pitfall is
what that folder tends to become over time without active pushback: each new slice that needs
*something* slightly reusable adds one more helper to `Shared/`, and eventually `Shared/` contains
business logic, not infrastructure — a `Shared/OrderHelpers.cs` with methods like
`CalculateDiscountedTotal` or `ValidateShippingAddress` that are really one or two slices' business
rules, now living outside any slice and implicitly depended on by several.

This is the same coupling VSA set out to eliminate, just relocated: once several slices depend on
`Shared/OrderHelpers`, changing that file again requires understanding every slice that calls it,
which is exactly the property VSA is supposed to avoid. Signs this has already happened:

- The shared folder has grown faster than the number of genuinely infrastructural concerns
  (logging, validation pipeline, persistence context) would justify.
- Functions in the shared folder take domain concepts as parameters and branch on their state
  (`if (order.Status == ...)`), rather than being generic over any request/response shape.
- Removing a "shared" helper and inlining it into its one real caller doesn't change behavior
  anywhere else — a sign it was never actually shared, just relocated out of the slice that owns
  it.

The fix mirrors the boundary test in
[cross-cutting-concerns.md](cross-cutting-concerns.md#a-practical-boundary-test): periodically
audit the shared folder against that test and move anything that fails it back into the slice(s)
that actually use it, even if that means accepting the duplication described above.
