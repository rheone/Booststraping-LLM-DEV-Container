# Chain of Responsibility vs. a Processing Pipeline

A request-processing pipeline — the style where each stage receives the request plus a delegate for
"the rest of the pipeline," and explicitly decides whether to call that delegate — is conceptually
close to Chain of Responsibility, but the two differ in a way worth naming precisely rather than
treating as the same thing under two names.

## The pipeline shape

```csharp
public delegate Task Next();
public delegate Task PipelineStage(RequestContext context, Next next);

public sealed class Pipeline
{
    private readonly List<PipelineStage> _stages = new();

    public Pipeline Use(PipelineStage stage)
    {
        _stages.Add(stage);
        return this;
    }

    public Task RunAsync(RequestContext context) => RunFrom(0, context);

    private Task RunFrom(int index, RequestContext context)
    {
        if (index >= _stages.Count)
        {
            return Task.CompletedTask;
        }

        return _stages[index](context, () => RunFrom(index + 1, context));
    }
}
```

Each stage looks like this:

```csharp
Pipeline pipeline = new Pipeline()
    .Use(async (context, next) =>
    {
        Console.WriteLine("before");
        await next();
        Console.WriteLine("after");
    })
    .Use(async (context, next) =>
    {
        context.Result = "handled";
        await next();
    });
```

## Where the two patterns actually differ

- **Default direction of flow.** A Chain of Responsibility handler that does nothing special simply
  doesn't forward, or forwards implicitly through a base class's `Handle` — "stop" is often the
  default outcome for a handler that isn't interested in the request (see
  [short-circuit-vs-always-continue.md](short-circuit-vs-always-continue.md)). A pipeline stage
  that does nothing special calls `next()` — continuing to the rest of the pipeline is the default,
  explicit action every stage takes unless it deliberately chooses not to.
- **Control after the rest of the chain runs.** A Chain of Responsibility handler generally has no
  code that runs *after* a later handler in the chain — once it forwards, its own involvement in
  that request is over. A pipeline stage explicitly `await`s `next()`, meaning it can run code both
  before *and after* every later stage completes — the "before/after" wrapping shape in the example
  above has no equivalent in a typical Chain of Responsibility handler.
- **What "the next thing" is.** A Chain of Responsibility handler holds a reference to a fixed next
  *handler object*, decided when the chain was built. A pipeline stage receives a *delegate*
  representing "everything after me," which it's free to call zero, one, or (unusually) more than
  once — the delegate is a first-class value passed per-invocation, not a persistent link on the
  stage itself.

## Why this distinction matters when choosing between them

Reach for the Chain of Responsibility shape when each handler's job is fundamentally a decision —
"is this my request to handle?" — with no need to wrap behavior around the handlers that come after
it. Reach for the pipeline shape when a stage's job includes wrapping the rest of the process — timing
it, transforming its outcome, catching and translating what happens downstream — because only the
explicit-next-delegate form gives a stage code that runs after the remainder of the chain completes.

Both are legitimate designs for "a request passes through a series of participants" — the difference
is whether any participant needs to observe or act on what happens *after* it in the sequence, not
just whether the sequence continues at all.
