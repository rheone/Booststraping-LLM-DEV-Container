# Context propagation across service boundaries

## The problem propagation solves

A trace that spans multiple services (service A calls service B over HTTP, or publishes a message
service C later consumes) is only a single connected trace if each hop carries enough information
for the next service to attach its own spans as children of the same trace, rather than starting an
unrelated trace of its own. Context propagation is the mechanism that carries that information
across the boundary.

## The W3C Trace Context standard

By default, OpenTelemetry .NET propagates trace context using the W3C Trace Context standard: a
`traceparent` header (and, when present, `tracestate`) carrying the trace ID, the calling span's ID,
and sampling flags. This is the default propagator — no extra configuration is needed for two
OpenTelemetry-instrumented .NET services talking over HTTP to stay connected in the same trace, as
long as both sides have the relevant instrumentation enabled (`AddHttpClientInstrumentation()` on
the caller, `AddAspNetCoreInstrumentation()` on the callee — see
`references/instrumentation-libraries.md`).

## How it flows through outgoing HTTP calls

`AddHttpClientInstrumentation()` injects the `traceparent` header onto every outgoing `HttpClient`
request automatically, populated from whatever `Activity.Current` is at the moment the request is
sent — this is why starting a custom `Activity` (`references/custom-tracing.md`) before making an
outbound call causes that call's span to correctly nest under your custom span, and why an outbound
call made with no `Activity.Current` at all (tracing not active, or `StartActivity` returned `null`)
still works but starts a fresh, disconnected trace on the receiving side.

## Receiving propagated context

`AddAspNetCoreInstrumentation()` reads an incoming request's `traceparent` header and starts that
request's root `Activity` as a *child* of the incoming context instead of a new root — the
receiving service's spans automatically join the caller's trace with no manual header parsing
required.

## Baggage: propagating arbitrary key/value context

`Baggage` (from the OpenTelemetry API package) carries arbitrary key/value pairs across service
boundaries alongside trace context — for a value that isn't itself a span attribute but that
downstream code or downstream spans should have access to (a tenant ID, a feature flag value driving
sampling decisions further downstream):

```csharp
Baggage.Current = Baggage.Current.SetBaggage("tenant.id", tenantId);
```

Baggage propagates over the wire the same way trace context does (via a `baggage` header, per the
W3C Baggage spec) but is not automatically attached to spans as tags — read it explicitly
(`Baggage.Current.GetBaggage("tenant.id")`) wherever a downstream component needs it, and add it as
a tag on a specific `Activity` explicitly if you want it visible on that span.

## Non-HTTP transports

For message queues and other non-HTTP transports, an instrumentation library for that specific
technology (`references/instrumentation-libraries.md`) is responsible for injecting/extracting
propagated context into whatever the transport's own message-attribute mechanism is (message
headers/properties) — the W3C Trace Context format itself is transport-agnostic, but *how* it rides
along a given transport is specific to that transport's instrumentation package, since there's no
universal "headers" concept across every messaging technology the way there is for HTTP.
