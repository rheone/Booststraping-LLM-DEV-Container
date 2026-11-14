# Core principles

## Definition

Vertical Slice Architecture builds an application "around distinct requests, encapsulating and grouping
all concerns from front-end to back" (Bogard, 2018). You treat each request as a distinct use case and
choose the simplest implementation that use case needs. One slice can be a ten-line transaction script.
The slice beside it can call a rich domain model. Neither choice constrains the other.

The governing rule: **minimize coupling between slices, and maximize coupling inside a slice.** Couple
along the axis of change. Code that changes together for one use case lives together.

Source: <https://www.jimmybogard.com/vertical-slice-architecture/>

## What VSA drops

Layered designs impose a fixed path on every request: an endpoint must call a service, which must call a
repository. VSA removes that mandate. You keep a layer-like abstraction only when it earns its place:

- The framework requires it (an endpoint, a page model, an ORM session).
- A slice's own logic has grown complex enough that a domain model or a seam pays for itself.
- Several slices share a proven rule (see promotion below).

## Start simple, refactor on smells

Start each slice as a transaction script: the handler reads, decides, writes, and returns. Refactor
toward a domain model, a domain service, or a shared query only when a code smell appears in the slice:
a handler that grows branches for several cases, invariants duplicated across slices, or logic that
belongs to an entity. VSA depends on a team that recognizes these smells and refactors. Without that
habit, handlers grow into the god services that layering produces.

## Leading words

| Word | Meaning in this skill |
| --- | --- |
| **Slice** | One use case, end to end: request type, handler, response type, edge mapping, and its tests. Named verb + noun (`CreateOrder`, `GetOrderById`, `CancelOrder`). |
| **Area** | A folder that groups related slices (`Features/Orders/`). An area can hold a `Shared/` folder for code two or more of its slices use. |
| **Edge** | The entry point that receives a request: HTTP endpoint, page, message consumer, scheduled job, gRPC method, CLI command. The edge binds input, calls the slice, and maps the result to the transport. |
| **Handler** | The code that runs the use case. It can be a static method, a class, or a mediator handler. |
| **Domain** | Entities, value objects, invariants, domain services, and domain events shared by slices. |
| **Shared infrastructure** | Technical code with no business rules: persistence configuration, pipeline steps, filters, HTTP clients, clock, ID generation. |
| **Shared kernel** | Evans' DDD term: a small, explicitly bounded subset of the domain model that two modules or teams agree to share and change only together. Keep it small. |
| **Promotion** | Moving code out of a slice after duplication proves it is shared: to the area's `Shared/`, the domain, or shared infrastructure. |
| **Seam** | A point where you can substitute behavior without editing the slice (an interface for an external API, `TimeProvider`). Add a seam for a concrete reason: a test needs it, or a second implementation exists. |

"Shared kernel" in Evans' DDD Reference: "Designate with an explicit boundary some subset of the domain
model that the teams agree to share. Keep this kernel small."
(<https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf>)

## Duplication and promotion

Tolerate small duplication between slices. Two slices that compute the same thing today can need
different behavior tomorrow; a premature shared abstraction then grows flags for each caller. Sandi Metz
states the principle: "duplication is far cheaper than the wrong abstraction"
(<https://sandimetz.com/blog/2016/1/20/the-wrong-abstraction>).

Promote code when all of these hold:

- The logic is identical, not merely similar.
- It encodes one rule that must stay the same for every caller (a business invariant or a technical
  convention), not a coincidence.
- It has at least two real callers. A third caller is a common, informal trigger ("rule of three",
  community practice, not a sourced threshold).

Where promoted code goes is in [shared-logic.md](shared-logic.md).

Bogard's own guidance for duplication between handlers
(<https://lostechies.com/jimmybogard/2016/12/12/dealing-with-duplication-in-mediatr-handlers>):

- A common query: a method on the `DbContext` or an extension method on `IQueryable<T>` / `DbSet<T>`.
- Domain behavior: a method on the entity or a domain service.
- Duplication inside one feature folder: a base handler class for that folder is acceptable.

## CQRS falls out of slicing

Splitting by request separates commands from queries naturally: Bogard says slicing "gives me CQRS out of
the gate". VSA does not require separate read and write models, separate stores, or a mediator. See
[patterns.md](patterns.md).

## Screaming architecture

Robert C. Martin asks that the top-level structure "scream" the domain (orders, billing) rather than the
framework (controllers, models). A `Features/Orders/CreateOrder` tree does this. Martin's post also asks
that use cases stay independent of frameworks and run without a web server or database. Bogard-style
slices sit directly on ASP.NET Core and the ORM and are tested through them. VSA adopts the folder-naming
half of the idea, not the framework-independence half.
(<https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html>)

## When VSA fits

Fits well:

- Line-of-business apps and APIs with many distinct use cases, including CRUD-heavy apps. Bogard's
  reference sample (ContosoUniversity) is a CRUD app.
- Teams organized by feature. Feature folders map to ownership and reduce merge conflicts.
- Codebases where features change at different rates.
- Work done by AI agents: one folder holds the whole change, so an agent reads less context and touches
  fewer files. This is community observation, not a measured result.

Fits poorly:

- Very small apps (a handful of endpoints) where folders per use case add ceremony with no payoff.
- Libraries, SDKs, and computation engines, which have no request-shaped use cases.
- One long shared workflow where every step applies the same rules. Its steps are not independent use
  cases.

Microsoft's warning that CQRS does not suit simple CRUD applies to CQRS with separate models, not to
VSA (<https://learn.microsoft.com/azure/architecture/patterns/cqrs>).

## Trade-offs to state honestly

- Consistency across slices (every command validated the same way) needs deliberate pipeline steps or
  architecture tests. It does not come from a shared service layer.
- Small duplication is accepted by design.
- Slice size varies with the use case. A review must judge each slice, not count files.
- Microsoft's architecture guidance still calls Clean Architecture "the most appropriate way to structure
  non-trivial monolithic applications"
  (<https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures>).
  The two combine: a Clean-style domain with slice-organized application code. See
  [comparisons.md](comparisons.md).
