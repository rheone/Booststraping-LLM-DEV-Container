# Philosophy and Structure

Chain of Responsibility decouples a request's sender from the object that eventually handles it, by
linking a series of handler objects and passing the request along the chain until one of them
handles it (or the chain ends).

- **Handler** — an object that either processes a request itself or forwards it to the next handler
  in the chain. Each handler holds a reference to only the *next* handler, never the whole chain.
  Its decision to handle or forward is made independently of every other handler.
- **Client** — the code that submits a request to the *first* handler in the chain. It has no idea
  how many handlers exist, what order they run in, or which one ultimately handles the request.

```text
Client ---> Handler A ---> Handler B ---> Handler C ---> (end of chain)
```

## The basic shape

```csharp
public abstract class Handler
{
    private Handler? _next;

    public Handler SetNext(Handler next)
    {
        _next = next;
        return next;
    }

    public Result? Handle(Request request) =>
        TryHandle(request) ?? _next?.Handle(request);

    protected abstract Result? TryHandle(Request request);
}
```

`TryHandle` returns `null` to mean "not my request, pass it on"; `Handle` wires that decision to
forwarding automatically, so every concrete handler only has to implement `TryHandle`.

`SetNext` returning the handler passed to it lets you build a chain by chaining the calls:

```csharp
var first = new HandlerA();
first.SetNext(new HandlerB()).SetNext(new HandlerC());
```

## When you reach for it

Use this pattern when:

- More than one object could plausibly handle a request, and which one actually does depends on
  the request's own data, not on a decision the client can or should make.
- You want to add, remove, or reorder handlers without touching the client that submits requests to
  the chain, or the other handlers already in it.
- Each handler's decision logic is genuinely independent — it doesn't need visibility into what
  other handlers in the chain do or have already tried.

## When you don't need it

If exactly one object always handles a given kind of request, and that mapping never changes at
runtime, a direct call to that object is simpler than routing the request through a chain with only
one link. Reach for the pattern once there's real uncertainty about which handler (if any) will
take responsibility, or a real need to vary the chain's membership independently of the client.
