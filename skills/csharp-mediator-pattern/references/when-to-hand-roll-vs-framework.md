# When to Hand-Roll vs. Adopt a Framework

The dispatcher in [hand-rolled-dispatcher.md](hand-rolled-dispatcher.md) is a few dozen lines. A
full third-party mediator framework adds pipeline behaviors (cross-cutting middleware around every
`Send` call — validation, logging, caching), assembly-scanning registration helpers, and a larger,
more thoroughly tested surface than a hand-rolled version will have on day one. Deciding between
them is a real tradeoff, not a default in either direction.

## Signals that favor hand-rolling

- **The request/handler set is small and stable.** A handful of request types with no expectation
  of rapid growth doesn't need generalized infrastructure for a problem that size.
- **Pipeline behaviors aren't needed, or the few that are needed are simple enough to write inline.**
  If the only cross-cutting concern is "log every request," a single line in the dispatcher does
  that without a behavior-pipeline abstraction around it.
- **Minimizing dependencies matters for this project** — a library meant for wide reuse, a codebase
  with a strict dependency-approval process, or a project that already has too many transitive
  dependencies to reason about.
- **The team wants full visibility into the dispatch mechanism.** A hand-rolled dispatcher is
  entirely inspectable in a few files; debugging exactly how a request reached its handler never
  requires stepping through code outside the project.

## Signals that favor a framework

- **The request/handler set is large and growing continuously**, especially across multiple teams
  or modules, where consistent registration conventions and tooling matter more than the dispatch
  mechanism's simplicity.
- **Multiple pipeline behaviors are needed and composed together** — validation, transaction
  scoping, caching, logging, authorization — where a hand-rolled equivalent would end up
  reimplementing a behavior-pipeline abstraction anyway, at which point using an existing, tested
  one is usually cheaper than maintaining an equivalent one.
- **The project already depends on the broader ecosystem a specific framework integrates with**,
  and that integration (health checks, diagnostics, source-generated registration) saves real
  setup work beyond dispatch itself.

## The middle ground

A hand-rolled dispatcher doesn't have to stay minimal forever — the pipeline-behavior gap is the
one capability most likely to justify starting with a framework instead. A hand-rolled version can
add its own simple pipeline hook without adopting a full framework:

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    TResponse Handle(TRequest request, Func<TResponse> next);
}

public sealed class Mediator : IMediator
{
    // ... resolve handler as before ...

    public TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        var behaviors = _services.GetServices<IPipelineBehavior<object, TResponse>>().Reverse();
        Func<TResponse> pipeline = () => InvokeHandler<TResponse>(request);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(request, next);
        }

        return pipeline();
    }

    private TResponse InvokeHandler<TResponse>(IRequest<TResponse> request) => default!; // as in hand-rolled-dispatcher.md
}
```

This is a genuine escalation of the hand-rolled dispatcher's scope, not a trivial addition — treat
adding a pipeline as the signal to re-evaluate whether a framework's already-built version of the
same capability is now the better tradeoff, rather than assuming hand-rolling remains the right call
indefinitely just because it was the right call at a smaller scale.
