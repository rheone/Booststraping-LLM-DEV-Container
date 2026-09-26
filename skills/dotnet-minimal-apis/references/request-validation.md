# Request Validation

Minimal APIs have no automatic model-state validation step — nothing inspects a bound request body
or parameter for validity unless you explicitly wire something to do so. Validation is a deliberate
choice among a few approaches, not a single built-in mechanism.

## Manual validation inside the handler

The simplest approach for a small number of endpoints: check the bound parameter directly and return
a problem response when it's invalid.

```csharp
app.MapPost("/orders", (CreateOrderRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerId))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.CustomerId)] = ["CustomerId is required."],
        });
    }

    return Results.Ok(CreateOrder(request));
});
```

`Results.ValidationProblem(...)` produces a standard `application/problem+json` response
(RFC 9457) with per-field error arrays — the same shape clients typically expect from any structured
validation failure, regardless of which mechanism produced it.

## Data annotations with a validation helper

Decorate the request type with `System.ComponentModel.DataAnnotations` attributes, then validate
explicitly with `Validator.TryValidateObject` (there is no automatic annotation-driven validation
step for minimal APIs the way there is for controller model binding):

```csharp
public sealed class CreateOrderRequest
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Total { get; set; }
}

app.MapPost("/orders", (CreateOrderRequest request) =>
{
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
    {
        return Results.ValidationProblem(results.ToDictionary(
            r => r.MemberNames.FirstOrDefault() ?? string.Empty,
            r => new[] { r.ErrorMessage ?? "Invalid value." }));
    }

    return Results.Ok(CreateOrder(request));
});
```

Wrapping this pattern in an endpoint filter (see [endpoint-filters.md](endpoint-filters.md)) avoids
repeating the `Validator.TryValidateObject` call in every handler that needs it.

## A validation endpoint filter using a third-party validator

The most common production shape: a validation library (any implementation of a `Validate`/
`ValidateAsync`-style contract for a given request type) invoked from a generic endpoint filter, so
individual handlers stay free of validation code entirely:

```csharp
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.GetArgument<T>(0);
        var result = await validator.ValidateAsync(argument);

        return result.IsValid
            ? await next(context)
            : Results.ValidationProblem(result.ToDictionary());
    }
}

app.MapPost("/orders", (CreateOrderRequest request) => Results.Ok(CreateOrder(request)))
   .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

This is the same filter shown in [endpoint-filters.md](endpoint-filters.md) — validation is one of
the most common reasons to reach for an endpoint filter in the first place, since it's exactly the
kind of cross-cutting, short-circuit-on-failure logic filters exist for.

## Choosing an approach

| Situation | Approach |
| --- | --- |
| One or two endpoints, simple checks | Manual validation inline in the handler |
| A handful of request types, no external validation library in the project | Data annotations plus `Validator.TryValidateObject`, likely wrapped in a shared filter |
| Many request types, or validation rules too complex for attributes (cross-field, DB-dependent checks) | A dedicated validator per request type, invoked from a generic endpoint filter |
