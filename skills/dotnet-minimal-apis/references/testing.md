# Testing

Minimal API handlers are plain delegates, and the routing/filter pipeline around them is ordinary
ASP.NET Core middleware — testing splits cleanly between calling a handler's logic directly and
exercising the full HTTP pipeline through the running app.

## Unit testing a handler extracted to a static method

A handler written as a static method (see [route-registration.md](route-registration.md)) is callable
directly in a test, with no HTTP involved:

```csharp
[Fact]
public void GetById_OrderExists_ReturnsOk()
{
    var repository = Substitute.For<IOrderRepository>();
    repository.Find(Arg.Any<Guid>()).Returns(new Order(Guid.NewGuid(), "customer-1", 42.00m));

    var result = OrderEndpoints.GetById(Guid.NewGuid(), repository);

    result.Should().BeOfType<Ok<Order>>();
}
```

Declaring the handler's return type as a concrete `TypedResults` type (or a `Results<...>` union, see
[typed-results.md](typed-results.md)) is what makes an assertion like `.Should().BeOfType<Ok<Order>>()`
possible — a handler returning the loosely-typed `IResult` interface can still be tested, but the
assertion has to unwrap or pattern-match the result instead of checking a concrete type directly.

## Testing an endpoint filter in isolation

Build an `EndpointFilterInvocationContext` and a stand-in `next` delegate, the same shape as testing
a MediatR-style pipeline behavior:

```csharp
[Fact]
public async Task ValidationFilter_InvalidRequest_ShortCircuitsWithProblem()
{
    var validator = Substitute.For<IValidator<CreateOrderRequest>>();
    validator.ValidateAsync(Arg.Any<CreateOrderRequest>())
        .Returns(new ValidationResult([new ValidationFailure("CustomerId", "required")]));

    var filter = new ValidationFilter<CreateOrderRequest>(validator);
    var httpContext = new DefaultHttpContext();
    var invocationContext = new DefaultEndpointFilterInvocationContext(httpContext, new CreateOrderRequest());
    var nextCalled = false;
    EndpointFilterDelegate next = _ => { nextCalled = true; return ValueTask.FromResult<object?>(Results.Ok()); };

    var result = await filter.InvokeAsync(invocationContext, next);

    nextCalled.Should().BeFalse();
    result.Should().BeOfType<ProblemHttpResult>();
}
```

## Integration testing the full pipeline with WebApplicationFactory

Reserve this for verifying route matching, model binding, filter registration order, and
authorization together — things that only exist once the app is actually wired up and running:

```csharp
public sealed class OrdersApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task PostOrders_InvalidRequest_ReturnsValidationProblem()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrderRequest { CustomerId = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("CustomerId");
    }

    [Fact]
    public async Task GetOrderById_UnknownId_Returns404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

Swap real infrastructure dependencies (a database, an external API client) for fakes via
`WebApplicationFactory.WithWebHostBuilder(...).ConfigureServices(...)`, the same as with any ASP.NET
Core integration test — nothing about minimal API registration changes how service replacement works
for testing.

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| A handler's business logic given specific inputs | Call the extracted static method directly |
| A single filter's short-circuit/pass-through logic | Construct the filter and a stand-in `next` delegate directly |
| Route matching, binding, filter pipeline ordering, authorization together | `WebApplicationFactory` driving real HTTP requests |

Most of the test suite should sit in the first row for the same reason it does with any
delegate-based design: a handler extracted to a plain static method has no framework dependency of
its own to fight through in a test.
