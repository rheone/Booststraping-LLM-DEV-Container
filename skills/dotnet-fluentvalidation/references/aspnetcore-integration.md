# ASP.NET Core Integration

FluentValidation's own documentation no longer recommends automatic validation for new projects.
Read this file before wiring FluentValidation into any ASP.NET Core project — the current
guidance is deliberate, manual invocation, not attribute-style auto-validation.

## The FluentValidation.AspNetCore package is deprecated

The `FluentValidation.AspNetCore` package (which hooked FluentValidation into MVC's model-binding
pipeline for automatic validation) is deprecated and unsupported. It remains installable for
legacy code that already depends on it, but current guidance does not recommend adding it to a new
project, for two structural reasons:

- **It only ever worked with MVC controllers and Razor Pages.** It has no effect on minimal API
  endpoints or Blazor, so a codebase using minimal APIs alongside MVC would get inconsistent
  validation behavior depending on which endpoint style handled the request.
- **It cannot run async rules.** Auto-validation invokes validators synchronously as part of model
  binding; a validator containing `MustAsync`/`CustomAsync`/`WhenAsync` throws
  `AsyncValidatorInvokedSynchronouslyException` when driven this way (see
  [async-validation.md](async-validation.md)).

Do not add `FluentValidation.AspNetCore` to a new project. Invoke validators explicitly instead, as
shown below.

## Minimal APIs: validate explicitly, return ValidationProblem

Inject `IValidator<T>` into the endpoint handler and call `ValidateAsync` before touching the
request further:

```csharp
app.MapPost("/orders", async (CreateOrderRequest request, IValidator<CreateOrderRequest> validator, IOrderService orders, CancellationToken ct) =>
{
    ValidationResult validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var orderId = await orders.CreateAsync(request, ct);
    return Results.Created($"/orders/{orderId}", orderId);
});
```

`ValidationResult.ToDictionary()` (available since v11.1) shapes the failures into the
`IDictionary<string, string[]>` that `Results.ValidationProblem` expects, producing a standard
RFC 9457 `ValidationProblemDetails` response (HTTP 400, one entry per invalid property, each with
its list of error messages) with no manual mapping code.

## Centralizing the check with an endpoint filter

To avoid repeating the validate-then-branch block in every handler, extract it into an
`IEndpointFilter` and apply it per-route or per-group:

```csharp
public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        ValidationResult result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        return result.IsValid
            ? await next(context)
            : Results.ValidationProblem(result.ToDictionary());
    }
}

// usage
app.MapPost("/orders", (CreateOrderRequest request, IOrderService orders, CancellationToken ct) =>
        orders.CreateAsync(request, ct))
    .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

This keeps every handler focused on its own logic while still validating consistently, without
reintroducing the synchronous, MVC-only auto-validation behavior the deprecated package had.

## MVC controllers and Razor Pages: manual invocation into ModelState

Without the deprecated auto-validation package, inject the validator into the controller and copy
failures into `ModelState` yourself:

```csharp
public sealed class OrdersController(IValidator<CreateOrderRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        ValidationResult result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        // ...
    }
}
```

`result.AddToModelState(ModelState)` is a FluentValidation extension method that copies each
`ValidationFailure` into ASP.NET Core's `ModelStateDictionary`, so `ValidationProblem(ModelState)`
produces the same `ValidationProblemDetails` shape ASP.NET Core's own attribute-based validation
would have produced.

## Registration

Register validators once at startup, scanning the assembly that contains them:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
```

This registers every `AbstractValidator<T>` found as `IValidator<T>` with a transient lifetime —
sufficient for the vast majority of validators, which are stateless beyond their constructor
dependencies.
