# Action Filters

Action filters run cross-cutting logic immediately before and after an action method executes —
logging, authorization checks beyond what `[Authorize]` covers, response shaping — without modifying
the action's own body.

## IActionFilter and IAsyncActionFilter

```csharp
public sealed class LoggingActionFilter(ILogger<LoggingActionFilter> logger) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        logger.LogInformation("Executing {Action}", context.ActionDescriptor.DisplayName);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        logger.LogInformation("Executed {Action}, result: {ResultType}",
            context.ActionDescriptor.DisplayName, context.Result?.GetType().Name);
    }
}
```

Prefer `IAsyncActionFilter` (a single `OnActionExecutionAsync` method) over the synchronous
`IActionFilter` pair when the filter's own logic needs to `await` anything — implementing both on the
same class is redundant and only one will actually run (the async version, when present):

```csharp
public sealed class TimingActionFilter(ILogger<TimingActionFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var executedContext = await next(); // runs the action (and any inner filters)
        logger.LogInformation("{Action} took {ElapsedMs}ms",
            context.ActionDescriptor.DisplayName, stopwatch.ElapsedMilliseconds);
    }
}
```

Not calling `next()` inside `OnActionExecutionAsync` short-circuits the pipeline — the action method
never runs, and `context.Result` (set explicitly before returning) becomes the response.

## Registering a filter

```csharp
[ServiceFilter(typeof(LoggingActionFilter))]
public sealed class OrdersController : ControllerBase { }

builder.Services.AddScoped<LoggingActionFilter>();
```

or globally, for every controller in the app:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<TimingActionFilter>();
});
```

`[ServiceFilter(typeof(T))]` resolves the filter instance from DI (letting it take constructor
dependencies); `[TypeFilter(typeof(T))]` also resolves from DI but additionally allows passing
constructor arguments explicitly via the attribute. A filter registered globally via
`options.Filters.Add<T>()` applies to every action across every controller without any per-class
attribute.

## The filter pipeline and its ordering

ASP.NET Core's MVC filter pipeline runs in a fixed stage order, each stage wrapping the next:

1. **Authorization filters** (`IAuthorizationFilter`) — run first; `[Authorize]` itself is
   implemented as an authorization filter.
2. **Resource filters** (`IResourceFilter`) — run after authorization, before model binding; can
   short-circuit before binding/validation even happens (e.g. for response caching).
3. **Action filters** (`IActionFilter`/`IAsyncActionFilter`) — run after model binding and
   validation, immediately around the action method itself.
4. **Exception filters** (`IExceptionFilter`) — run only if an unhandled exception propagates out of
   the action or a later stage.
5. **Result filters** (`IResultFilter`/`IAsyncResultFilter`) — run immediately around executing the
   action's `IActionResult` (writing the response).

Within the action-filter stage specifically, when multiple action filters apply to one action,
**global filters run first (outermost), then class-level filters, then method-level filters** — and
each one's "before" code runs in that order while its "after" code runs in the reverse order,
mirroring how the earlier stages wrap the later ones.

## Filters vs. middleware

A filter operates within MVC's action-invocation pipeline and has access to MVC-specific context
(`ActionDescriptor`, model binding results, the resolved `IActionResult`) that ordinary ASP.NET Core
middleware, running earlier in the request pipeline, does not have. Reach for a filter when the
cross-cutting concern needs that MVC-specific context; reach for middleware when it doesn't (request
logging that doesn't care which action ultimately handles the request, for example).
