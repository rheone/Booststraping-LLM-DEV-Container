# Endpoint Filters

`IEndpointFilter` wraps a minimal API handler's invocation with cross-cutting logic — validation,
logging, short-circuiting a response — without changing the handler's own signature or body.

## Implementing a filter

```csharp
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.GetArgument<T>(0);
        var result = await validator.ValidateAsync(argument);

        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
```

- **`context.GetArgument<T>(index)`** — reads a specific handler parameter by its positional index in
  the handler's argument list, typed.
- **`next(context)`** — invokes the next filter in the pipeline, or the handler itself if this is the
  last filter; **not calling `next`** short-circuits the request, returning this filter's own result
  instead of ever running the handler.

## Registering a filter on one endpoint

```csharp
app.MapPost("/orders", (CreateOrderRequest request, IOrderService orders) => orders.CreateAsync(request))
   .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

Filters registered via `.AddEndpointFilter<T>()` are resolved from DI per invocation, so a filter can
take constructor dependencies exactly like any other DI-registered service.

## Inline filters

For simple, one-off cross-cutting logic that doesn't warrant its own class, `.AddEndpointFilter(...)`
also accepts a delegate directly:

```csharp
app.MapGet("/orders/{id:guid}", (Guid id) => GetOrder(id))
   .AddEndpointFilter(async (context, next) =>
   {
       var id = context.GetArgument<Guid>(0);
       if (id == Guid.Empty)
       {
           return Results.BadRequest("id must not be empty.");
       }

       return await next(context);
   });
```

## Filter ordering

Filters run in the order they're registered, wrapping outward-in around the handler — the first
registered filter is the outermost, and its code before `next(context)` runs first, while its code
after `next(context)` runs last:

```csharp
app.MapPost("/orders", Handler)
   .AddEndpointFilter<LoggingFilter>()   // runs first (outermost)
   .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>(); // runs second, then the handler
```

## Applying a filter to every endpoint in a group

Registering a filter on a route group (see [route-groups.md](route-groups.md)) applies it to every
endpoint mapped within that group, without repeating the `.AddEndpointFilter<T>()` call per endpoint:

```csharp
var orders = app.MapGroup("/orders").AddEndpointFilter<AuditLoggingFilter>();

orders.MapGet("/{id:guid}", (Guid id) => GetOrder(id));
orders.MapPost("/", (CreateOrderRequest request) => CreateOrder(request));
```

## Modifying the result after the handler runs

A filter can inspect and replace what the handler returned, not just short-circuit before it runs:

```csharp
public sealed class ETagFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result is IValueHttpResult { Value: IETaggable taggable })
        {
            context.HttpContext.Response.Headers.ETag = taggable.ETag;
        }

        return result;
    }
}
```
