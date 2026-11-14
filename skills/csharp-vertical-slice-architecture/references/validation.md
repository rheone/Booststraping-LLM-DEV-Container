# Validation

## Two kinds of rules

| Kind | Examples | Where it runs | Failure |
| --- | --- | --- | --- |
| **Input shape** | Required, length, range, format, collection not empty | Before the handler, in the edge's validation step | 400 `ValidationProblem` |
| **Business rule / invariant** | Customer exists, stock available, order not shipped, total never negative | In the handler (rules that need data) or the domain (invariants of one entity) | A result outcome: 404, 409, 422 |

Input rules belong to the slice: the request's attributes or the slice's validator type. The mechanism
that runs them (filter, pipeline step) is shared infrastructure. Invariants belong on the entity:
"All the domain logic should be contained in the domain classes"
(<https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api>).

## Choose the mechanism

Follow the repo. If it already uses a validator library, keep it (standing rule 2). Otherwise, present the
options and ask.

| Option | Edge types | Notes |
| --- | --- | --- |
| **.NET 10+ built-in Minimal API validation** (`AddValidation`) | Minimal APIs | DataAnnotations on records; runs as an endpoint filter; source generated |
| **DataAnnotations in MVC / Razor Pages** | Controllers, pages | The model-validation pipeline; `[ApiController]` returns 400 automatically |
| **Validator library run by an endpoint filter** (for example FluentValidation) | Minimal APIs | Rules as code; async rules; reusable rule sets |
| **Validator library run by a mediator pipeline step** | Every edge through the mediator | One mechanism for HTTP, messages, jobs, CLI |
| **`IValidatableObject` / custom attributes** | All of the above that use DataAnnotations | Cross-field rules on the request record |
| **Options validation** (`ValidateDataAnnotations().ValidateOnStart()`, `[OptionsValidator]` source generator) | Configuration | For configuration objects, not requests; the generator is reflection-free |

## Built-in Minimal API validation (.NET 10 and 11)

```csharp
builder.Services.AddValidation();

public sealed record CreateOrderRequest(
    [Required] Guid? CustomerId,
    [MinLength(1)] IReadOnlyList<CreateOrderLine> Lines);

public sealed record CreateOrderLine(
    [Required, StringLength(32)] string Sku,
    [Range(1, 1000)] int Quantity);
```

Facts and traps (<https://learn.microsoft.com/aspnet/core/fundamentals/validation>,
<https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0>):

- An endpoint filter validates query, header, and body parameters before the handler. Failures return 400
  with problem details through `IProblemDetailsService`. Opt an endpoint out with `.DisableValidation()`.
- The source generator discovers validatable types **only in the assembly that calls `AddValidation`**.
  When request records live in another project (Layouts D, E, F), validation is skipped with **no error
  and no log**. Call `AddValidation` from that assembly, or keep requests in the host assembly. Add an
  integration test per slice that posts an invalid request and asserts 400.
- It does not reject unknown JSON properties unless you set
  `JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow`.
- It is not supported for MVC or Razor Pages.
- The APIs live in `Microsoft.Extensions.Validation`. `[ValidatableType]` and the resolver APIs are
  experimental in .NET 10.
- .NET 11 adds async validation (`AsyncValidationAttribute`, `IAsyncValidatableObject`), localized
  messages, and validation of union cases
  (<https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11>).

Attribute targets on positional records: put attributes on the constructor **parameters** (no
`property:` target), as Microsoft's examples do. MVC binding through a record constructor also reads
parameter attributes and ignores property attributes. `[Required]` on a non-nullable `Guid` never fails
(the default `Guid.Empty` is a value); use `Guid?` with `[Required]`, or a custom "not empty" rule.

## Validator library in an endpoint filter

```csharp
internal sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        TRequest? request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        ValidationResult result = await validator
            .ValidateAsync(request, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return result.IsValid
            ? await next(context).ConfigureAwait(false)
            : TypedResults.ValidationProblem(result.ToDictionary());
    }
}

// In the slice's Map method:
orders.MapPost("/", HandleAsync).AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

The filter is generic shared infrastructure; the validator class sits in the slice folder. FluentValidation's
own docs no longer recommend its ASP.NET auto-validation package and say to use a filter for Minimal APIs
(<https://docs.fluentvalidation.net/en/latest/aspnet.html>).

## Validation in a mediator pipeline step

When a mediator step validates, it must return a failure without throwing for an expected outcome. Give
the result type a validation case and a static factory; see "Results in a pipeline" in
[results-and-errors.md](results-and-errors.md). A step that throws a `ValidationException` for invalid
input uses exceptions for control flow; keep it only when the repo already does this.

## Invariants in the domain

```csharp
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public static Order Place(Guid customerId, IEnumerable<(Product Product, int Quantity)> lines, DateTimeOffset now)
    {
        Order order = new() { Id = Guid.CreateVersion7(now), CustomerId = customerId, PlacedAt = now };
        foreach ((Product product, int quantity) in lines)
        {
            order.AddLine(product, quantity);
        }

        return order;
    }

    public void AddLine(Product product, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        _lines.Add(new OrderLine(product.Sku, quantity, product.Price));
    }

    // Id, CustomerId, PlacedAt, Lines, Status...
}
```

The guard throws because input validation has already rejected a zero quantity; reaching the guard with
one is a bug. If the domain must report a rule failure the caller can act on, return an outcome instead
(for example `AddLineResult`).

## Checklist

- Every request with input rules has them declared in its slice.
- Every slice has an integration test that sends one invalid request and asserts 400 with the expected
  field key.
- No handler re-checks shape rules the validation step already enforces.
- Business rules that need data return outcomes, not exceptions.
