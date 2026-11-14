# Vertical Slice Architecture in .NET — research notes

Compiled 2026-11-13 to inform the rewrite of the local skill `skills/csharp-vertical-slice-architecture`
(v1.0.0, tool-agnostic, 8 reference files). Each claim cites its owner: Jimmy Bogard's own posts and
sample repo, Microsoft Learn, library repos and docs, and Evans / Martin / Brown for the vocabulary.
Community writers (Milan Jovanović, Derek Comartin) appear only as labeled **community practice**.
Anything not traced to a primary source is marked **unverified** or **community opinion**.

Repos read locally (shallow clones, 2026-11-13):

- `jbogard/ContosoUniversityDotNetCore-Pages`, HEAD `abb4c08` (2025-12-03), net10.0.
- `ardalis/CleanArchitecture`, HEAD 2026-11-13 (includes `MinimalClean/`).
- `jasontaylordev/CleanArchitecture`, HEAD 2026-11-09.
- `TNG/ArchUnitNET`, HEAD 2026-10-24; `BenMorris/NetArchTest`; `NeVeSpl/NetArchTest.eNhancedEdition`.
- `LuckyPennySoftware/MediatR` (the old `jbogard/MediatR` URL redirects there).

Short labels used below:

- **VSA18** = <https://www.jimmybogard.com/vertical-slice-architecture/> (Bogard, 2018-04-19)
- **CONTOSO** = <https://github.com/jbogard/ContosoUniversityDotNetCore-Pages>
- **WEBARCH** = <https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures>
- **MSAPP** = <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api>
- **MSPERSIST** = <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design>
- **EFTEST** = <https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy>
- **VALID** = <https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-10.0>
- **DDDREF** = Evans, *DDD Reference* (2015), <https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf>

Version facts as of this date: .NET 11.0.0 and .NET 10.0.15 both shipped 2026-11-10. .NET 11 is STS and
.NET 10 is LTS. .NET 9 is EOL
(<https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json>).

---

## 1. Definition, origin, Bogard's core claims

| Claim | Source / quote |
| --- | --- |
| VSA is built "around distinct requests, encapsulating and grouping all concerns from front-end to back." | VSA18 |
| Coupling rule: "Minimize coupling between slices, and maximize coupling in a slice." Couple "along the axis of change." | VSA18 |
| Each request is its own use case: "I treat each request as a distinct use case in how to approach its code." | VSA18 |
| Start simple, then refactor: teams can "start simple (Transaction Script)" and "simply refactor to the patterns that emerges from code smells we see in the business logic." | VSA18 |
| Layers are dropped as a mandate: "we don't need any kind of 'shared' layer abstractions like repositories, services, controllers." Some remain because "they are still required by our tools (like controllers or ORM units-of-work)", but "we keep our cross-slice logic sharing to a minimum." | VSA18 |
| The rejected default: "Controller MUST talk to a Service that MUST use a Repository." Layered designs are called "mock-heavy" with "rigid rules around dependency management." | VSA18 |
| CQRS comes along: splitting by request "gives me CQRS out of the gate." | VSA18 |
| Stated downside: it works only if "your team understands code smells and refactoring." It fails "If your team does not understand when a 'service' is doing too much." | VSA18 |
| Older ideas behind it: Microsoft's MVC e-book calls the idea "feature folders or feature slices." It cites a 2016 MSDN article (Steve Smith) and DevIQ "Vertical Slices." | <https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps#structuring-the-application> |
| Razor Pages fit: "Those building with vertical slice architectures will find a very natural fit with Razor Pages." Bogard's migration was "basically renaming the `Features` folder to `Pages`." | <https://www.jimmybogard.com/migrating-contoso-university-example-to-razor-pages/> (2018-02-21) |
| Mediator only at the edges: "I only use MediatR on the edges of my systems (with Vertical Slice Architecture)" and "I don't ever see `Send` outside of a controller/page etc." | <https://jimmybogard.com/you-probably-dont-need-to-worry-about-mediatr/> (2022-05-05) |
| 2026 framing for AI-assisted work: AI tools "don't necessarily create malleable or maintainable code", and VSA can shorten review cycles "by reducing side-effects and coupling in your systems." This is a webinar announcement, not an argument. | <https://www.jimmybogard.com/vertical-slice-architecture-webinar/> (2026-07-23) |

Not in VSA18, though often attributed to it: the words "duplication", "wrong abstraction", "DRY", and any
testing guidance. The line "duplication is far cheaper than the wrong abstraction" comes from Sandi Metz
(<https://sandimetz.com/blog/2016/1/20/the-wrong-abstraction>, 2016-01-20; first said at RailsConf 2014).

Vocabulary the skill relies on:

- **Screaming architecture.** Robert C. Martin, 2011-09-30. The top-level structure should "scream" the
  domain, such as health care or accounting, and not the framework. "Architectures should not be supplied
  by frameworks." "Good architectures are centered around use-cases." "You shouldn't need the web server
  running in order to run your tests." The same goes for the database
  (<https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html>). Note the tension:
  Martin wants use cases kept apart from frameworks, while Bogard-style slices sit directly on
  ASP.NET/EF and are tested through them.
- **Package by feature / component.** Simon Brown names three styles. "Package by layer" splits code by
  technical role. "Package by feature" slices it "based upon related features, domain concepts, bounded
  contexts, or aggregate roots." "Package by component" bundles a coarse-grained component behind one
  interface, with everything else non-public so that "we can rely on the compiler to enforce this
  architectural principle" (<https://simonbrown.je/modular-monolith>, GOTO Berlin 2018). The C# analog is
  `internal` plus assembly boundaries.

---

## 2. Folder and file layouts in practice

### 2a. Real examples

**Bogard, ContosoUniversity (CONTOSO, net10.0, Razor Pages):** single project. There is no `Features/`
folder: the Razor Pages tree *is* the feature tree.

```text
ContosoUniversity/
  Data/SchoolContext.cs                       # one DbContext, holds Begin/Commit/RollbackTransaction
  Infrastructure/                             # cross-cutting only
    DbContextTransactionPageFilter.cs         # transaction per page handler
    ValidatorPageFilter.cs                    # ModelState -> 400 JSON
    EntityModelBinder*.cs, Tags/*             # HtmlTags conventions
  Models/Course.cs, Student.cs, ...           # shared EF entities (the domain)
  Pages/Courses/Create.cshtml(.cs)            # one use case per page
  Pages/Courses/Edit.cshtml(.cs)
  Pages/Instructors/CreateEdit.cshtml(.cs)    # one slice serving two verbs on purpose
  MappingExtensions.cs, PaginatedList.cs
ContosoUniversity.IntegrationTests/
  SliceFixture.cs                             # WebApplicationFactory + Testcontainers + Respawn
  Pages/Courses/EditTests.cs                  # tests mirror Pages/<Area>/<UseCase>
```

One file per use case, with nested types. `Pages/Courses/Edit.cshtml.cs` holds the `PageModel`, `Query`,
`QueryValidator`, `QueryHandler`, `Command` (a `record`), `MappingProfile`, `CommandValidator`, and
`CommandHandler`. The query returns the `Command` type as the form model, so request and response are
shared *inside* the slice. `CommandHandler` changes tracked entities and does **not** call `SaveChanges`.
The page filter commits (`SchoolContext.CommitTransactionAsync` calls `SaveChangesAsync`, then commits).
Packages (`ContosoUniversity.csproj`): MediatR 14.0.0-beta-1, AutoMapper 16.0.0-beta-1,
FluentValidation.AspNetCore 11.3.1, EF Core 10.

**Ardalis `min-clean` template (single project, vertical slices):** the README describes it as "a single
project organized by vertical slices, with layer boundaries enforced by NsDepCop"
(<https://github.com/ardalis/CleanArchitecture>).

```text
MinimalClean.Architecture.Web/
  CartFeatures/AddToCart/AddToCartEndpoint.cs     # FastEndpoints (REPR)
  CartFeatures/AddToCart/AddToCartHandler.cs      # record command + handler in one file
  CartFeatures/Checkout/..., CartFeatures/GetById/...
  CartFeatures/CartDto.cs, CartResponse.cs        # shared within the feature area
  ProductFeatures/...
  Domain/CartAggregate/Cart.cs, Specifications/CartByIdSpec.cs
  Infrastructure/Data/AppDbContext.cs, EfRepository.cs, EventDispatcherInterceptor.cs
  Configurations/MediatorConfig.cs, LoggingBehavior.cs
  config.nsdepcop                                 # compile-time namespace rules
```

The handler uses `Mediator` (martinothamar source generator) `ICommandHandler<,>`, returns
`ValueTask<Result<CartDto>>` (Ardalis.Result), and goes through `IRepository<Cart>` + Ardalis.Specification.
So this template keeps repositories, unlike Bogard and Wolverine. `config.nsdepcop` makes illegal
dependencies a build **error**:

```xml
<Disallowed From="MinimalClean.Architecture.Web.Domain.*" To="MinimalClean.Architecture.Web.CartFeatures.*" />
<Disallowed From="MinimalClean.Architecture.Web.CartFeatures.*" To="MinimalClean.Architecture.Web.Infrastructure.*" />
<!-- Feature slices are independent of one another; share via Domain instead. -->
<Disallowed From="MinimalClean.Architecture.Web.CartFeatures.*" To="MinimalClean.Architecture.Web.ProductFeatures.*" />
<AnalyzerConfig><IssueKind Name="IllegalDependency" Severity="Error" /></AnalyzerConfig>
```

**Jason Taylor Clean Architecture template (layered projects, feature folders inside Application):**
`src/{Domain,Application,Infrastructure,Web,Shared,AppHost,ServiceDefaults}`. Inside Application the layout
is `TodoItems/Commands/CreateTodoItem/{CreateTodoItem.cs,CreateTodoItemCommandValidator.cs}` and
`Common/{Behaviours,Exceptions,Interfaces,Models,Security}`. The README lists MediatR, FluentValidation,
and "NUnit, Shouldly, Moq & Respawn", targeting .NET 10 (<https://github.com/jasontaylordev/CleanArchitecture>).
This is a **hybrid**: project layers on the outside, feature folders in the Application project.

**Wolverine guidance:** "Wolverine is well suited for a 'Vertical Slice Architecture' approach". Name the
file "after the command or query message, even for HTTP endpoints". One file holds the message, an
optional validator as an inner type, and the handler or endpoint
(<https://wolverinefx.net/tutorials/vertical-slice-architecture.html>).

**Community practice (secondary):** Milan Jovanović shows a folder per feature with separate files
(`GetActivityEndpoint.cs`, `GetActivityQueryHandler.cs`, `CreateActivity.Validator.cs`)
(<https://www.milanjovanovic.tech/blog/vertical-slice-architecture>, 2023-11-04). Derek Comartin:
"Things that change together (features) belong together", and "features may share an underlying rich
domain model" (<https://codeopinion.com/restructuring-to-a-vertical-slice-architecture/>, 2021-09-01).

### 2b. Layout dimensions

| Dimension | Options seen in primary sources | Evidence |
| --- | --- | --- |
| Granularity of file | One file per use case with nested types; or one folder per use case with a file per type | Nested: CONTOSO, Wolverine docs. Folder: ardalis min-clean, Jason Taylor |
| Top-level folder | `Pages/<Area>/<UseCase>` (Razor Pages); `Features/<Area>/<UseCase>`; `<Area>Features/<UseCase>`; `<Area>/Commands/<UseCase>` | CONTOSO; Bogard 2018 Razor post; ardalis; Jason Taylor |
| Projects | Single web project (CONTOSO, ardalis min-clean); layered projects with feature folders inside (Jason Taylor, ardalis full template "Core, UseCases, Infrastructure, Web") | READMEs above |
| Domain location | Shared `Models/` or `Domain/` used by every slice, never owned by one slice | CONTOSO `Models/`, ardalis `Domain/` |
| Enforcement | None (CONTOSO); NsDepCop analyzer (ardalis); architecture tests (§8) | — |

### 2c. Modular monolith variant

Microsoft's e-book allows a monolith that is "not monolithic but organized into several libraries,
components, or layers" in one deployment (WEBARCH). It says "If you can't deliver independent feature
slices of the application, separating it only adds complexity." Bounded contexts should "evolve
independently" and own their persistence
(<https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles#common-design-principles>).
The vertical-slices-inside-modules layout (`Modules/<Module>/Features/<UseCase>`) has **no Microsoft or
Bogard reference sample** that I found. Treat it as **community practice**. Brown's "package by component"
is the primary source for the module-boundary idea.

---

## 3. High-level patterns

| Pattern | What primary sources say | Notes for a tool-agnostic skill |
| --- | --- | --- |
| **Mediator (generic)** | An in-memory mediator "is smart enough to redirect to the right command handler based on the type of the command." It lets you add "an open number of cross-cutting concerns like logging, validations, audit, and security" through "decorators (or pipeline behaviors)" (MSAPP). Bogard: the mediator gives "Request-in, response-out", "quite valuable in building consistently behaving tests" (quoted in MSAPP). | Optional. VSA18 does not mention a mediator. Direct DI injection of a handler class is equally valid. |
| **CQRS** | "Separate write operations, or commands, from read operations, or queries" (<https://learn.microsoft.com/azure/architecture/patterns/cqrs>). The simplest form is "just separating the queries from the commands and using the same database" (<https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/eshoponcontainers-cqrs-ddd-microservice>). Azure says CQRS may not suit cases where "The domain or the business rules are simple" or where "A simple CRUD-style user interface ... [is] sufficient." | VSA does not require it. Bogard says VSA gives CQRS "out of the gate" (VSA18), which makes it a side effect, not a rule. |
| **Pipeline / behaviors / decorators** | MSAPP: behaviors are like AOP but "explicit and applied only in the context of the mediator, so debugging is much more predictable." Bogard: "The thing you can't do with trivial OOP techniques are cross-cutting concerns", and "the Microsoft DI container does not support decorators" (2022 post). | The DI-decorator claim is from 2022. I did not re-check it against .NET 11 DI docs (**unverified for current**). |
| **Endpoint filters (Minimal APIs)** | Filters "run code before and after the endpoint handler", "inspect and modify parameters", "intercept the response". Before-`next` code runs FIFO, after-`next` code runs FILO. Filters on a group act as if added to each endpoint (<https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0>). `IEndpointFilter.InvokeAsync(EndpointFilterInvocationContext, EndpointFilterDelegate)` returns `ValueTask<object?>`. | The framework-native pipeline for HTTP slices. No mediator needed. |
| **Route groups** | `MapGroup` sets a common prefix, and `RequireAuthorization`/`WithMetadata` apply to the whole group (<https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups>). | One group per feature area is a natural way to register slices. |
| **REPR** | "The REPR Design Pattern defines web API endpoints as having three components: a Request, an Endpoint, and a Response." It argues to "do away with multi-action Controllers entirely" (<https://deviq.com/design-patterns/repr-design-pattern>). FastEndpoints "nudges you towards the REPR Design Pattern" (<https://fast-endpoints.com/>). | REPR is the HTTP-edge form of one slice. DevIQ's REPR page does not mention VSA. |
| **TypedResults** | Prefer `TypedResults` over `Results`. `Results<T1,…>` gives a compile-time check that a handler returns only the declared results, plus OpenAPI metadata (<https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0>). | Gives each slice an explicit response contract. |
| **DTOs as records** | Minimal APIs validate DataAnnotations on record parameters (§6). For MVC record binding, "validation and binding metadata on parameters is used. Any metadata on properties is ignored" (<https://learn.microsoft.com/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0#constructor-binding-and-record-types>). | `sealed record` request/response per slice is consistent with both. |
| **Request/response per slice** | CONTOSO defines `Query`, `Command`, and `Model`/`Result` types nested in each page. They are never reused across pages. | The skill's "no shared `OrderDto`" rule matches the sample. Reuse inside one slice (Edit's `Query` returns `Command`) also matches it. |

---

## 4. Dependency rules and cross-slice communication

### 4a. What may reference what

Primary-source-backed rules:

| Rule | Source |
| --- | --- |
| Slice → slice: not allowed | Bogard: "minimize coupling between slices" (VSA18). ardalis `config.nsdepcop`: "Feature slices are independent of one another; share via Domain instead." |
| Slice → domain model (entities, aggregates, domain services): allowed. The domain is shared. | CONTOSO `Models/` used by all pages. ardalis `Domain/`. Comartin (community): "features may share an underlying rich domain model." |
| Domain → slice: not allowed | ardalis: `Disallowed From="...Domain.*" To="...CartFeatures.*"`. Clean Architecture: the core "has no dependencies on other application layers" (WEBARCH). |
| Slice → infrastructure (DbContext, cross-cutting): **disputed** | Allowed in CONTOSO, where handlers inject `SchoolContext`. Wolverine says "recommend against wrapping your persistence tooling like Marten or EF Core with any kind of repository abstractions." **Not allowed** in ardalis min-clean: "Feature slices depend on Domain abstractions, not Infrastructure implementations." |
| Cross-cutting code knows no slice | Implied by the pipeline/behavior guidance (MSAPP). No primary source states it as a rule. **Community opinion.** |

### 4b. Cross-slice communication options

| Option | For | Against | Source |
| --- | --- | --- | --- |
| **Shared query or helper** (method on DbContext, extension on `IQueryable`/`DbSet`) | Simple. Keeps the mediator at the edge. | Shared code can grow into a hidden service layer. | Bogard, "Dealing with Duplication in MediatR Handlers" (2016-12-12): for common queries use a "Method on the DbContext or an extension method to IQueryable or DbSet". For domain behavior use a "Method on your domain model or perhaps a domain service". Inside one feature folder, "a base handler class for that feature folder would be a good idea." <https://lostechies.com/jimmybogard/2016/12/12/dealing-with-duplication-in-mediatr-handlers> |
| **Handler calls handler** (through the mediator) | Reuse. | "I'm not a fan of this approach – it gets quite confusing" (Bogard 2016, same post). He wants MediatR "to serve as the outermost window." | Microsoft's eShopOnContainers does it once on purpose: `IdentifiedCommandHandler` re-sends the inner command for idempotency (MSAPP). |
| **Domain events, in process** | Side effects across aggregates stay explicit. Open/closed. | Transaction scope must be chosen. Harder to trace. | "Use domain events to explicitly implement side effects across multiple aggregates." Dispatch "right before" `SaveChanges` keeps one transaction. Dispatch after means eventual consistency (<https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation>). ardalis min-clean dispatches from an EF `EventDispatcherInterceptor`. |
| **Notifications / integration events through a broker** | Decoupled and asynchronous. | Needs an outbox and idempotent consumers. | Azure CQRS page: use the Transactional Outbox and idempotent consumers when publishing events from writes (<https://learn.microsoft.com/azure/architecture/patterns/cqrs>). |

---

## 5. Unit of work vs. repository in EF Core

| Fact | Source |
| --- | --- |
| "A DbContext instance represents a session with the database and can be used to query and save instances of your entities. **DbContext is a combination of the Unit Of Work and Repository patterns.**" | <https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext?view=efcore-10.0> |
| `DbSet<TEntity>` docs say only that it "can be used to query and save instances of `TEntity`." Microsoft gives the repository role to **DbContext**, not to DbSet. The plan's "DbSet is the repository" is a fair paraphrase, but not Microsoft's wording. | <https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbset-1?view=efcore-10.0> |
| "EF DbContext implements both the Repository and the Unit of Work patterns." UoW runs on `SaveChanges`. "The Unit of Work pattern can be implemented with or without using the Repository pattern." | MSPERSIST |
| "Repositories shouldn't be mandatory." Bogard's feedback is quoted: "I'm really not a fan of repositories, mainly because they hide the important details of the underlying persistence mechanism ... Going CQRS meant that we didn't really have a need for repositories any more." | MSPERSIST |
| A DbContext "is designed to be used for a *single* unit-of-work". "In many web applications, each HTTP request corresponds to a single unit-of-work", and `AddDbContext` registers it as scoped. | <https://learn.microsoft.com/ef/core/dbcontext-configuration/> |
| Using DbContext directly "result[s] in the simplest code". Custom repositories help when you need to mock data access in unit tests. | <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core> |
| Soft delete and multi-tenancy can be enforced in the model with **global query filters**. EF 10 adds **named** filters: `HasQueryFilter("SoftDeletionFilter", ...)` and `IgnoreQueryFilters(["SoftDeletionFilter"])`. | <https://learn.microsoft.com/ef/core/querying/filters>, <https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew#named-query-filters> |
| Testing view: for a reliable test double, "a repository layer is the only approach allowing the comprehensive and reliable stubbing/mocking of the data layer" but has "a significant cost". The repository "cannot expose any IQueryable-returning methods". Repositories also help "if your application needs to support more than one database." | EFTEST |

When a repository still earns its place (from the sources above): (1) you want unit tests that stub
query *outputs* (EFTEST); (2) the store is not EF, or you need several providers (EFTEST); (3) an aggregate
has complex load rules and many slices load it. Point (3) is **community opinion**, and the ardalis
template's choice is an example of it. Soft delete and tenant isolation are **not** good reasons on their
own, because global query filters already cover them.

Transaction boundary seen in practice: CONTOSO uses one transaction per Razor Page handler
(`DbContextTransactionPageFilter`) and handlers do not call `SaveChanges`. ardalis's
`AddAsync saves by default` comment shows the repository saving per call. MS eShop dispatches domain events
inside `SaveChanges` (domain-events page above).

---

## 6. Validation and error handling

### 6a. Validation options

| Option | Facts | Source |
| --- | --- | --- |
| **.NET 10 built-in Minimal API validation** | `builder.Services.AddValidation();` registers validation. "An endpoint filter performs validation" before the handler. It covers query, header, and body. Failure returns 400 with details. Records are supported: `public record Product([Required] string Name, [Range(1, 1000)] int Quantity);`. Opt out with `.DisableValidation()`. Errors flow through `IProblemDetailsService`. | <https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#minimal-apis> |
| — package and status | APIs moved to `Microsoft.Extensions.Validation` (NuGet now 11.0.0). The top-level `AddValidation` and the filter are stable. The resolver APIs and `[ValidatableType]`/`[SkipValidation]` are **experimental in .NET 10**. | Same page; VALID |
| — gotchas | (1) A source generator "only discovers validatable types within the assembly where `AddValidation` is called." Split-project VSA must call it per assembly. (2) Missing metadata "doesn't produce a runtime exception or log entry". Invalid input reaches the handler without warning. (3) It does not reject unmapped JSON properties unless `UnmappedMemberHandling = Disallow`. (4) "The API isn't supported for MVC or Razor Pages." (5) Before .NET 11, nullable value-type parameters were not validated (dotnet/aspnetcore #67033). | VALID |
| **.NET 11 additions** | Async validation (`AsyncValidationAttribute`, `IAsyncValidatableObject`). Built-in localization of validation messages. Endpoint filters now run on parameter-binding failures. C# 15 **union types** serialize in Minimal APIs and MVC, and OpenAPI shows them as `anyOf`. | <https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11?view=aspnetcore-10.0> |
| **DataAnnotations in MVC / Razor Pages** | The model-validation pipeline. For records, parameter attributes are used and property attributes are ignored. | model-binding link in §3 |
| **Options validation** | `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`, or `AddOptionsWithValidateOnStart<T>()`. The `[OptionsValidator]` source generator is reflection-free and AOT-friendly. | <https://learn.microsoft.com/dotnet/core/extensions/options#options-validation>, <https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator> |
| **FluentValidation-style pipeline** | FluentValidation's own docs call ASP.NET auto-validation "no longer recommended". It is "not asynchronous" and "only works with MVC Controllers and Razor Pages". For Minimal APIs "use a filter". CONTOSO still uses `AddFluentValidationAutoValidation()`. MS eShop uses a mediator `ValidatorBehavior` that throws on failure, and notes "you could also return a result object" (MSAPP). | <https://docs.fluentvalidation.net/en/latest/aspnet.html> |
| **Domain invariants** | "All the domain logic should be contained in the domain classes", not in the command handler (MSAPP). | MSAPP |

### 6b. Results vs. exceptions, ProblemDetails

- Microsoft performance guidance: "Exceptions should be rare ... exceptions shouldn't be used to control
  normal program flow". **Do not** throw or catch as normal flow, "especially in hot code paths"
  (<https://learn.microsoft.com/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0#minimize-exceptions>).
  For the related "return null/default for common error cases", see
  <https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions>.
- MS eShop takes the other side: a command handler result "should be either successful execution of the
  command, or an exception" (MSAPP). The Microsoft sources do not agree, so the choice belongs to the team.
- Result-type examples: ardalis min-clean returns `Result<T>` (`Result.NotFound(...)`) and maps it with
  `Ardalis.Result.AspNetCore`. Bogard lists "Baking in support for result patterns" on the MediatR roadmap
  (<https://www.jimmybogard.com/automapper-and-mediatr-roadmaps/>, 2025-07-08). .NET 11 C# union types
  are a native option (§6a). Whether to use them for results is an **open question**.
- ProblemDetails: `AddProblemDetails()` + `UseExceptionHandler()` + `UseStatusCodePages()` produce RFC
  9457/7807 bodies for error responses that have no body yet
  (<https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0>).
- `IExceptionHandler`: register with `AddExceptionHandler<T>()`. Handlers are singletons and run in
  registration order until one returns `true`. They only run if `UseExceptionHandler` is called. **Since
  .NET 10, handled exceptions no longer emit diagnostics by default** (set `SuppressDiagnosticsCallback` to
  change this). A handler that returns `true` must write the status code, or the response becomes 404
  (<https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0#iexceptionhandler>).
- `ThrowOnBadRequest` defaults to `true` only in Development. In Production, binding failures write 400
  directly and skip custom exception handlers (error-handling-api page above).

---

## 7. Cross-cutting concerns

| Concern | Mechanism (framework-native first) | Source |
| --- | --- | --- |
| Logging / timing | Endpoint filter or mediator behavior. MS eShop `LoggingBehavior`. ardalis `LoggingBehavior<,>` registered in `options.PipelineBehaviors`. | MSAPP; ardalis `MediatorConfig.cs` |
| Validation | §6 | — |
| Authorization | `RequireAuthorization()` / `[Authorize]` per endpoint or per `MapGroup`; `AllowAnonymous()` | <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#authorization> |
| Transactions | Filter around the handler (CONTOSO `DbContextTransactionPageFilter`). Or one `SaveChangesAsync` per command (implicit EF transaction). Or a mediator behavior (**community practice**). | CONTOSO; EF DbContext lifetime page |
| Caching (HTTP) | `CacheOutput()` / `[OutputCache]` per endpoint or group. Named policies. Tag eviction. By default only GET/HEAD 200 responses are cached, and authenticated responses are not. | <https://learn.microsoft.com/aspnet/core/performance/caching/output?view=aspnetcore-10.0> |
| Error mapping | `IExceptionHandler` + ProblemDetails (§6b) | — |
| Rate limiting, CORS, short-circuit | Middleware plus endpoint metadata. .NET 11 adds a `[ShortCircuit]` attribute. | aspnetcore-11 release notes |

Microsoft's controllers-to-Minimal-API migration advice: "Replace action filters with endpoint filters or
middleware", and "Replace exception filters with exception handling middleware" (error-handling-api page).

---

## 8. Testing

### 8a. What the primary sources do

- **Bogard / CONTOSO: integration-first, through the mediator, against a real database.** The test project
  has **no unit tests**. `SliceFixture` builds a `WebApplicationFactory<Program>` and starts SQL Server
  with Testcontainers (`new MsSqlBuilder()...Build()`, `StartAsync()`, `GetConnectionString()`). It resets
  data with Respawn (`Respawner.CreateAsync(cs)`, `ResetAsync(cs)`) and runs each test in a DI scope and
  transaction. Tests call `_fixture.SendAsync(new Edit.Query { Id = ... })`, which hits the handler through
  real DI, real EF, and a real DB but **not HTTP**. Tests mirror the feature tree
  (`IntegrationTests/Pages/Courses/EditTests.cs`).
- **EF Core team:** "We recommend that developers have good test coverage of their application running
  against their actual production database system." "Avoid the in-memory provider for testing purposes."
  "Avoid mocking `DbSet` for querying purposes." If you need a test double, use a repository and not a fake
  provider (EFTEST). The in-memory provider is "highly discouraged", and "New features are not being
  added" (<https://learn.microsoft.com/ef/core/providers/in-memory/>).
- **WebApplicationFactory:** package `Microsoft.AspNetCore.Mvc.Testing`. Expose `Program` with
  `public partial class Program { }` or `InternalsVisibleTo`. Override services with
  `ConfigureTestServices` through `WithWebHostBuilder`. Mock auth with a test `AuthenticationHandler`
  (<https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0>).
- **Testcontainers for .NET:** "a library to support tests with throwaway instances of Docker containers"
  (<https://dotnet.testcontainers.org/>). NuGet `Testcontainers.MsSql` 4.16.0.
- **Respawn:** "a small utility to help in resetting test databases to a clean state". Supports SQL Server,
  PostgreSQL, MySQL, Oracle, and Informix (<https://github.com/jbogard/Respawn>). NuGet 7.0.0.
- **Unit-testing handlers:** a Minimal API static handler that returns `TypedResults` can be unit-tested by
  asserting the result type (`Assert.IsType<Ok<Todo[]>>(result)`)
  (<https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0#use-the-typedresults-api>).
  Pure domain logic is unit-tested without a DB. A handler that uses `DbContext` directly cannot be
  reliably unit-tested for queries (EFTEST).

### 8b. Architecture tests — real APIs

**NetArchTest.Rules** (MIT, NuGet 1.3.2, last release **2021-05-23**: low activity). `ResideInNamespace`
is a **case-insensitive `StartsWith` on the type's full name** (`FunctionDelegates.cs`). So
`"MyApp.Features.Order"` also matches `MyApp.Features.Orders`. Include the trailing dot. `HaveDependencyOn*`
matching is also prefix-based (`NamespaceTree`, "same results as ... String.StartsWith").
`TestResult.FailingTypeNames` exists.

```csharp
using NetArchTest.Rules;

[Fact]
public void Feature_slices_do_not_reference_each_other()
{
    var assembly = typeof(Program).Assembly;
    const string root = "MyApp.Features.";
    var features = assembly.GetTypes()
        .Select(t => t.Namespace)
        .Where(ns => ns is not null && ns.StartsWith(root, StringComparison.Ordinal))
        .Select(ns => ns!.Substring(root.Length).Split('.')[0])
        .Distinct()
        .ToArray();

    foreach (var feature in features)
    {
        var others = features.Where(f => f != feature).Select(f => root + f + ".").ToArray();
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespace(root + feature + ".")
            .ShouldNot().HaveDependencyOnAny(others)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}

[Fact]
public void Domain_does_not_depend_on_features() =>
    Assert.True(Types.InAssembly(typeof(Program).Assembly)
        .That().ResideInNamespace("MyApp.Domain.")
        .ShouldNot().HaveDependencyOn("MyApp.Features")
        .GetResult().IsSuccessful);
```

The trailing-dot argument to `HaveDependencyOnAny` is **unverified**: `NamespaceTree` splits on dots, so
check that `"MyApp.Features.Billing."` still matches types in `MyApp.Features.Billing.*`. If it does not,
drop the dot and accept the prefix risk.

**NetArchTest.eNhancedEdition** (fork, NuGet 1.4.5, released 2025-06-04) has a built-in slice rule. Types
are grouped by the namespace segment after the prefix. "Dependency from slice to type that is not part of
any other slice is allowed" (<https://github.com/NeVeSpl/NetArchTest.eNhancedEdition>):

```csharp
var result = Types.InAssembly(typeof(Program).Assembly)
    .Slice()
    .ByNamespacePrefix("MyApp.Features")
    .Should()
    .NotHaveDependenciesBetweenSlices()
    .GetResult();
```

**ArchUnitNET** (Apache-2.0, `TngTech.ArchUnitNET` + `TngTech.ArchUnitNET.xUnit` 0.13.5, released
2026-10-26; MSTestV2 and NUnit packages also exist). In 0.13.x, `ResideInNamespace(string fullName)` is
**exact**. Use `ResideInNamespaceOrBelow` or `ResideInNamespaceMatching(regex)` for subtrees. Slice
patterns need `(*)` or `(**)`. The `"Prefix.(*).."` form comes from the library's own tests.

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public class ArchitectureTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(Program).Assembly).Build();

    [Fact]
    public void Feature_slices_do_not_depend_on_each_other() =>
        SliceRuleDefinition.Slices()
            .Matching("MyApp.Features.(*)..")
            .Should().NotDependOnEachOther()
            .Check(Architecture);

    [Fact]
    public void Domain_does_not_depend_on_features() =>
        Types().That().ResideInNamespaceOrBelow("MyApp.Domain")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("MyApp.Features"))
            .Check(Architecture);
}
```

Sources: ArchUnitNET `documentation/docs/guide.md` (shows `Slices().Matching("Module.(*)").Should().BeFreeOfCycles()`,
`.Check(Architecture)`, `TngTech.ArchUnitNET.xUnit`); `ArchUnitNET/Fluent/Slices/SlicesShould.cs`
(`NotDependOnEachOther()`, `BeFreeOfCycles()`); `AddTypePredicate.cs` (`ResideInNamespace`,
`ResideInNamespaceOrBelow`, `ResideInNamespaceMatching`). Repo: <https://github.com/TNG/ArchUnitNET>.

**Compile-time alternative:** NsDepCop analyzer config (ardalis, §2a) fails the *build* with
`IllegalDependency` = Error, and needs no test project (<https://github.com/realvizu/NsDepCop>, as
referenced by the ardalis config).

---

## 9. Comparisons

| Style | Organizing axis (primary definition) | What VSA keeps | What VSA drops |
| --- | --- | --- | --- |
| **N-layer (UI/BLL/DAL)** | "UI layer ... interacts only with the BLL", and the BLL "should only interact with persistence by going through the DAL". Downside: the BLL depends on data-access details (WEBARCH). | One deployable unit. Optional reuse of low-level helpers. | The mandatory hops between layers. The service/repository per entity. |
| **Clean / Onion / Hexagonal (Ports & Adapters)** | WEBARCH treats them as one family: "Hexagonal ... Ports-and-Adapters ... Onion ... Clean". Dependencies point inward to the Application Core, and Infrastructure implements Core interfaces. | Dependency direction for the domain (domain depends on nothing). Optional ports for external systems. | A project per layer as the top-level structure. Interfaces with one implementation. Repositories as a rule. ardalis min-clean keeps Clean's rules but slices the folders, so the two can mix. |
| **Traditional monolith** | "Entirely self-contained ... deployed as a single unit" (WEBARCH). | Single deployment. | — (VSA is an internal organization choice, not a deployment choice). |
| **Modular monolith** | Single code tree. Components behind interfaces, enforced by visibility (Brown). | Modules can contain slices. | — (They work at different levels: modules by bounded context, slices by request. **Community practice.**) |
| **Microservices** | Each service implements "a specific end-to-end domain or business capability within a certain context boundary" (<https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/microservices-architecture>). | Organizing by business capability. | Network boundaries. VSA is in-process. |

Microsoft still says the Clean variant is "the most appropriate way to structure non-trivial monolithic
applications" (WEBARCH). Microsoft does **not** endorse VSA in prose beyond the "feature folders / feature
slices" section of the MVC e-book.

---

## 10. Migration and bootstrapping

### 10a. Migration from layered to VSA

- **Strangler Fig (Azure Architecture Center):** "Incrementally migrate a legacy system by gradually
  replacing specific pieces of functionality". A façade routes requests to the old or new path, and the old
  path is removed when it has no dependents. It may not suit cases where "You migrate a small system and
  replacing the whole system is simple" (<https://learn.microsoft.com/azure/architecture/patterns/strangler-fig>).
  Inside one codebase, the "façade" is the route table. Move one endpoint at a time to a slice. This
  in-process reading is **community opinion**: Azure describes the pattern at system level.
- **Bogard's .NET migration series** (2023-09-27) migrates the "controller and its vertical slice of backend
  and frontend functionality". "Only Migrate What's Required." "It can be tempting to try and port all
  business logic all at once." Migrate dependencies such as `ILogger<T>` first
  (<https://www.jimmybogard.com/tales-from-the-net-migration-trenches-migrating-business-logic/>). This is a
  Framework → Core migration, not a layered → VSA one. It is the nearest first-party account of
  feature-by-feature moves.
- Comartin (community): "Then change the dependency in this feature". Restructuring is done feature by
  feature (codeopinion link above).

### 10b. Bootstrapping a new VSA project (facts the steps can rely on)

- `dotnet new web` or `webapi` gives a single-project Minimal API app. The `webapiaot` template "Uses
  Minimal APIs only, as MVC isn't yet compatible with Native AOT" and enables the JSON source generator
  (<https://learn.microsoft.com/aspnet/core/fundamentals/native-aot?view=aspnetcore-10.0>).
- Register `AddProblemDetails`, `UseExceptionHandler`, and `AddValidation` (if .NET 10+). Map each feature
  area with `MapGroup`. Add one test project with `WebApplicationFactory<Program>` and expose `Program`
  (§8a).
- Templates that ship a sliced layout today: `ardalis` `min-clean`. Jason Taylor's template is layered with
  feature folders.

### 10c. Adding a slice (facts)

- One folder or file named after the use case. A `sealed record` request and response. A handler (static
  method, class, or mediator handler). An endpoint registration in the area's group. A test in the
  mirrored test folder (CONTOSO, ardalis).
- If validation uses `AddValidation`, the request type must live in the assembly that calls it (VALID).

---

## 11. Code smells and anti-patterns

| Smell | Primary evidence | Status |
| --- | --- | --- |
| **Shared-kernel creep** (a growing `Common/` with business rules) | Evans's Shared Kernel is a *context-mapping* pattern: "Designate with an explicit boundary some subset of the domain model that the teams agree to share. **Keep this kernel small.**" It "shouldn't be changed without consultation with the other team" (DDDREF p. 31). The DDD term is about two teams and bounded contexts, not a utilities folder. | The term is misused when applied to a `Common/` folder (see Contradictions). |
| **Handler calling handler** | Bogard: "I'm not a fan of this approach – it gets quite confusing" (2016). "I don't ever see `Send` outside of a controller/page" (2022). | Primary. |
| **God `Common`/`Shared` folder** | Bogard 2018: "keep our cross-slice logic sharing to a minimum." No primary source sets a threshold. | Thresholds are **community opinion**. |
| **Generic repository over EF** | Bogard (in MSPERSIST): repositories "hide the important details of the underlying persistence mechanism." Wolverine recommends against them. EFTEST: a repository must return `IEnumerable`, not `IQueryable`, to be stubbable. A generic `IQueryable` repository gives neither benefit. | Primary (Bogard, Wolverine, EF team). ardalis disagrees in practice. |
| **Anemic over-abstraction** (interfaces with one implementation, service per entity) | VSA18 rejects "Controller MUST talk to a Service that MUST use a Repository". WEBARCH notes layered DI makes swapping easy, which is only useful if you do swap. | Primary for the rejection. "Interface with one impl" as a smell is **community opinion**. |
| **Inconsistent slice granularity** | No primary source. Bogard's own `Instructors/CreateEdit` slice merges two verbs on purpose. | **Community opinion.** The sample shows that merging can be intentional. |
| **Leaking EF entities to the API** | CONTOSO projects to slice-specific types (`ProjectTo<Command>`). Microsoft: queries "return data transfer objects (DTOs)" (Azure CQRS page). Exposing entities is not argued directly by MS. | Partial. |
| **Exceptions as control flow** | MS best practices (§6b). | Primary (performance guidance). |
| **Validation silently skipped** | `AddValidation` in the wrong assembly → "No automatic validation runs" with no log (VALID). | Primary. A new, .NET 10-specific smell. |

---

## 12. Often overlooked

- **Observability per slice.** Create one `ActivitySource` and `Meter` per app or component, stored
  statically, with hierarchical names. Each source must be added with `AddSource()`
  (<https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs>).
  Activity names per slice and metric tags by slice are **community practice**. No primary source.
- **Authorization per slice.** Use endpoint or group metadata (§7). Resource-based checks inside handlers
  are not covered by any VSA primary source (**unverified** as VSA guidance).
- **Versioning.** `Asp.Versioning.Http` (NuGet 10.2.3). The official sample uses `app.NewVersionedApi()`,
  `.HasApiVersion(1.0)`, and `MapGroup(...).HasApiVersion(2.0)`
  (<https://github.com/dotnet/aspnet-api-versioning/blob/main/examples/AspNetCore/WebApi/MinimalApiExample/Program.cs>).
  A versioned endpoint is a new slice, or a group over existing slices. This is a design choice; no
  primary VSA source covers it.
- **Background jobs and messages as slices.** Bogard: the mediator lives "on the edges", and a message
  consumer is an edge like a controller (2022 post). Wolverine treats message handlers and HTTP endpoints
  the same way. MS eShop starts `CreateOrderCommand` from an integration-event handler (MSAPP).
- **UI slices.** Razor Pages is Bogard's canonical slice host (2018 posts, CONTOSO). For Blazor,
  Microsoft says Blazor apps "should be built as separate projects", with shared types in "a common shared
  project" (MVC e-book link in §1). No primary source shows Blazor components organized as VSA slices
  (**unverified**).
- **Source generators and AOT.**
  - Native AOT "can't use unbounded reflection". Libraries that use reflection to discover types or
    generate code at runtime need updates (<https://learn.microsoft.com/aspnet/core/fundamentals/native-aot?view=aspnetcore-10.0>).
  - Minimal APIs have **partial** AOT support through the Request Delegate Generator. MVC has **none**.
    Razor Pages hosts (CONTOSO) cannot be AOT-published.
  - MediatR's `Mediator.cs` uses `MakeGenericType` + `Activator.CreateInstance` at dispatch, and the
    MediatR source has no AOT/trim annotations. MediatR's docs make no AOT claim. That it fails under AOT
    is **inferred, not tested**.
  - `Mediator` (martinothamar, MIT, 3.0.2) claims "Full Native AOT support without reflection or runtime
    code generation", returns `ValueTask`, and "doesn't support generic requests/notifications"
    (<https://github.com/martinothamar/Mediator>).
  - FastEndpoints supports AOT through `FastEndpoints.Generator` and generated serializer contexts
    (<https://fast-endpoints.com/docs/native-aot>).
  - Bogard on mediator source generation: "a bit less interesting in real-world projects" (roadmap, 2025).
- **MediatR licensing (decision input).**
  - Announced 2025-04-16 (<https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/>). It
    launched 2025-07-02 as "Lucky Penny Software", dual-licensed "Reciprocal Public License 1.5 (RPL1.5)"
    and the Lucky Penny commercial license.
  - It applies from **MediatR v13.0**. Earlier packages "are bound by the license agreements at the time
    of their release" (<https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/>).
  - Community edition: under $5M gross revenue, no more than $10M outside capital, not government, not
    higher-ed institutional use. A key is required "only for auditing purposes", and there are "no runtime
    limits" without one (<https://mediatr.io/>).
  - Current release is MediatR 15.0.0 (2026-11-12, targets .NET 11). Repo:
    <https://github.com/LuckyPennySoftware/MediatR>.
  - Alternatives with first-party pages: `Mediator` (MIT), Wolverine (prefers handlers and endpoints over
    "mediator-style forwarding" for GET queries: "we believe that is just unnecessary complexity"),
    FastEndpoints (MIT, REPR).
- **AI / agent navigability.** Bogard ties VSA to AI-assisted development only loosely (2026 webinar). The
  skill's claim that a feature tree helps "a new contributor (human or agent)" is **community opinion**.
  No primary study.

---

## 13. .NET 11 / C# 15: unions and closed hierarchies for slice results

Scope addition: the skill targets .NET 10 and .NET 11 and wants to use C# 15 `union` types and `closed`
classes for handler results.

### 13a. Status

| Fact | Value | Source |
| --- | --- | --- |
| .NET 11 | **GA**: 11.0.0 released 2026-11-10, STS. .NET 10.0.15 (LTS) shipped the same day. | <https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json> |
| C# 15 | Default language version for `net11.0` in the .NET 11 SDK. Features: collection expression arguments, **union types**, **closed hierarchies**, extension indexers, labeled `break`/`continue`, non-virtual static interface members on older runtimes, memory safety (preview). | <https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15>, <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview#c-15> |
| `LangVersion preview` needed? | **No.** "Except for memory safety, these features don't require `<LangVersion>preview</LangVersion>`." | What's new in C# 15 |
| Unions champion | dotnet/csharplang #9662 "[Proposal]: Unions (VS 18.8, .NET 11)". Milestone 15.0. Labels "Implemented Needs ECMA Spec" (still open, waiting only on the ECMA spec). | `gh issue view 9662 -R dotnet/csharplang` |
| Closed hierarchies champion | dotnet/csharplang #9499 "[Proposal]: Closed Hierarchies (VS 18.8, .NET 11)". An open follow-up question, #10237, asks whether direct descendants must share the base's accessibility. | `gh search issues --repo dotnet/csharplang` |
| Runtime dependency of unions | Unions "rely on the `UnionAttribute` and `IUnion` types in the `System.Runtime.CompilerServices` namespace. **The .NET 11 runtime includes these types.**" | <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/union> |
| Using them on `net10.0` | **Unverified.** The docs say the attribute and interface ship in the .NET 11 runtime. Closed hierarchies emit `IsClosedTypeAttribute` (diagnostic CS9395 checks its shape). Whether `LangVersion 15` plus hand-declared polyfills works on `net10.0` is not documented. Treat both features as **.NET 11-only** until tested. For .NET 10, a slice can use a hand-written `abstract record` base with a discard arm, or `Results<...>` alone. | union reference; <https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/union-declaration-errors> |

### 13b. `union` syntax and behavior (primary: language reference and spec)

- Declaration: `public union Pet(Cat, Dog, Bird);`. Case types can be any type that converts to `object`:
  classes, structs, interfaces, type parameters, nullable types, other unions. Generic unions are allowed:
  `public union Result<T>(T, Exception);`. A body may add members, but no instance fields,
  auto-properties, or field-like events, and no public single-parameter constructors.
- Lowering: a `union` declaration becomes a **struct** marked `[Union]` that implements `IUnion`. It has
  one public constructor per case and an `object? Value` property. It "always boxes value-type cases."
  You can write custom (class-based or non-boxing) unions by following the *basic union pattern*.
- **Implicit conversion** from each case type: `Pet pet = new Dog("Rex");`. A user-defined implicit
  operator takes priority. If two case types are equally applicable, the conversion is ambiguous and the
  compiler reports an error.
- **Pattern matching unwraps**: type and declaration patterns test `Value`, not the union. Untyped
  property patterns (`{ Name: "Rex" }`) use the union's own members. `pet is Pet` is error CS8121.
- **Exhaustiveness**: a `switch` that handles every case type needs no discard. **But** a `default` union
  has `Value == null`. If all case types are non-nullable, the compiler may *not* warn about a missing
  `null` arm, and the switch then throws `SwitchExpressionException` at runtime. The docs recommend a
  `null` arm over `_`, because `_` hides warnings when you add a case later.
- Spec: <https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-15.0/unions>.
  Tutorial with `Result<T>` migration: <https://learn.microsoft.com/dotnet/csharp/whats-new/tutorials/unions>.

### 13c. `closed` classes (primary: language reference and spec)

- `public closed record class GateState;` with `public record class Open(float Percent) : GateState;`.
  Direct subtypes must be in the **same assembly and module**. `closed` is a contextual keyword. A closed
  class is implicitly `abstract` and cannot be combined with `sealed`, `static`, or explicit `abstract`
  (CS9381/CS9384). Deriving from another assembly is CS9382.
- Exhaustiveness covers **direct** descendants only. A non-closed subtype can still be extended from other
  assemblies (and still matches its base case). Mark intermediate types `closed` or `sealed` to lock them.
- Accessibility catch: if a direct descendant is less accessible than the base (for example an `internal`
  case under a `public` closed base), switches in other assemblies get CS8509. Keep result cases as
  accessible as the base.
- Generic rule: a generic subtype must use all its type parameters in the base type (CS9383).
- `closed` applies to classes and record classes only. Closed interfaces are mentioned in the spec as a
  possible future addition.
- Sources: <https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/closed>,
  <https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-15.0/closed-hierarchies>,
  <https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns#closed-hierarchy-patterns>.

### 13d. Unions and closed hierarchies vs. ASP.NET Core and System.Text.Json

| Area | Behavior | Source |
| --- | --- | --- |
| JSON (STJ) unions | "`JsonSerializer` writes the active case value", with **no wrapper and no discriminator**. A default union serializes as `null`. Deserialization picks the case by JSON **token kind**. Two object cases need `[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]`, which goes by root property names, or a custom `JsonTypeClassifierFactory`. Works with source generation (`PetJsonContext.Default.PetUnion` in the docs). | <https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/union-types>, <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/libraries> |
| JSON closed hierarchies | `[JsonPolymorphic(InferClosedTypePolymorphism = true)] public closed record Event;` writes `{"$type":"Created","Id":42}`. "The `closed` modifier alone doesn't add a discriminator." Microsoft recommends closed hierarchies over unions "When you control the types and JSON contract". | union-types page, "Choose unions or closed hierarchies" |
| Minimal APIs / MVC bodies | Unions are supported "anywhere System.Text.Json is used: JSON request and response bodies in Minimal APIs and MVC". They are **not** supported for route, query, header, or form binding. | <https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11?view=aspnetcore-10.0> |
| OpenAPI | A returned union is described as an **`anyOf`** schema of its case types. Case components reuse their bare names (no `$type`). `[JsonUnion]` classifiers disambiguate same-shape cases. Swashbuckle and NSwag pick it up through ApiExplorer (`JsonTypeInfoKind.Union`). | aspnetcore-11 notes; dotnet/aspnetcore PR #67001 |
| Validation | Merged for 11.0: "Support validation for C# union cases" (#68846). Fixes for skipped and collection union cases in Minimal API validation (#69870/#69928). "Validate active union cases in MVC" (#69834/#69929). Not checked against Learn prose. | `gh search prs --repo dotnet/aspnetcore union --merged` |
| Union of `IResult` types as an endpoint return | **Unverified, and probably wrong.** A generated union is a struct that does not implement `IResult`, and ASP.NET Core 11 treats unions as JSON bodies. So `union R(Created<T>, NotFound)` returned from an endpoint would likely be JSON-serialized as a 200 body and not executed as a result. No aspnetcore issue or doc covers this (searches for "union IResult" and "union TypedResults" returned nothing). | — |

### 13e. Fitting this into a slice

Pattern that matches the primary sources:

1. Keep the **domain outcome** as a C# union or closed hierarchy owned by the slice.
2. Keep the **HTTP contract** as the existing `Results<T1, …>` union from `TypedResults`. That type
   already gives compile-time return checks and OpenAPI status-code metadata (§3).
3. Map one to the other with an exhaustive `switch` at the endpoint.

```csharp
namespace MyApp.Features.Orders.CreateOrder;

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLine> Lines);
public sealed record OrderCreated(Guid OrderId);
public sealed record OrderInvalid(IDictionary<string, string[]> Errors);
public sealed record CustomerMissing(Guid CustomerId);

// C# 15 / .NET 11: implicit conversion from each case, exhaustive switch.
public union CreateOrderResult(OrderCreated, OrderInvalid, CustomerMissing);

public static class CreateOrderEndpoint
{
    public static async Task<Results<Created<OrderCreated>, ValidationProblem, NotFound>> HandleAsync(
        CreateOrderRequest request, CreateOrderHandler handler, CancellationToken ct)
    {
        CreateOrderResult result = await handler.HandleAsync(request, ct);
        return result switch
        {
            OrderCreated created => TypedResults.Created($"/orders/{created.OrderId}", created),
            OrderInvalid invalid => TypedResults.ValidationProblem(invalid.Errors),
            CustomerMissing => TypedResults.NotFound(),
            null => throw new UnreachableException("Handler returned a default CreateOrderResult."),
        };
    }
}
```

Notes on this example:

- **Naming collision.** The coordinator's sketch `union CreateOrderResult(Created, ValidationFailed,
  NotFound)` uses names that clash with `Microsoft.AspNetCore.Http.HttpResults.Created`/`Created<T>`
  and `NotFound`. Name domain cases after the domain (`OrderCreated`, `CustomerMissing`). This is
  inferred from the namespaces, not a documented warning.
- **The `null` arm** follows the language reference's advice for default unions (§13b). If the handler
  can never return `default`, some teams will drop it. The docs explain why to keep it.
- **Target typing of the switch arms** to `Results<…>` relies on `Results<>`'s implicit conversions plus
  C# target-typed switch expressions. I did not compile this exact snippet: **unverified**. The pattern
  matches the Learn example `Task<Results<Ok<Todo>, NotFound>>`.
- **Closed-hierarchy alternative** for the same slice: `public closed record CreateOrderResult;` with
  `public sealed record OrderCreated(Guid OrderId) : CreateOrderResult;` and so on. You get the same
  exhaustive switch, no boxing concern, and a `$type` discriminator if the outcome is ever serialized
  (`InferClosedTypePolymorphism`). Use it when the cases share a meaning ("an outcome of CreateOrder").
  Use a union when the cases are existing, unrelated types (for example `Result<T>(T, Exception)`).
- **Pipelines and behaviors.** A generic validation behavior that short-circuits must build the
  handler's result type. With a union it can do this only if the union has a known validation case, for
  example through a static abstract interface member. No primary source shows this, so treat it as a
  **design gap**.
- **.NET 10 fallback.** Return `Results<…>` straight from the handler or endpoint, or use an
  `abstract record` base and a discard arm. No language support is needed.

---

## Contradictions with current skill

Each item: skill location → what it says → what the sources say.

1. `references/pitfalls.md` (duplication section): says "duplication is cheaper than the wrong abstraction"
   is "traced to Jimmy Bogard's original framing." **Contradicted.** VSA18 never uses "duplication" or
   "wrong abstraction". The line is Sandi Metz's (2016, RailsConf 2014). Bogard's own duplication advice
   (2016) is pragmatic: extract to a DbContext method, an `IQueryable` extension, a domain method, or a
   per-feature base handler.
2. `references/pitfalls.md` "A commonly cited threshold: two occurrences ... third ... extracting." This is
   the "rule of three". No source in this research. Label it **community opinion** or cite it properly.
3. "Shared kernel" across `SKILL.md` description, `cross-cutting-concerns.md`, and `pitfalls.md` (and the
   plan's leading words). The skill uses it for a `Common/`/`Shared/` folder of cross-cutting
   infrastructure. **Contradicts Evans:** Shared Kernel is a context-mapping pattern for a small, explicitly
   bounded *domain model subset shared between teams* (DDDREF p. 31). Use another term (for example
   "shared infrastructure"), or define the skill's own meaning and note the difference.
4. `references/cross-cutting-concerns.md` "Minimal shared base abstractions ... not a base class with
   virtual methods every handler is expected to override." **Partly contradicted** by Bogard 2016: "a
   base handler class for that feature folder would be a good idea" for duplication *inside* one feature
   folder.
5. `references/data-access-patterns.md` Pattern B lists "soft-delete filters, tenant isolation" as reasons
   for a shared repository. **Weakened:** EF Core global query filters (named in EF 10) enforce both at the
   DbContext. The real repository drivers in the sources are test doubles and multiple providers (EFTEST).
6. `references/slice-anatomy.md` sample handler injects `IOrderRepository`, while `data-access-patterns.md`
   recommends DbContext directly as the "middle ground". **Inconsistent with itself.** CONTOSO (the
   reference sample) injects `SchoolContext` directly.
7. `references/slice-anatomy.md` sample throws `InvalidOperationException` for an empty order (an
   expected input failure). **Conflicts with** Microsoft's "exceptions shouldn't be used to control normal
   program flow" (ASP.NET Core best practices). Input rules belong in request validation (400
   ValidationProblem). Note that MS eShop does accept exceptions from command handlers, so this is a
   choice. But the sample should not model validation as an exception.
8. `references/testing.md` makes handler unit tests with fakes the default, with "every branch in a
   handler gets a unit test" and integration only for "one or two representative paths". **Contradicts
   the primary sources:** Bogard's sample has *only* integration ("slice") tests through the mediator
   against a real DB (Testcontainers + Respawn). The EF Core team recommends starting with
   real-database tests and discourages the in-memory provider and `DbSet` mocking (EFTEST). A
   "FakeOrderRepository" only works if the slice uses a repository, which conflicts with item 6.
9. `references/testing.md` "Integration test ... (a real in-process HTTP pipeline)". **Incomplete:**
   Bogard's `SliceFixture` tests below HTTP, through the mediator inside a DI scope. That is a distinct and
   documented style.
10. `references/fit-and-adoption.md` "CRUD-heavy admin/back-office applications ... fit poorly." **Not
    supported** and arguably contradicted: Bogard's flagship VSA sample (ContosoUniversity) is a CRUD app.
    Microsoft's "not suitable for simple CRUD" warning is about **CQRS with separate models**
    (<https://learn.microsoft.com/azure/architecture/patterns/cqrs>), not VSA. The plan says the
    opposite ("CRUD-heavy line-of-business apps ... fit well"). Settle it, and label it as opinion.
11. `references/philosophy-and-organization.md` "Traditional N-tier/onion/'clean' architecture organizes
    source code by technical role". **Imprecise:** Microsoft defines Clean/Onion by *dependency direction*
    toward the Application Core (WEBARCH). Templates such as ardalis min-clean mix Clean's rules with
    slice folders.
12. `references/philosophy-and-organization.md` treats VSA as a way to get "screaming architecture".
    **Nuance:** Martin's 2011 post also asks that use cases stay independent of frameworks and be testable
    without a web server or database. Bogard-style slices sit on the framework and are tested with a real
    DB. The folder-naming part matches, the framework-independence part does not.
13. Layout guidance (`SKILL.md` quick start, `philosophy-and-organization.md`) shows only `Features/`.
    **Missing primary evidence:** Bogard's canonical layout is `Pages/<Area>/<UseCase>.cshtml.cs` with
    nested types. Bogard moved *from* `Features/` *to* `Pages/` (2018-02-21).
14. `references/cqrs-relationship.md` "neither requires the other." This is consistent, but Bogard says
    slicing "gives me CQRS out of the gate". Quote him instead of stating it neutrally.
15. The skill says nothing about where transactions commit. CONTOSO commits in a page filter, and its
    handlers never call `SaveChanges`. This is a gap, not a contradiction.

---

## Open questions

1. **Dual targeting.** The skill now targets .NET 10 and .NET 11. Unions and `closed` depend on .NET 11
   runtime types (§13a). Does every union/closed example need a .NET 10 fallback, or is each one marked
   ".NET 11+"? Can unions or `closed` be polyfilled on `net10.0`? Not tested.
2. **Union vs. closed hierarchy as the default slice result** (§13e). Microsoft prefers closed
   hierarchies when you own the JSON contract. Unions fit when the cases are unrelated existing types.
   Which one does the skill teach first?
3. **Generic short-circuit behaviors with union results** (§13e). No primary pattern exists. Leave it
   out, or design one, for example a static abstract `IHasValidationCase<TSelf>`?
4. **Slices → infrastructure.** CONTOSO and Wolverine say slices use DbContext directly. ardalis forbids
   it and goes through Domain-defined repositories. The skill has to pick one default or teach both with
   a rule for choosing.
5. **Test level.** HTTP-level (`WebApplicationFactory.CreateClient`) vs. Bogard's below-HTTP "slice
   fixture" vs. unit tests with a repository seam. Which one is the default, and what minimum per slice?
6. **Architecture-test library.** NetArchTest.Rules is stable but has had no release since 2021. The
   eNhanced fork has slice rules. ArchUnitNET is active (0.13.5, 2026-10). NsDepCop works at build time.
   One recommendation or several?
7. **NetArchTest trailing-dot behavior** in `HaveDependencyOnAny` is unverified (§8b). Run a scratch test
   before publishing rule code.
8. **Modular-monolith layout.** No first-party sample of `Modules/<M>/Features/<UseCase>` was found. Is it
   in scope, and on what authority?
9. **Hand-rolled dispatcher.** The plan wants one because of MediatR licensing. No primary source gives a
   reference implementation. Bogard notes MS DI has no decorator support (2022, not re-checked for .NET
   11). A hand-rolled pipeline needs either manual composition or a different container.
10. **Blazor slices.** No primary guidance. Exclude them, or mark them as opinion.
11. **Is the DI-decorator gap still true** in .NET 11's `Microsoft.Extensions.DependencyInjection`? Not
    checked here.

---

## Verification runs (2026-11-13)

Run as .NET file-based apps on SDK 11.0.100 while writing the skill. Each result supersedes the matching
claim above.

| Claim | Result |
| --- | --- |
| §13e union → `Results<...>` exhaustive switch with `null` arm | **Compiles and runs** on `net11.0`: outcomes map to `NotFound`, `ValidationProblem`, `Created<T>`. |
| §13c `closed record` switch without discard | **Compiles**; exhaustive with no warning. |
| §13d union of `IResult` types returned from an endpoint | **Confirmed wrong usage**: returns HTTP 200 with body `{"statusCode":404}`. |
| §13d `[JsonPolymorphic(InferClosedTypePolymorphism = true)]` | **Confirmed**: writes `{"$type":"OrderCancelled",...}`. |
| §6a `AddValidation` on records with parameter attributes | **Confirmed**: 400 for missing `Guid?`, empty list (`MinLength(1)`), and nested `Lines[0].Quantity` range. `MinLength` on `IReadOnlyList<T>` emits trim warning IL2026. |
| §8b NetArchTest trailing dot in `HaveDependencyOnAny("X.Billing.")` | **Breaks the rule**: the violation is not detected (false pass). Without the dot it is detected. |
| §8b NetArchTest prefix matching | **Confirmed**: `ResideInNamespace("...Order")` matches `...Orders...`. An anchored `ResideInNamespaceMatching` regex (`^...GetOrder(\.` or end of string`)`) fixes it. |
| §8b ArchUnitNET `Slices().Matching("MyApp.Features.(*)..")` groups by area | **Wrong**: each distinct namespace under `Features` is its own slice, including `Orders.Shared` (it flagged `Orders.CreateOrder -> Orders.Shared`). It enforces the strict level only. Pairwise `ResideInNamespaceOrBelow` rules give area-level sharing and do not prefix-match `GetOrder` / `GetOrderById`. |
| ArchUnitNET `Domain` rule (§8b) | **Passes** as written. |
| C# 14 / `net10.0` abstract record with private constructor and nested cases | **Compiles and runs**; needs a discard arm. |
| System.CommandLine 2.0.0 `SetAction(async (ParseResult, CancellationToken) => int)` | **Compiles and runs**; returns the exit code. |
| Hand-rolled mediator with ordered pipeline steps | **Runs**; the step registered first is outermost. |
| File-based apps | Default to `PublishAot=true`, which disables reflection-based JSON; `#:property PublishAot=false` restores it. |
| NHibernate current release | 5.7.1 on nuget.org. |

---

## Sources

Bogard / first-party VSA:

- <https://www.jimmybogard.com/vertical-slice-architecture/> (2018-04-19)
- <https://www.jimmybogard.com/migrating-contoso-university-example-to-razor-pages/> (2018-02-21)
- <https://lostechies.com/jimmybogard/2016/12/12/dealing-with-duplication-in-mediatr-handlers>
- <https://jimmybogard.com/you-probably-dont-need-to-worry-about-mediatr/> (2022-05-05)
- <https://www.jimmybogard.com/tales-from-the-net-migration-trenches-migrating-business-logic/> (2023-09-27)
- <https://www.jimmybogard.com/vertical-slice-architecture-webinar/> (2026-07-23)
- <https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/> (2025-04-16)
- <https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/> (2025-07-02)
- <https://www.jimmybogard.com/automapper-and-mediatr-roadmaps/> (2025-07-08)
- <https://www.jimmybogard.com/automapper-17-0-and-mediatr-15-0-released/> (2026-11-12)
- <https://mediatr.io/>
- <https://github.com/jbogard/ContosoUniversityDotNetCore-Pages> (HEAD abb4c08)
- <https://github.com/LuckyPennySoftware/MediatR>
- <https://github.com/jbogard/Respawn>

Microsoft Learn:

- <https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures>
- <https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps>
- <https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles>
- <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api>
- <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design>
- <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core>
- <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation>
- <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns>
- <https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/eshoponcontainers-cqrs-ddd-microservice>
- <https://learn.microsoft.com/azure/architecture/patterns/cqrs>
- <https://learn.microsoft.com/azure/architecture/patterns/strangler-fig>
- <https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext?view=efcore-10.0>
- <https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbset-1?view=efcore-10.0>
- <https://learn.microsoft.com/ef/core/dbcontext-configuration/>
- <https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy>
- <https://learn.microsoft.com/ef/core/providers/in-memory/>
- <https://learn.microsoft.com/ef/core/querying/filters>
- <https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew>
- <https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-groups>
- <https://learn.microsoft.com/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0>
- <https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions>
- <https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0>
- <https://learn.microsoft.com/dotnet/core/extensions/options>
- <https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator>
- <https://learn.microsoft.com/aspnet/core/fundamentals/native-aot?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/rdg?view=aspnetcore-10.0>
- <https://learn.microsoft.com/aspnet/core/performance/caching/output?view=aspnetcore-10.0>
- <https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs>
- <https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/microservices-architecture>
- <https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json>
- <https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/union>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-15.0/unions>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/closed>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-15.0/closed-hierarchies>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns#closed-hierarchy-patterns>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/union-declaration-errors>
- <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview>
- <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/libraries>
- <https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/union-types>
- <https://github.com/dotnet/csharplang/issues/9662>, <https://github.com/dotnet/csharplang/issues/9499>
- <https://github.com/dotnet/aspnetcore/pull/67001>, <https://github.com/dotnet/aspnetcore/pull/68846>

Library and template repos / docs:

- <https://github.com/BenMorris/NetArchTest> (NuGet `NetArchTest.Rules` 1.3.2)
- <https://github.com/NeVeSpl/NetArchTest.eNhancedEdition> (NuGet 1.4.5)
- <https://github.com/TNG/ArchUnitNET> (NuGet `TngTech.ArchUnitNET` 0.13.5)
- <https://github.com/realvizu/NsDepCop>
- <https://github.com/martinothamar/Mediator> (NuGet `Mediator.SourceGenerator` 3.0.2)
- <https://wolverinefx.net/tutorials/vertical-slice-architecture.html>
- <https://fast-endpoints.com/>, <https://fast-endpoints.com/docs/native-aot>
- <https://deviq.com/design-patterns/repr-design-pattern>
- <https://github.com/ardalis/CleanArchitecture>
- <https://github.com/jasontaylordev/CleanArchitecture>
- <https://docs.fluentvalidation.net/en/latest/aspnet.html>
- <https://dotnet.testcontainers.org/>
- <https://github.com/dotnet/aspnet-api-versioning>

Vocabulary owners:

- <https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html>
- <https://simonbrown.je/modular-monolith>
- <https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf>
- <https://sandimetz.com/blog/2016/1/20/the-wrong-abstraction>

Community practice (secondary, labeled where used):

- <https://www.milanjovanovic.tech/blog/vertical-slice-architecture> (2023-11-04)
- <https://codeopinion.com/restructuring-to-a-vertical-slice-architecture/> (2021-09-01)
