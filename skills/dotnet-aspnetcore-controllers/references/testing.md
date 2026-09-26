# Testing

Controller actions are ordinary methods on a class you can instantiate directly — most of the value
in testing them comes from unit tests that skip the HTTP pipeline entirely; reserve integration tests
for the pipeline concerns (routing, model binding, filters, `[ApiController]`'s automatic behaviors)
that only exist once the app is actually running.

## Unit testing an action method directly

```csharp
[Fact]
public void GetById_OrderExists_ReturnsOkWithOrder()
{
    var repository = Substitute.For<IOrderRepository>();
    var order = new Order(Guid.NewGuid(), "customer-1", 42.00m);
    repository.Find(order.Id).Returns(order);

    var controller = new OrdersController(repository);

    var result = controller.GetById(order.Id);

    result.Should().BeOfType<OkObjectResult>()
        .Which.Value.Should().Be(order);
}

[Fact]
public void GetById_OrderMissing_ReturnsNotFound()
{
    var repository = Substitute.For<IOrderRepository>();
    repository.Find(Arg.Any<Guid>()).Returns((Order?)null);

    var controller = new OrdersController(repository);

    var result = controller.GetById(Guid.NewGuid());

    result.Should().BeOfType<NotFoundResult>();
}
```

Constructing the controller directly with `new` and its dependencies as fakes works because nothing
about calling an action method requires the DI container, routing, or the HTTP pipeline to be
involved — only `HttpContext`/`User`/`ModelState`-touching code inside the action needs additional
setup (see below).

## Setting up ControllerContext for actions that touch HttpContext or ModelState

An action that reads `User`, `HttpContext`, or manually inspects/mutates `ModelState` needs a
`ControllerContext` assigned before the test calls it, since `ControllerBase` normally gets this from
the framework during a real request:

```csharp
[Fact]
public void Create_InvalidCustomer_AddsModelError()
{
    var controller = new OrdersController(repository)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        },
    };

    var result = controller.Create(new CreateOrderRequest { CustomerId = "unknown" });

    result.Should().BeOfType<BadRequestObjectResult>();
}
```

Assigning a fresh `DefaultHttpContext()` is usually enough; set `HttpContext.User` explicitly (a
`ClaimsPrincipal` built by hand) for an action that reads the current user.

## Testing an action filter in isolation

```csharp
[Fact]
public async Task TimingActionFilter_CallsNext()
{
    var filter = new TimingActionFilter(Substitute.For<ILogger<TimingActionFilter>>());
    var actionContext = new ActionContext(
        new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    var executingContext = new ActionExecutingContext(
        actionContext, [], new Dictionary<string, object?>(), controller: null!);
    var nextCalled = false;
    ActionExecutionDelegate next = () =>
    {
        nextCalled = true;
        return Task.FromResult(new ActionExecutedContext(actionContext, [], controller: null!));
    };

    await filter.OnActionExecutionAsync(executingContext, next);

    nextCalled.Should().BeTrue();
}
```

## Integration testing with WebApplicationFactory

Reserve this layer for verifying routing, `[ApiController]`'s automatic 400 behavior, filter pipeline
ordering, and content negotiation together — behavior that genuinely only exists once the app is
running as a whole:

```csharp
public sealed class OrdersApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task PostOrders_MissingCustomerId_ReturnsAutomatic400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest { Total = 10m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("CustomerId");
    }
}
```

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| An action's business logic given specific inputs | Instantiate the controller directly with faked dependencies |
| Logic touching `HttpContext`/`User`/`ModelState` | Instantiate the controller with a `ControllerContext` assigned |
| A single filter's before/after or short-circuit logic | Construct the filter and a stand-in `next`/context directly |
| `[ApiController]`'s automatic 400, attribute routing, filter pipeline ordering, content negotiation | `WebApplicationFactory` driving real HTTP requests |

Most of the test suite should sit in the first two rows — controller actions are ordinary methods,
and testing them doesn't require exercising the framework's request pipeline at all.
