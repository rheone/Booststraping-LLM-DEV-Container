# Relationship types: Lazy&lt;T&gt;, Func&lt;T&gt;, Owned&lt;T&gt;, and circular dependencies

Autofac recognizes several generic wrapper types as special "relationship types" in constructor
parameters — when a constructor asks for one of these instead of the service directly, Autofac
supplies a purpose-built implementation automatically, with no registration required for the
wrapper itself (only for the inner service `T`).

## `Func<T>` — deferred/repeated resolution

Injecting `Func<T>` instead of `T` defers construction until the delegate is actually invoked, and
allows resolving fresh instances on demand from a component that itself lives longer than the
services it needs repeatedly.

```csharp
public class BatchProcessor
{
    private readonly Func<IOrderProcessor> _processorFactory;
    public BatchProcessor(Func<IOrderProcessor> processorFactory) => _processorFactory = processorFactory;

    public void ProcessAll(IEnumerable<Order> orders)
    {
        foreach (var order in orders)
        {
            var processor = _processorFactory(); // new instance per call, per IOrderProcessor's own lifetime
            processor.Process(order);
        }
    }
}
```

Each invocation of the `Func<T>` re-resolves `T` from the scope the `Func<T>` itself was resolved
in — an `InstancePerDependency` service yields a new instance every call; a `SingleInstance` one
returns the same instance every time, same as it would via direct injection.

## `Lazy<T>` — deferred, memoized resolution

`Lazy<T>` defers construction until first access, then caches that one instance for the lifetime of
the `Lazy<T>` wrapper itself — useful for an expensive-to-construct dependency that's only needed
on some code paths.

```csharp
public class ReportGenerator
{
    private readonly Lazy<IExpensiveRenderer> _renderer;
    public ReportGenerator(Lazy<IExpensiveRenderer> renderer) => _renderer = renderer;

    public void GenerateIfNeeded(bool needsRendering)
    {
        if (needsRendering)
        {
            _renderer.Value.Render(); // IExpensiveRenderer constructed here, not at ReportGenerator construction
        }
    }
}
```

## `Owned<T>` — explicit disposal boundary

`Owned<T>` hands the resolving code an instance plus an implicit child lifetime scope that exists
solely to own it — calling `Dispose()` on the `Owned<T>` disposes the inner instance (and anything
else that scope constructed on its behalf) immediately, independent of the outer scope's own
lifetime. This is how to get "dispose this dependency right now, on demand" without tearing down
the whole enclosing scope.

```csharp
public class ImportJob
{
    private readonly Func<Owned<IFileParser>> _parserFactory;
    public ImportJob(Func<Owned<IFileParser>> parserFactory) => _parserFactory = parserFactory;

    public void Import(string path)
    {
        using Owned<IFileParser> owned = _parserFactory();
        owned.Value.Parse(path);
    } // owned.Value (and anything it depends on within its private scope) disposed here
}
```

`Func<Owned<T>>` is the common combination: a factory for "give me a new, independently-disposable
instance each time," as opposed to plain `Func<T>` where disposal stays tied to the ambient scope.

## Circular dependencies

Constructor injection cannot resolve a genuine cycle (`A` needs `B` in its constructor, `B` needs
`A` in its constructor) — Autofac throws `Autofac.Core.Registration.CircularDependencyDetectedException`
rather than infinite-looping. The three ways out, in order of preference:

1. **Redesign**: a true constructor cycle is usually a sign two responsibilities should be split
   or that a third type should mediate between them — this is the fix to reach for first.
2. **Break the cycle with `Lazy<T>` or `Func<T>`**: if `A` only needs `B` after construction (not
   during it), injecting `Lazy<B>`/`Func<B>` into `A` instead of `B` directly defers the resolution
   of `B` past `A`'s own construction, breaking the cycle without redesigning either type.
3. **Property injection with `PropertyWiringOptions.AllowCircularDependencies`**: for cases neither
   of the above fits, `PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)` (see
   `references/property-method-injection.md`) lets Autofac construct both objects with the cyclic
   property left unset initially, then wire the property afterward — the least preferred option,
   since it trades a compiler-enforced constructor contract for a runtime-only guarantee.
