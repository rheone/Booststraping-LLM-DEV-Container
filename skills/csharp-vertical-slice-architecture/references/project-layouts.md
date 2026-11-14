# Project layouts

Every layout below satisfies the core rule: one use case's code lives together and is named after what it
does. Pick by repo convention first. In a new project, ask the user and suggest **Layout A**.

## Detect the existing layout

Look for these markers before you choose:

| Marker in the repo | Layout in use |
| --- | --- |
| `Features/<Area>/<UseCase>/` with several files per folder | A |
| `Features/<Area>/<UseCase>.cs` holding nested `Command`/`Query`/`Handler` types | B |
| `Pages/<Area>/<UseCase>.cshtml.cs` holding nested `Query`/`Command`/`Handler` types | C |
| Separate `*.Features` or `*.Application` project referenced by a thin host project | D |
| `Modules/<Module>/` folders or `*.Modules.<Module>` projects | E |
| `Domain` project plus feature folders in an application project | F |
| `Controllers/`, `Services/`, `Repositories/` only | Layered, not sliced: see the migration workflow |

## Layout A: single project, folder per slice, file per type

The suggested starting point. Easy to scan, easy to diff, scales to large slices.

```text
src/MyApp/
├── Program.cs
├── Features/
│   ├── Orders/
│   │   ├── OrdersEndpoints.cs              # MapGroup("/orders") and MapXxx calls for this area
│   │   ├── CreateOrder/
│   │   │   ├── CreateOrderRequest.cs
│   │   │   ├── CreateOrderResponse.cs
│   │   │   ├── CreateOrderResult.cs        # union or closed hierarchy of outcomes
│   │   │   ├── CreateOrderValidator.cs
│   │   │   ├── CreateOrderHandler.cs
│   │   │   └── CreateOrderEndpoint.cs
│   │   ├── GetOrderById/
│   │   └── Shared/                         # used by 2+ Orders slices only
│   └── Billing/
├── Domain/
│   ├── Orders/Order.cs, OrderLine.cs, OrderStatus.cs
│   └── Customers/Customer.cs
└── Infrastructure/
    ├── Persistence/AppDbContext.cs, Configurations/, Migrations/
    ├── Behaviors/ or Filters/              # pipeline steps
    └── Clients/PaymentGatewayClient.cs
tests/
├── MyApp.IntegrationTests/Features/Orders/CreateOrder/CreateOrderTests.cs
├── MyApp.UnitTests/Domain/Orders/OrderTests.cs
└── MyApp.ArchitectureTests/SliceIsolationTests.cs
```

## Layout B: single project, one file per slice

Best locality: the whole use case fits in one file. Use it when slices are small. Split a slice into
Layout A's folder form when its file grows past what you can read in one screen or two.

```text
src/MyApp/Features/Orders/
├── OrdersEndpoints.cs
├── CreateOrder.cs          # static class CreateOrder { Request, Response, Result, Validator, Handler, MapEndpoint }
├── GetOrderById.cs
└── CancelOrder.cs
```

```csharp
namespace MyApp.Features.Orders;

public static class CreateOrder
{
    public sealed record Request(Guid CustomerId, IReadOnlyList<Line> Lines);
    public sealed record Line(string Sku, int Quantity);
    public sealed record Response(Guid OrderId);

    internal sealed class Handler(AppDbContext db, TimeProvider clock) { /* ... */ }

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", Endpoint).WithName(nameof(CreateOrder));

    private static async Task<Results<Created<Response>, ValidationProblem, NotFound>> Endpoint(
        Request request, Handler handler, CancellationToken ct) { /* ... */ }
}
```

## Layout C: Razor Pages as the feature tree

Bogard's reference sample (ContosoUniversity, .NET 10) uses this layout. The `Pages/` tree is the feature
tree; there is no `Features/` folder. Each page file nests `Query`, `Command`, validators, and handlers.
A page filter wraps every handler in a transaction, and handlers never call `SaveChanges`.

```text
src/ContosoUniversity/
├── Pages/Courses/Create.cshtml(.cs)
├── Pages/Courses/Edit.cshtml(.cs)
├── Pages/Instructors/CreateEdit.cshtml(.cs)    # one slice serves two verbs on purpose
├── Models/Course.cs, Student.cs                # shared entities (the domain)
├── Data/SchoolContext.cs
└── Infrastructure/DbContextTransactionPageFilter.cs, ValidatorPageFilter.cs
```

Source: <https://github.com/jbogard/ContosoUniversityDotNetCore-Pages>

## Layout D: host project plus features project

Use when you need a compile-time boundary between the host (composition root, middleware, configuration)
and the slices, or several hosts (API, worker) share the same slices.

```text
src/MyApp.Api/          # Program.cs, middleware, endpoint mapping calls, appsettings
src/MyApp.Worker/       # hosted services and consumers that call the same slices
src/MyApp.Features/     # Features/, Domain/, Infrastructure/ as in Layout A
```

If you use .NET 10+ built-in Minimal API validation (`AddValidation`), its source generator discovers
validatable types only in the assembly that calls `AddValidation`. Call it from the assembly that holds
the request types, or validation is skipped with no error and no log. See [validation.md](validation.md).

## Layout E: modular monolith with slices inside modules

Modules split by bounded context; slices split each module by use case. Modules talk through a public
contracts surface or integration events, never through each other's internals. This layout has no
Microsoft or Bogard reference sample; it is community practice built on Simon Brown's "package by
component" (<https://simonbrown.je/modular-monolith>).

```text
src/MyApp.Host/
src/Modules/Ordering/
├── MyApp.Ordering/                 # internal slices, domain, persistence for Ordering
│   ├── Features/PlaceOrder/...
│   ├── Domain/
│   └── Persistence/OrderingDbContext.cs   # one DbContext (or schema) per module
└── MyApp.Ordering.Contracts/       # public: integration events, query interfaces other modules may call
src/Modules/Billing/
├── MyApp.Billing/
└── MyApp.Billing.Contracts/
```

Rules specific to this layout:

- A module references another module's `.Contracts` project only.
- Types inside a module are `internal` by default, so the compiler enforces the boundary.
- Each module owns its tables or schema.

## Layout F: hybrid with a domain project

Use when the domain model is rich, shared by many slices, and you want the compiler to keep it free of
infrastructure. Jason Taylor's Clean Architecture template is layered on the outside with feature folders
inside its Application project (`TodoItems/Commands/CreateTodoItem/`).

```text
src/MyApp.Domain/          # entities, value objects, domain events; no package references to ASP.NET Core or the ORM
src/MyApp.Application/     # Features/<Area>/<UseCase>/ slices
src/MyApp.Infrastructure/  # DbContext / ISession, clients
src/MyApp.Web/             # host
```

This layout reintroduces project-level layering. Choose it for the compile-time domain boundary, not by
habit.

## Naming conventions

- Slice: verb + noun, as a product feature reads (`CreateOrder`, `ApproveInvoice`, `GetOrderHistory`).
  Not a noun alone (`Order`) and not a technical role (`OrderService`).
- Namespace mirrors folders: `MyApp.Features.Orders.CreateOrder`. A type's full name tells you where it
  lives.
- Types inside a slice use the slice name as prefix in Layout A (`CreateOrderRequest`) and short names
  nested in a static class in Layout B (`CreateOrder.Request`).
- Area folders are plural nouns (`Orders`, `Invoices`).
- Test files mirror the slice path: `tests/.../Features/Orders/CreateOrder/CreateOrderTests.cs`.

## Visibility

Make slice internals `internal` (handler, validator, mapping), and keep request and response records
`public` only when an external assembly needs them (a test project with `InternalsVisibleTo` does not).
`internal` plus assembly boundaries let the compiler enforce isolation in Layouts D, E, and F.
