# Property and method injection

Autofac's default is constructor injection — required dependencies belong on the constructor, made
explicit and required by the type's own signature. Property/method injection exists for cases
where that default doesn't fit (optional dependencies, legacy types with settable properties and
no suitable constructor, or types built by something other than Autofac that still need wiring
after construction).

## `PropertiesAutowired()`

Opts a registration into autowiring its public settable properties: after construction, Autofac
tries to resolve a value for each writable property whose type is registered, and sets it if found
(properties with no matching registration are silently left at their default/initial value).

```csharp
public class ReportGenerator
{
    public ILogger? Logger { get; set; }         // optional collaborator, wired if registered
    public required IReportStore Store { get; }    // still prefer constructor injection for requireds
}

builder.RegisterType<ReportGenerator>().PropertiesAutowired();
```

`PropertiesAutowired` accepts a `PropertyWiringOptions` argument
(`PropertyWiringOptions.AllowCircularDependencies`) to permit wiring properties that would
otherwise create a circular reference through the constructor graph — one of the few places
Autofac can break a cycle that pure constructor injection cannot (see
`references/relationship-types.md` for the more common circular-dependency escape hatches).

Because unresolved properties fail silently rather than throwing, `PropertiesAutowired` is easy to
misuse as a way to avoid declaring real (required) dependencies explicitly — prefer constructor
injection whenever a dependency is actually required, and reserve property injection for
dependencies a type can genuinely function without.

## `InjectProperties` on `IComponentContext`

For code paths where Autofac didn't construct the object at all (e.g. an object created by
`new` outside the container, or handed back by a third-party framework) but still needs its
Autofac-registered dependencies wired onto existing properties:

```csharp
var reportGenerator = new ReportGenerator();
scope.InjectProperties(reportGenerator);
```

This is an imperative escape hatch, not something to reach for in ordinary application code — using
it routinely is itself a sign the type should be constructed by the container in the first place.

## Method injection

Autofac has no first-class "method injection" attribute/API analogous to `PropertiesAutowired`.
The conventional pattern is either:

- An `Initialize(IDependency dep)`-style method called explicitly by a factory/module after
  resolving the object, with the dependency itself resolved from the same scope, or
- `OnActivated`/`OnActivating` registration hooks, which run arbitrary code (including calling
  methods on the newly built instance) as part of the activation pipeline:

```csharp
builder.RegisterType<ReportGenerator>()
    .OnActivated(e => e.Instance.Initialize(e.Context.Resolve<IReportStore>()));
```

`OnActivating` runs before the instance is fully wired (useful for wrapping/replacing the instance
or setting properties before other decorators/interceptors apply); `OnActivated` runs once
activation (including property injection) has completed — pick the one matching whether the
dependency needs to be in place before or after the rest of Autofac's pipeline runs.
