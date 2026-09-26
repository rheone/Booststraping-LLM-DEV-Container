# Core Concept and Two Intents

## The problem

A set of objects that need to coordinate with each other — a form's controls reacting to each
other's changes, a group of command handlers a controller needs to invoke — can end up each holding
direct references to every other object it needs to talk to. As the set grows, so does the number
of pairwise references, until every object knows about most of the others and a change to any one
of them risks touching all of them.

## The pattern

A mediator is a single object that every colleague talks to instead of talking to each other
directly. A colleague sends the mediator something (an event, a request); the mediator decides what
happens next — which other colleague, if any, handles it. No colleague holds a reference to any
other colleague; every one of them holds a reference to the mediator only.

```csharp
public interface IDialogMediator
{
    void NotifyChanged(object sender, string propertyName);
}

public sealed class OrderFormMediator : IDialogMediator
{
    private readonly QuantityField _quantity;
    private readonly PriceField _price;
    private readonly TotalLabel _total;

    public OrderFormMediator(QuantityField quantity, PriceField price, TotalLabel total)
    {
        _quantity = quantity;
        _price = price;
        _total = total;
    }

    public void NotifyChanged(object sender, string propertyName)
    {
        if (sender == _quantity || sender == _price)
        {
            _total.SetValue(_quantity.Value * _price.Value);
        }
    }
}
```

`QuantityField` and `PriceField` never reference `TotalLabel` directly — they only call
`_mediator.NotifyChanged(this, ...)`. Adding a fourth field that also affects the total means
changing the mediator, not every existing field.

## Two distinct intents

The word "mediator" covers two related but different jobs in C# code, and conflating them leads to
designs that don't fit either one cleanly.

### Intent 1: mediating colleague objects (the classic intent)

The example above is the classic intent — a fixed, known set of collaborating objects (UI controls,
components in a subsystem) that need to react to each other without referencing each other
directly. The mediator here typically has domain-specific methods (`NotifyChanged`,
`ItemSelected`) rather than a single generic `Send`, because it's coordinating a specific, small
group of known participants, not routing arbitrary requests to arbitrary handlers.

### Intent 2: request/handler dispatch (the common modern usage)

The far more common shape in current C# codebases uses "mediator" to mean a generic dispatcher: a
caller constructs a request object and hands it to a single `Send` (or `Publish`) method, and the
mediator locates whichever handler is registered for that request's type and invokes it. The caller
never references the handler type at all — not because the handler is one of several colleagues
reacting to each other, but because decoupling the caller from every individual handler keeps a
class like a web controller from taking a constructor dependency on every handler it might ever
need to invoke.

```csharp
public sealed class OrdersController
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) => _mediator = mediator;

    public OrderDto Get(int id) => _mediator.Send(new GetOrderQuery(id));
    public void Post(CreateOrderCommand command) => _mediator.Send(command);
}
```

This is the shape [hand-rolled-dispatcher.md](hand-rolled-dispatcher.md) and
[generic-mediator.md](generic-mediator.md) build out in full, and the one most C# code reaching for
"mediator" today actually means. It fits naturally into a CQRS-style codebase — where commands and
queries are already modeled as distinct request objects — as the mechanism that routes each one to
its handler, though nothing about the dispatch mechanism itself requires a CQRS architecture around
it.

## Telling them apart in practice

Ask what's actually being decoupled. If it's a small, fixed set of objects that all need to react
to each other's state changes, you want intent 1's domain-specific mediator with purpose-named
methods. If it's a caller that shouldn't need a reference to every possible handler it might invoke,
and requests are open-ended (new request types get added over time without the caller changing),
you want intent 2's generic `Send`-based dispatcher.
