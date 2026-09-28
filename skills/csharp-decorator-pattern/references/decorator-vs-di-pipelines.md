# Decorator Intent vs. Container Pipeline/Interceptor Features

Some dependency injection containers offer a built-in mechanism for wrapping a resolved instance
with additional behavior — registering an interceptor, a pipeline behavior, or a decorator
registration that the container applies automatically whenever it resolves a given interface. This
file describes how that class of feature relates to the Decorator pattern in intent, without naming
or assuming any specific container product.

## The shared intent

Both a hand-written decorator chain and a container's built-in wrapping mechanism solve the same
problem: adding cross-cutting behavior (logging, caching, validation, retry, metrics) around a call
to an existing interface implementation, without modifying that implementation's class or any code
that already depends on the interface. Whether the wrapping object is constructed by hand
(`new LoggingReportGenerator(new BasicReportGenerator())`) or assembled by a container following a
registration rule, the resulting object graph is structurally identical: an outer object holding an
inner object behind the same shared interface, forwarding and augmenting each call.

## Where they differ

- **Who builds the chain.** A hand-written decorator chain is explicit code you read top to bottom,
  as in [chaining-decorators.md](chaining-decorators.md). A container-driven wrapping mechanism
  builds the equivalent chain from registration metadata (attributes, fluent registration calls, or
  convention) at resolution time — the chain still exists, but its shape lives in configuration
  rather than in a constructor call you can step through in a debugger as plainly.
- **Cross-cutting scope.** A hand-written decorator wraps one interface, deliberately, at one
  construction site. A container-driven mechanism is often designed to apply a behavior (logging,
  transaction handling) across every resolution of many interfaces that match a convention, which
  trades per-case deliberateness for consistency applied automatically across a whole codebase.
- **Where the composition logic lives.** A hand-written chain's ordering logic (see
  [chaining-decorators.md](chaining-decorators.md)) is visible source code. A container's automatic
  wrapping applies whatever ordering rule the container's own configuration establishes, which needs
  to be understood from that configuration rather than from the call site using the interface.

## Choosing between them

Reach for an explicit, hand-written decorator chain when the set of decorators applied is small,
specific to one interface, and benefits from being readable directly in the code that assembles it —
most single-service cross-cutting concerns fit this case. Reach for a container's automatic wrapping
mechanism when the same cross-cutting behavior needs to apply consistently across a large number of
interfaces by convention, and maintaining a hand-written chain at every one of those construction
sites would itself become the boilerplate problem the pattern exists to avoid. Either way, the
object built at the end is a decorator in the structural sense described throughout this skill —
an object forwarding calls to another instance of the same interface while adding its own behavior
around that call.
