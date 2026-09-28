# Philosophy and organization

## The core idea: slice by feature, not by layer

Traditional N-tier/onion/"clean" architecture organizes source code by **technical role**:
everything that talks HTTP goes in `Controllers/`, everything that holds business rules goes in
`Services/`, everything that touches the database goes in `Repositories/`. A single user-facing
capability — "create an order" — is implemented as one class in each of those folders, wired
together through interfaces.

Vertical Slice Architecture organizes source code by **feature/use-case** instead. All the code
needed to fulfill one request — its input shape, its logic, its output shape — lives together,
usually in one file or one folder, regardless of which "layer" that code would technically belong
to in an N-tier diagram. The organizing question changes from "what kind of thing is this file?"
to "what capability does this file belong to?"

This is often summarized (originating with Jimmy Bogard's formulation of the pattern) as:
**minimize coupling between slices, and maximize coupling within a slice.** Two different features
can look completely different internally — one might be a single method, another might have its
own multi-step pipeline — without either affecting the other, because nothing is shared by
default.

## "Screaming architecture" — what the folder tree tells you

Robert C. Martin coined the phrase "screaming architecture" to describe a codebase whose top-level
structure announces *what the system does* rather than *which framework or pattern it uses*.
Opening a layered codebase's folder tree tells you it uses an MVC-shaped framework — `Controllers/`,
`Models/`, `Views/` — but tells you nothing about whether it's a billing system or a shipping
system. Opening a sliced codebase's `Features/` folder tells you exactly what the application does:
`CreateOrder/`, `CancelOrder/`, `GetOrderById/`, `ApplyDiscountCode/`. VSA is one concrete way to
get a codebase that "screams" its purpose — the top-level structure mirrors the product's
capabilities, not its plumbing.

This matters most at the point where a new contributor (human or agent) opens the repository for
the first time: a feature-organized tree answers "what does this do" before a single line of code
is read; a layer-organized tree only answers "how is this built."

## What a slice typically contains

A slice is the smallest unit that fulfills one request end to end. At minimum it typically bundles:

- An **input shape** — the data the caller supplies (route/query/body parameters shaped into one
  type).
- The **logic** that turns that input into an effect and/or a result — validation, business rules,
  persistence calls, whatever the use case actually requires.
- An **output shape** — the data returned to the caller.

These three pieces are colocated — often as three small types in one file, or three files in one
feature folder — rather than the input type living in an API project, the logic living in a
service class in a business-logic project, and the output type living in a DTO project. Colocation
is the mechanism; the payoff is that changing one feature's behavior means editing one place
instead of coordinating edits across several folders (and, in a larger codebase, several
projects).

A slice is deliberately **shallow but complete**: it does not need its own controller, its own
service interface, and its own repository interface stacked on top of each other the way a layered
feature would. One handler method is often the whole implementation. See
[slice-anatomy.md](slice-anatomy.md) for the concrete request/handler/response shape.

## Folder and namespace conventions

The most common top-level convention is a `Features/` (or `UseCases/`) folder, with one
subfolder per slice named after the use case in verb-noun or noun-verb form:

```text
Features/
  Orders/
    CreateOrder/
      CreateOrderRequest.cs
      CreateOrderHandler.cs
      CreateOrderResponse.cs
    GetOrderById/
      GetOrderByIdRequest.cs
      GetOrderByIdHandler.cs
      GetOrderByIdResponse.cs
    CancelOrder/
      ...
```

Variations seen across mature codebases, all satisfying the same underlying rule (a feature's code
lives together and is named after what it does):

- **Flat feature folders** — `Features/CreateOrder/`, `Features/GetOrderById/` directly under
  `Features/`, with no entity-level grouping. Simpler for small-to-medium applications; the
  entity name (`Order`) becomes implicit in the slice name instead of a folder level.
- **Entity-grouped feature folders** — `Features/Orders/CreateOrder/`, as above. Scales better once
  an application has many entities, since related slices for the same entity sort together.
- **Single-file-per-slice** — the request, handler, and response for one slice are all nested
  types (or top-level types) in a single `CreateOrder.cs` file rather than a folder. Favored when
  a slice is small; reduces file-navigation overhead at the cost of longer individual files.

Naming convention: the slice folder/file name is the use case, phrased as a verb-noun pair
(`CreateOrder`, `CancelOrder`, `GetOrderById`) rather than a noun alone (`Order`) or a technical
role (`OrderService`). The name should read like an entry in a product's feature list, which is the
same instinct behind "screaming architecture" above — a reader scanning slice names should be able
to reconstruct the application's capabilities without opening any files.

Namespaces typically mirror the folder structure 1:1 (`MyApp.Features.Orders.CreateOrder`), which
keeps "where is this type" answerable from the fully qualified type name alone, without needing to
search the solution.

## Contrast with N-tier / onion / clean architecture

None of these are wrong so much as answering a different question. Layered architectures optimize
for **substitutability along an axis** — swap the data-access layer, swap the UI layer — by
enforcing that dependencies point inward/downward through well-defined layer interfaces. That
payoff is real when a project genuinely expects to swap a layer (e.g., support multiple UI
front-ends over one shared business/data core).

VSA optimizes for a different property: **the blast radius of a single feature change**. In a
layered codebase, adding a field to "create order" touches the controller, the service, the
repository, and often several DTOs/mappers, all in different folders/projects. In a sliced
codebase, the same change touches one slice. The tradeoff is that cross-cutting consistency (e.g.,
"every write path validates the same way") has to be achieved deliberately — see
[cross-cutting-concerns.md](cross-cutting-concerns.md) — rather than falling out for free from a
shared service layer that every feature is forced to go through.

VSA and layered architecture are not mutually exclusive at every scale: a single slice's *internal*
code can still separate "handler logic" from "persistence" if that slice is complex enough to
benefit from it (see [data-access-patterns.md](data-access-patterns.md)). What VSA rejects is
using layer as the top-level, solution-wide organizing axis for every feature regardless of size or
complexity.
