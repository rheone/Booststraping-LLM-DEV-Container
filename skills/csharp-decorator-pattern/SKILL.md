---
name: csharp-decorator-pattern
description: Reference for the Decorator design pattern in C# — wrapping an interface implementation to add behavior without modifying it, a generic decorator base class that forwards every member so a wide interface's decorators only override what they change, chaining multiple decorators together and choosing their order deliberately, and how decorator composition compares in intent to a DI container's own pipeline/interceptor-style wrapping features. Use when adding cross-cutting behavior (logging, caching, retry, validation, metrics) around an existing implementation without changing its class, reducing boilerplate for decorators over a wide interface, or deciding the order of a decorator chain.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Decorator Pattern

Decorator adds behavior to an object without modifying its class: a decorator implements the same
interface as the object it wraps, forwards calls to it, and adds its own behavior before, after, or
instead of forwarding.

## Quick start

```csharp
public interface IReportGenerator
{
    string Generate(ReportData data);
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
        return _inner.Generate(data);
    }
}
```

For an interface with many members, a generic decorator base class that forwards every member lets
a concrete decorator override only the ones it changes — see
[references/generic-decorator-base.md](references/generic-decorator-base.md).

## Pick your reference file

| Situation | Reference file |
| --- | --- |
| Writing a decorator that wraps a small interface | [references/classic-decorator.md](references/classic-decorator.md) |
| The wrapped interface has many members and full forwarding is repetitive | [references/generic-decorator-base.md](references/generic-decorator-base.md) |
| Composing multiple decorators and deciding their order | [references/chaining-decorators.md](references/chaining-decorators.md) |
| Comparing hand-written decorators to a DI container's own wrapping features | [references/decorator-vs-di-pipelines.md](references/decorator-vs-di-pipelines.md) |
| Testing a decorator or a chain of decorators | [references/testing-decorators.md](references/testing-decorators.md) |
| Adding a new decorator without touching existing ones | [references/extending-decorators.md](references/extending-decorators.md) |
