# Code smells

Each smell has a symptom you can detect mechanically, the reason it hurts, and the refactor. The review
workflow walks this list in order.

## Coupling smells

| Smell | Symptom | Refactor |
| --- | --- | --- |
| **Slice references slice** | A type in `Features.Orders.CreateOrder` uses a type from `Features.Orders.GetOrderById` or `Features.Billing.*` | Duplicate the small piece, promote it (see [shared-logic.md](shared-logic.md)), or use an event |
| **Handler calls handler** | A handler injects another handler, or calls `Send` / `SendAsync` on a mediator | Extract the shared rule to the domain or a shared query; if both edges run the same use case, call one handler from both edges |
| **Shared folder as hidden layer** | `Common/`, `Shared/`, or `Helpers/` holds methods that take domain types and branch on their state; it grows faster than the slice count | Move each method back into the slices that call it; keep only generic infrastructure |
| **Shared response DTO** | One `OrderDto` returned by several slices | Give each slice its own response record; let them diverge |
| **Shared code references a slice** | Infrastructure, domain, or area `Shared/` imports a slice namespace | Invert: the slice depends on shared code, never the reverse |
| **Domain depends outward** | Entities reference `DbContext`, `IResult`, `HttpContext`, request records, or ORM attributes beyond what the repo accepts | Move configuration to infrastructure; pass values in |
| **Legacy calls Features** (during migration) | Old services or controllers reference slice types | Route the legacy caller through the slice's edge, or move the caller into a slice |

## Layering smells

| Smell | Symptom | Refactor |
| --- | --- | --- |
| **Sliced folders, layered code** | `Features/Orders/CreateOrder/` exists, but its handler calls `OrderService.Create`, which calls `OrderRepository` | Inline the service method into the handler; delete the service when its last caller moves |
| **Service per entity** | `OrderService` with `Create`, `Cancel`, `Get`, `List` | Split each method into its slice |
| **Generic repository over the ORM** | `IRepository<T>` with `GetAll`, `Find(predicate)`, or `IQueryable<T>` | Use the session directly, or a repository per aggregate with named methods (see [data-access.md](data-access.md)) |
| **Interface with one implementation, never faked** | `ICreateOrderHandler` / `CreateOrderHandler` with no test double | Remove the interface; inject the class |
| **Mapping layer for identical shapes** | Mapper profiles between records with the same members | Project directly in the query, or construct the response by hand |

## Slice-shape smells

| Smell | Symptom | Refactor |
| --- | --- | --- |
| **Fat slice** | A handler branches on a mode or type flag; several distinct outcomes for distinct callers | Split along the flag |
| **Anemic split** | Two slices always run together and never alone | Merge them |
| **Logic in the edge** | The endpoint, controller, or consumer queries data or applies business rules | Move the logic into the handler; the edge only binds and maps |
| **Transport in the handler** | The handler returns `IResult`, reads `HttpContext`, or sets status codes | Return an outcome; map at the edge |
| **ORM entity in a contract** | A request or response exposes an entity, `IQueryable<T>`, or a navigation property | Introduce a slice-specific record; project into it |
| **Tracked read** | A query slice loads tracked entities then maps them | `AsNoTracking()` + `Select` into the response |
| **Inconsistent naming** | Slices named `OrderCreate`, `CreateOrder`, `NewOrderHandler` in one area | Rename to the repo's verb-noun convention |

## Error and validation smells

| Smell | Symptom | Refactor |
| --- | --- | --- |
| **Exceptions as control flow** | A handler throws `NotFoundException` / `ValidationException` for an expected outcome; a global handler maps it | In new code, return outcomes; in a repo that standardizes on exceptions, follow it and note the cost |
| **Non-exhaustive outcome switch** | A discard `_` arm that returns a success or a generic 500 on a union or closed hierarchy | Remove the discard; add the missing arms; add a `null` arm for unions |
| **Validation silently skipped** | Requests in an assembly other than the one calling `AddValidation`; no invalid-input test | Call `AddValidation` from the right assembly; add a 400 test per slice |
| **Shape rules in the handler** | The handler checks required fields or ranges | Move them to attributes or the slice's validator |

## Async smells

| Smell | Symptom | Refactor |
| --- | --- | --- |
| **Sync over async** | `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, or a sync ORM call (`SaveChanges`, `ToList`) in a slice | Make the call chain async up to the edge |
| **Dropped token** | An async method has no `CancellationToken`, or a caller with a token in scope calls without it (CA2016) | Add `CancellationToken ct = default` last; pass the caller's token |
| **Missing `ConfigureAwait(false)`** | An `await` in `src/` outside UI code has no `ConfigureAwait(false)` (CA2007) | Add it; enable CA2007 for `src/` |
| **Nullable token** | A `CancellationToken?` parameter | Use `CancellationToken ct = default` |

## Testing smells

| Smell | Symptom | Refactor |
| --- | --- | --- |
| **Mocked ORM** | Tests mock `DbSet<T>` or use the EF Core in-memory provider | Test against the real database engine in a container (see [testing.md](testing.md)) |
| **Test tree does not mirror features** | Tests grouped by technical type (`HandlerTests/`, `ValidatorTests/`) | Mirror the slice path |
| **No architecture tests, or ignored ones** | Isolation rules exist only in docs; rules are skipped | Offer architecture tests (see [architecture-tests.md](architecture-tests.md)) |

## Severity for reviews

| Severity | Smells |
| --- | --- |
| High | Slice references slice, handler calls handler, shared code references a slice, domain depends outward, legacy calls Features, validation silently skipped, ORM entity in a contract, sync over async |
| Medium | Sliced folders with layered code, shared folder as hidden layer, shared response DTO, fat slice, logic in the edge, transport in the handler, exceptions as control flow (new code), non-exhaustive switch, mocked ORM, dropped token |
| Low | Generic repository, single-implementation interface, mapping layer, anemic split, tracked read, inconsistent naming, test tree layout, missing `ConfigureAwait(false)`, nullable token |
