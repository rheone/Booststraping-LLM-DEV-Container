# Instrumentation libraries

## What an instrumentation library does

An instrumentation library hooks into an existing framework or client (ASP.NET Core's request
pipeline, `HttpClient`, a database driver) and emits `Activity`/metric data for it automatically —
without your code adding a single manual span or measurement for that framework's own work.
Instrumentation packages are added inside the relevant `WithTracing`/`WithMetrics` builder, not
`AddOpenTelemetry()` itself.

## ASP.NET Core instrumentation

```csharp
.WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
.WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation())
```

`AddAspNetCoreInstrumentation()` (package `OpenTelemetry.Instrumentation.AspNetCore`) creates one
`Activity` per incoming HTTP request, tagged with the route, method, and status code, and records
request-duration metrics — the root span most incoming-request traces build on. Every custom
`Activity` your own code starts during that request (`references/custom-tracing.md`) becomes a child
of this one automatically, as long as it's started while that request's `Activity` is still current.

## `HttpClient` instrumentation

```csharp
.WithTracing(tracing => tracing.AddHttpClientInstrumentation())
```

`AddHttpClientInstrumentation()` (also in the `OpenTelemetry.Instrumentation.AspNetCore` package's
family, or its own dedicated package depending on the version) wraps outgoing `HttpClient` requests
with a span for each call, and propagates trace context onto the outgoing request's headers
automatically (see `references/context-propagation.md`) — without this, an outbound HTTP call from
an instrumented service to another service loses the trace continuity between them.

## Other auto-instrumentation packages

The same pattern — a NuGet package named `OpenTelemetry.Instrumentation.<Technology>`, added inside
`WithTracing`/`WithMetrics` via an `Add<Technology>Instrumentation()` extension method — covers
other common dependencies (a specific database client, a specific messaging client, gRPC, and
others). Each targets one technology's own client library and instruments only that library's calls
— check the specific package for the technology in use, since coverage, configuration options, and
exact instrumentation depth vary per package and per version.

## Configuring what an instrumentation library captures

Most instrumentation extension methods accept an options delegate to filter or enrich what gets
recorded, e.g. excluding certain routes from ASP.NET Core instrumentation or adding extra tags to
every captured `HttpClient` span:

```csharp
.AddAspNetCoreInstrumentation(options =>
{
    options.Filter = httpContext => httpContext.Request.Path != "/health";
})
```

Filtering out high-volume, low-value routes (health checks, readiness probes) this way keeps
exported trace volume focused on requests actually worth analyzing.

## Instrumentation libraries vs. custom spans

Instrumentation libraries cover *framework-level* work (an HTTP request arriving, an outbound call
going out) automatically. They don't know anything about your own domain logic inside a request —
for that, `references/custom-tracing.md` covers starting your own `Activity` instances to make an
application's internal steps visible inside the trace an instrumentation library already started.
