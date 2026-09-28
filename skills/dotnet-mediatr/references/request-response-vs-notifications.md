# Request/Response vs. Fire-and-Forget Notifications

MediatR gives you two distinct dispatch shapes with different guarantees. Picking the wrong one
for a given use case is a design smell, not just a style preference.

## Request/response (`Send`)

- Exactly one handler, resolved and required to exist.
- Caller gets a return value (or `Unit`/nothing, but still a single synchronous-style completion
  it awaits).
- Caller knows, at the point of the `Send` call, that the operation either completed or threw —
  there's no ambiguity about "did this happen."
- Natural fit for: commands that must succeed-or-fail as a unit and the caller needs to know
  which; queries where a result is obviously required.

```csharp
var orderId = await sender.Send(new CreateOrder(customerId, total), cancellationToken);
// caller has orderId here, or an exception was thrown — no other outcome
```

## Notification (`Publish`)

- Zero-to-many handlers, none required to exist (publishing to a notification with no handlers
  registered is a legitimate no-op, not an error).
- Caller gets no result data back from any handler.
- Caller cannot assume any particular handler ran, or that "the" thing it cares about happened —
  only that the event was raised. Depending on the configured `INotificationPublisher` (see
  [notifications-and-publishing.md](notifications-and-publishing.md)), a failing handler may or
  may not have prevented others from running.
- Natural fit for: "something happened, and other parts of the system may or may not care" —
  domain events, integration triggers, side-effect fan-out that the triggering code should be
  decoupled from.

```csharp
await publisher.Publish(new OrderCreated(orderId, customerId), cancellationToken);
// caller does not know, or need to know, which downstream handlers ran
```

## The decision test

Ask: **does the caller need to know the outcome of this specific operation to proceed correctly?**

- Yes → request/response. If "send the confirmation email" fails and the calling code needs to
  retry, surface an error, or otherwise branch on that failure, it should not be modeled as a
  notification handler the caller can't see into.
- No → notification. If the calling code's own logic is complete regardless of whether zero,
  one, or five other things react to the event, that reaction belongs in a notification handler.

## A common anti-pattern: notifications used as a response-hiding request

Publishing an event and then having exactly one handler that does something the *caller* actually
depends on (e.g. publishing `OrderCreated` and expecting a specific handler to synchronously
populate a value the controller then reads from a side channel) defeats the purpose of both
mechanisms — it's a request wearing a notification's clothes, minus the compiler-enforced
guarantee that a handler exists and minus a return value. If the caller has a real dependency on
"this specific thing happened and I need its result," that's a request/response, full stop.

## A common anti-pattern in the other direction: modeling independent side effects as sequential Sends

Chaining several unrelated `Send` calls after a command completes, purely to trigger unrelated
side effects (send an email, log an audit entry, notify another bounded context) that the calling
code has no actual interest in the result of, reintroduces tight coupling between the caller and
every side effect — exactly what `Publish`/`INotification` exists to avoid. If the caller doesn't
need any of those results, they're events, not requests.
