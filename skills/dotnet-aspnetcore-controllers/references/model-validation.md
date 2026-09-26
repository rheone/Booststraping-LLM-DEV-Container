# Model Validation and Automatic 400 Behavior

Controller-based model validation runs `System.ComponentModel.DataAnnotations` attributes on a bound
model automatically, recording results in `ModelState` — and, under `[ApiController]`, short-
circuits an invalid request with a 400 response before the action method body ever runs.

## Declaring validation on a request model

```csharp
public sealed class CreateOrderRequest
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Total { get; set; }

    [EmailAddress]
    public string? NotificationEmail { get; set; }
}
```

## Automatic 400 under [ApiController]

With `[ApiController]` applied to the controller, an action whose bound model fails validation never
executes its body — the framework returns a 400 with a structured
`application/problem+json` body automatically:

```csharp
[HttpPost]
public IActionResult Create(CreateOrderRequest request)
{
    // This line only runs if `request` already passed validation —
    // no need to check ModelState.IsValid manually here.
    return Ok(CreateOrder(request));
}
```

Without `[ApiController]` (a controller deriving from `Controller` for MVC pages, or one that opted
out), this automatic short-circuit doesn't happen — you must check `ModelState.IsValid` explicitly
inside the action and return `BadRequest(ModelState)` yourself:

```csharp
[HttpPost]
public IActionResult Create(CreateOrderRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    return Ok(CreateOrder(request));
}
```

## Customizing the automatic 400 response

`ApiBehaviorOptions.InvalidModelStateResponseFactory` overrides what the automatic 400 actually
returns — useful for a project-wide custom problem-details shape:

```csharp
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path,
        };

        return new BadRequestObjectResult(problemDetails);
    };
});
```

## Adding validation errors manually

`ModelState.AddModelError(key, message)` adds an error outside of data-annotation validation — for a
cross-field check or a check requiring a database lookup that attributes alone can't express:

```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateOrderRequest request)
{
    if (!await customerRepository.ExistsAsync(request.CustomerId))
    {
        ModelState.AddModelError(nameof(request.CustomerId), "Customer does not exist.");
        return ValidationProblem(ModelState);
    }

    return Ok(await CreateOrderAsync(request));
}
```

Because `[ApiController]`'s automatic short-circuit only inspects `ModelState` populated by binding
and data-annotation validation *before* the action runs, an error added manually inside the action
body (as above) needs its own explicit `return ValidationProblem(ModelState)` — the automatic
behavior has already passed by the time the action method is executing.

## IValidatableObject for cross-property validation on the model itself

For validation rules that depend on more than one property of the same model, implement
`IValidatableObject` directly on the request type instead of a custom attribute — the framework
invokes it automatically as part of the same validation pass that runs data-annotation attributes:

```csharp
public sealed class DateRangeRequest : IValidatableObject
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
        {
            yield return new ValidationResult("End must be after Start.", [nameof(End)]);
        }
    }
}
```
