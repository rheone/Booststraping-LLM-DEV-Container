# Results and errors

## Choose the style

Follow the repo. Look for: a `Result<T>` type or package, `union` declarations, closed record hierarchies,
`OneOf`-style types, custom exception types caught by an `IExceptionHandler` or middleware, or a mediator
pipeline step that throws on validation. If the repo has no convention, present both styles below and ask.

| Style | Expected outcomes are... | Strengths | Costs |
| --- | --- | --- | --- |
| **Result values** | Returned: a `union`, a closed hierarchy, or a `Result<T>` type | Outcomes are visible in the signature; the compiler checks the edge's switch; no hidden control flow; cheap | Every layer must pass results through |
| **Exceptions mapped to ProblemDetails** | Thrown as typed exceptions and mapped by a global handler | Short handler code; existing in many codebases (Microsoft's eShop sample) | Outcomes hidden from signatures; exceptions cost time on hot paths |

Whichever style the repo uses, exceptions mean **exceptional** conditions in every example you write: a
broken invariant, an unreachable branch, a failed dependency. Microsoft: "exceptions shouldn't be used to
control normal program flow" (<https://learn.microsoft.com/aspnet/core/fundamentals/best-practices#minimize-exceptions>).
When the repo already throws for expected outcomes, follow it in that repo and say so in your answer;
do not extend the pattern into new shared code.

## Language version gate

| Project language version | Outcome type |
| --- | --- |
| C# 15+ (default for `net11.0`) | `union` for unrelated case types; `closed` record hierarchy for cases that share a meaning |
| C# 14 and earlier (`net10.0` and below) | Abstract record with a private constructor and nested sealed cases |

Check `<LangVersion>` and `<TargetFramework>` in the project file and `Directory.Build.props`. Unions need
the `UnionAttribute` and `IUnion` types that the .NET 11 runtime ships; treat unions and `closed` as
.NET 11-only. (<https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/union>)

## C# 15 `union`

```csharp
public sealed record OrderCreated(Guid OrderId, string OrderNumber);
public sealed record CustomerMissing(Guid CustomerId);
public sealed record ProductsUnavailable(IReadOnlyList<string> Skus);

public union CreateOrderResult(OrderCreated, CustomerMissing, ProductsUnavailable);
```

Facts that shape usage (<https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/union>):

- A `union` compiles to a struct with one constructor per case and an `object? Value`. It boxes value-type
  cases.
- Each case type converts implicitly: `return new CustomerMissing(id);` in a method that returns
  `CreateOrderResult`.
- Patterns test the case value: `result switch { OrderCreated c => ..., ... }`. A switch that covers every
  case is exhaustive.
- `default(CreateOrderResult)` has a null value. Add a `null` arm; the compiler may not warn when it is
  missing, and the switch then throws at runtime. Prefer `null` over `_` so adding a case later still
  warns.
- A `union` is a value type. "No result" is `CreateOrderResult?` (`Nullable<T>`), and a conditional
  `cond ? null : await ...` does not compile without a cast; prefer adding a case
  (`OrderMissing`) over a nullable union.
- System.Text.Json writes the active case with no discriminator. ASP.NET Core 11 accepts unions in JSON
  bodies and describes them in OpenAPI as `anyOf`. Unions do not bind from route, query, header, or form.

Use a `union` when the cases are existing, unrelated types (`union LookupResult(Customer, CustomerMissing)`).

## C# 15 `closed` hierarchy

```csharp
public closed record CancelOrderResult;
public sealed record OrderCancelled(Guid OrderId) : CancelOrderResult;
public sealed record OrderNotFound(Guid OrderId) : CancelOrderResult;
public sealed record OrderAlreadyShipped(Guid OrderId, DateTimeOffset ShippedAt) : CancelOrderResult;
```

Facts (<https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/closed>):

- A `closed` class is implicitly abstract. Direct subtypes must be in the same assembly.
- A switch over the direct subtypes is exhaustive without a discard. Exhaustiveness covers direct
  descendants only; seal the cases.
- Keep every case as accessible as the base. An `internal` case under a `public` base makes switches in
  other assemblies non-exhaustive (CS8509).
- `[JsonPolymorphic(InferClosedTypePolymorphism = true)]` adds a `$type` discriminator if the outcome is
  ever serialized. Microsoft recommends closed hierarchies over unions when you own the types and the JSON
  contract.

Use a closed hierarchy when the cases are all "an outcome of this use case".

## C# 14 and earlier: abstract record with a private constructor

```csharp
public abstract record CancelOrderResult
{
    private CancelOrderResult() { }

    public sealed record Cancelled(Guid OrderId) : CancelOrderResult;
    public sealed record NotFound(Guid OrderId) : CancelOrderResult;
    public sealed record AlreadyShipped(Guid OrderId, DateTimeOffset ShippedAt) : CancelOrderResult;
}
```

The private constructor stops other types from deriving, but the compiler does not know the set is
closed. The edge's switch needs a discard arm, which throws because reaching it is a bug:

```csharp
return result switch
{
    CancelOrderResult.Cancelled => TypedResults.NoContent(),
    CancelOrderResult.NotFound => TypedResults.NotFound(),
    CancelOrderResult.AlreadyShipped shipped => TypedResults.Conflict(
        new { shipped.OrderId, shipped.ShippedAt }),
    _ => throw new UnreachableException($"Unhandled result {result.GetType().Name}."),
};
```

Nested case names (`CancelOrderResult.NotFound`) avoid the collision with `HttpResults.NotFound` because
you qualify them.

## Mapping outcomes at the edge

The handler returns the domain outcome. The edge maps it to the transport with one exhaustive switch.
For Minimal APIs, return `Results<T1, T2, ...>` built from `TypedResults`; it gives a compile-time check of
the allowed results and OpenAPI metadata
(<https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses>).

| Outcome | HTTP |
| --- | --- |
| Created | `TypedResults.Created(location, body)` 201 |
| Succeeded, no body | `TypedResults.NoContent()` 204 |
| Input shape invalid | `TypedResults.ValidationProblem(errors)` 400 |
| Entity not found | `TypedResults.NotFound()` 404 |
| Not allowed for this user | `TypedResults.Forbid()` 403 |
| State conflict (already shipped, version mismatch) | `TypedResults.Conflict(...)` or `TypedResults.Problem(statusCode: 409)` 409 |
| Business rule rejected | `TypedResults.Problem(title, detail, statusCode: 422)` 422, or 400 per repo convention |

Do not return a union of `IResult` types from an endpoint. A union struct does not implement `IResult`,
and ASP.NET Core serializes it as a JSON body with status 200.

For non-HTTP edges, map the same outcome to that transport: acknowledge or dead-letter a message, set a CLI
exit code, return a gRPC status.

## Exceptions and ProblemDetails

Wire these in every HTTP app, whichever style the repo uses for expected outcomes:

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

app.UseExceptionHandler();
app.UseStatusCodePages();
```

Facts (<https://learn.microsoft.com/aspnet/core/fundamentals/error-handling>):

- `IExceptionHandler` implementations are singletons and run in registration order until one returns
  `true`. They run only when `UseExceptionHandler()` is in the pipeline.
- A handler that returns `true` must set the status code.
- From .NET 10, handled exceptions no longer emit diagnostics by default. Log inside your handler, or
  configure `SuppressDiagnosticsCallback`.
- In Production, Minimal API binding failures write 400 directly and do not reach exception handlers
  (`ThrowOnBadRequest` is `true` only in Development).

## Results in a pipeline

A validation pipeline step that short-circuits must produce the handler's result type. With results:

- **Endpoint filter**: the filter returns `TypedResults.ValidationProblem(...)` as an `IResult` before the
  handler runs. It never needs the slice's result type. This is the simplest option for HTTP.
- **Mediator step**: give the outcome type a known validation case and a static factory. For example, an
  interface `IHasValidationFailure<TSelf> { static abstract TSelf ValidationFailed(IDictionary<string,
  string[]> errors); }` that each result type implements. No primary source shows this pattern; treat it as
  a design you own.
