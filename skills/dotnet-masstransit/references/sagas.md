# Sagas

A saga tracks state across a sequence of messages that together represent one long-running business
process — an order that moves through submission, payment, and fulfillment over minutes, hours, or
days, where a single consumer handling a single message isn't enough because the process spans
multiple messages arriving at different times.

## The concept: state plus correlation

A saga instance is a persisted piece of state, keyed by a `CorrelationId`, that MassTransit loads,
updates, and saves as each relevant message arrives. Where a stateless consumer forgets everything
between messages, a saga remembers what has already happened for a specific process instance (a
specific order, a specific workflow run) and decides what to do next based on both the incoming
message and the state already recorded.

## MassTransitStateMachine: state machine as saga

MassTransit's built-in approach expresses a saga as an explicit state machine:

```csharp
public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;
    public decimal Total { get; set; }
}

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Submitted { get; private set; } = default!;
    public State PaymentReceived { get; private set; } = default!;

    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; } = default!;
    public Event<PaymentProcessed> PaymentProcessedEvent { get; private set; } = default!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmittedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderSubmittedEvent)
                .Then(context => context.Saga.Total = context.Message.Total)
                .TransitionTo(Submitted));

        During(Submitted,
            When(PaymentProcessedEvent)
                .TransitionTo(PaymentReceived));
    }
}
```

Each `Event` correlates an incoming message to a saga instance by a shared identifier
(`CorrelateById`). Each `State` names a point the process can be in; `Initially`/`During` blocks
declare which events are valid in which state and what happens when one occurs — updating saga
data, transitioning to a new state, or publishing/sending further messages.

## Registration

```csharp
x.AddSagaStateMachine<OrderStateMachine, OrderState>()
    .InMemoryRepository();
```

A saga needs a repository to persist instances between messages — an in-memory repository is
sufficient for local development and testing; a production deployment persists saga state to
durable storage so an instance survives a process restart.

## When a saga is the right tool

Reach for a saga when a process genuinely spans multiple independent messages over time and needs
to remember state between them. A request that's fully handled by a single consumer reacting to a
single message never needs one — introducing saga machinery for a process with no meaningful
intermediate state just adds a persistence and correlation burden with nothing to show for it.
