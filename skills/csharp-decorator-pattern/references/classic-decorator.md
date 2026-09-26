# Classic Decorator

The Decorator pattern adds behavior to an object without modifying its class or the classes of
anything that already depends on it. A decorator implements the same interface as the object it
wraps, holds a reference to that wrapped object, and forwards each call to it — adding its own
behavior before, after, or instead of forwarding.

## Shape

```csharp
public interface IReportGenerator
{
    string Generate(ReportData data);
}

public sealed class BasicReportGenerator : IReportGenerator
{
    public string Generate(ReportData data) => data.ToPlainText();
}
```

A decorator wraps an `IReportGenerator` and adds behavior around the call:

```csharp
public sealed class CachingReportGenerator : IReportGenerator
{
    private readonly IReportGenerator _inner;
    private readonly IDictionary<ReportData, string> _cache = new Dictionary<ReportData, string>();

    public CachingReportGenerator(IReportGenerator inner) => _inner = inner;

    public string Generate(ReportData data)
    {
        if (_cache.TryGetValue(data, out var cached))
        {
            return cached;
        }

        var result = _inner.Generate(data);
        _cache[data] = result;
        return result;
    }
}

public sealed class LoggingReportGenerator : IReportGenerator
{
    private readonly IReportGenerator _inner;
    private readonly ILogger _logger;

    public LoggingReportGenerator(IReportGenerator inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public string Generate(ReportData data)
    {
        _logger.LogInformation("Generating report for {DataId}", data.Id);
        var result = _inner.Generate(data);
        _logger.LogInformation("Report generated, {Length} characters", result.Length);
        return result;
    }
}
```

Nothing about `BasicReportGenerator` changes to gain caching or logging, and nothing about code that
already depends on `IReportGenerator` needs to change either — it still calls `Generate` on
whatever `IReportGenerator` it was given, unaware of how many layers of decoration sit behind that
reference.

## Composing decorators

```csharp
IReportGenerator generator = new LoggingReportGenerator(
    new CachingReportGenerator(
        new BasicReportGenerator()),
    logger);

var report = generator.Generate(data);
```

Each layer forwards to the next until the call reaches `BasicReportGenerator`, which is the only
layer that does not wrap anything further. The order of composition matters and is chosen
deliberately: here, logging wraps caching, so a cache hit still gets logged; wrapping the other way
(`CachingReportGenerator(LoggingReportGenerator(...))`) would mean a cache hit skips the inner
logging entirely, since the cache layer never calls through to it.

## Decorator vs. inheritance

A subclass that overrides `Generate` and calls `base.Generate()` achieves something superficially
similar, but statically fixes the added behavior to that one subclass at compile time. A decorator
achieves the same "add behavior around an existing implementation" outcome as a runtime-composable
object — any number of decorators can wrap any implementation of the interface, chosen and ordered
at the point of construction rather than baked into a class hierarchy. Prefer the decorator when the
added behavior (caching, logging, retry, validation) is orthogonal to which concrete implementation
it wraps and might need to apply to more than one of them.
