# Testing code that depends on Autofac

## Default: test the class, not the container

For the large majority of classes, the fact that Autofac will eventually construct them in
production is irrelevant to unit testing them. If a class declares its dependencies as constructor
parameters (the idiomatic Autofac style), test it the same way you'd test any constructor-injected
class: construct it directly with hand-built fakes/mocks/stubs, call methods, assert behavior. No
container involved.

```csharp
[Fact]
public void Place_ValidatesOrderBeforeSaving()
{
    var repository = new FakeOrderRepository();
    var validator = new FakeOrderValidator(isValid: false);
    var sut = new OrderService(repository, validator);

    Assert.Throws<InvalidOrderException>(() => sut.Place(new Order()));
    Assert.Empty(repository.SavedOrders);
}
```

This is faster, more isolated, and fails with a clearer stack trace than resolving `OrderService`
through a real or test container — reserve container-involving tests for the things that are
actually about the container: registration correctness and module composition.

## What's actually worth testing "through" Autofac

Two things a plain constructor test cannot verify:

1. **That the production registration graph is actually consistent** — every type the app expects
   to resolve really is registered, with no missing dependency, wrong lifetime, or forgotten
   `As<T>()` that would only surface as a runtime crash the first time that code path executes in
   production.
2. **That a module registers what it claims to register** — a module is itself a unit worth testing
   in isolation, independent of the rest of the app's composition root.

## Verifying registrations resolve: a minimal test container

Build a `ContainerBuilder`, register the module(s)/registrations under test (and *only* those,
plus whatever minimal stand-ins their dependencies need), `Build()`, and assert that the services
you expect to be resolvable actually resolve without throwing:

```csharp
[Fact]
public void OrderingModule_RegistersOrderService()
{
    var builder = new ContainerBuilder();
    builder.RegisterModule<OrderingModule>();
    // Provide minimal stand-ins for anything OrderingModule expects from elsewhere in the app:
    builder.RegisterInstance(new FakeAppConfig()).As<IAppConfig>();

    using IContainer container = builder.Build();

    var orderService = container.Resolve<IOrderService>();
    Assert.NotNull(orderService);
}
```

Prefer asserting `Resolve<T>()` succeeds (throwing is the failure signal) over deeply asserting on
the resolved object's internals — this test's job is "does the graph wire up," not "does the
business logic work," which the plain constructor tests already cover.

## Whole-container structural validation

For a larger app, a single test that builds the *real* composition-root registrations (reusing
whatever method/module list production startup uses) and resolves every publicly-entry-point type
(every controller, every message handler, every hosted service) catches missing-registration bugs
in one pass, without needing a test per service:

```csharp
[Fact]
public void AllControllers_ResolveFromContainer()
{
    var builder = new ContainerBuilder();
    Startup.ConfigureAutofacContainer(builder); // the same method Program.cs calls

    using IContainer container = builder.Build();

    foreach (var controllerType in typeof(Startup).Assembly.GetTypes()
        .Where(t => t.Name.EndsWith("Controller")))
    {
        using var scope = container.BeginLifetimeScope();
        var instance = scope.Resolve(controllerType); // throws on missing registration
        Assert.NotNull(instance);
    }
}
```

This kind of test trades some runtime cost (it builds a full real container) for catching an entire
class of "forgot to register X" bugs at test time instead of in production. Some dependencies
(external HTTP clients, real database connections) may still need to be swapped for fakes in the
composition method itself, or the composition method needs a seam (e.g. an `IWebHostEnvironment`
check, or a parameter) to substitute test doubles for genuinely external resources.

## Testing a module in isolation

Treat a module's `Load` method as its own unit of behavior: build a container with *only* that
module (plus the smallest possible set of stand-in dependencies it needs to satisfy resolution),
and assert on what it claims to provide — service types, lifetimes, and any decorators it applies.

```csharp
[Fact]
public void OrderingModule_RegistersOrderServiceAsInstancePerLifetimeScope()
{
    var builder = new ContainerBuilder();
    builder.RegisterModule<OrderingModule>();
    builder.RegisterInstance(new FakeAppConfig()).As<IAppConfig>();
    using IContainer container = builder.Build();

    using var scope1 = container.BeginLifetimeScope();
    using var scope2 = container.BeginLifetimeScope();

    var fromScope1 = scope1.Resolve<IOrderService>();
    var againFromScope1 = scope1.Resolve<IOrderService>();
    var fromScope2 = scope2.Resolve<IOrderService>();

    Assert.Same(fromScope1, againFromScope1);   // same scope -> same instance
    Assert.NotSame(fromScope1, fromScope2);      // different scope -> different instance
}
```

## What to avoid

- Don't resolve the system-under-test through a container in an ordinary unit test just because
  it's *possible* to — it adds container-build time, obscures which specific dependency a failing
  test is actually about, and couples the test to registration details (lifetimes, module
  structure) that have nothing to do with the behavior being verified.
- Don't build a full production container per test method if a narrower one (just the module or
  registrations under test) answers the question — a slow test suite from container construction
  is avoidable overhead, not an inherent cost of testing DI wiring.
- Don't assert on lifetime/scope behavior (`Assert.Same`/`Assert.NotSame` across scopes) for every
  registration in the app — reserve it for lifetimes that are easy to get subtly wrong (see
  `references/common-pitfalls.md`), not as a blanket pattern applied everywhere.
