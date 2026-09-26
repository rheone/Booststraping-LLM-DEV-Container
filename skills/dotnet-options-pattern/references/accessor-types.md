# Accessor Types: IOptions, IOptionsSnapshot, IOptionsMonitor

The three accessor interfaces differ in DI lifetime and in whether they observe configuration
changes made after the app starts. Picking the wrong one either locks a singleton to stale values it
should refresh, or pays reload overhead a value that never changes doesn't need.

## IOptions\<TOptions\>: singleton, computed once

`IOptions<TOptions>` is registered as a singleton. It computes `.Value` once, the first time it's
resolved, and caches it for the lifetime of the application — it never reflects a configuration
change made after that first resolution, no matter how the underlying configuration source changes.

```csharp
public sealed class StartupBanner(IOptions<SmtpOptions> options)
{
    public string Describe() => $"SMTP host: {options.Value.Host}";
}
```

Inject this when the consuming service is itself a singleton and the option values genuinely should
not change without an application restart (e.g. values that gate one-time startup wiring).

## IOptionsSnapshot\<TOptions\>: scoped, recomputed per scope

`IOptionsSnapshot<TOptions>` is registered as scoped. It recomputes `.Value` once per DI scope (in
ASP.NET Core, effectively once per HTTP request) by re-reading the current configuration state at
the start of that scope. It picks up configuration changes made since the last scope, but only at
scope boundaries — not mid-request.

```csharp
public sealed class TenantSettingsService(IOptionsSnapshot<TenantOptions> options)
{
    public TenantOptions Current => options.Value;
}
```

Inject this in scoped or transient services in a web app when you want each request to see
up-to-date configuration without needing to react to a change mid-request. It cannot be injected
into a singleton — a singleton captured a scoped dependency at construction would pin it to whatever
scope built the singleton, defeating the per-scope recomputation and typically throwing or warning
depending on how strict scope validation is configured.

## IOptionsMonitor\<TOptions\>: singleton, live-updating, with change notifications

`IOptionsMonitor<TOptions>` is registered as a singleton, but unlike `IOptions<TOptions>`, its
`.CurrentValue` property always reflects the latest configuration state — it re-reads on every
access rather than caching a single computed value. It also exposes `OnChange`, a subscription
callback that fires whenever the bound configuration changes (see
[hot-reload.md](hot-reload.md)).

```csharp
public sealed class RateLimiterOptionsHolder(IOptionsMonitor<RateLimiterOptions> monitor)
{
    public int CurrentLimit => monitor.CurrentValue.RequestsPerMinute;
}
```

Inject this into a singleton service that needs to react to configuration changes without a
restart — a background worker reading a polling interval, a rate limiter reading its current
threshold, or anything using `OnChange` to re-initialize state when a value changes.

## Choosing between them

| Consumer lifetime | Needs live updates? | Accessor |
| --- | --- | --- |
| Singleton | No | `IOptions<TOptions>` |
| Singleton | Yes | `IOptionsMonitor<TOptions>` |
| Scoped/transient (e.g. per-request) | Yes, at scope granularity | `IOptionsSnapshot<TOptions>` |
| Scoped/transient | No (values never change at runtime) | `IOptions<TOptions>` still works, but `IOptionsSnapshot<TOptions>` costs nothing extra and stays correct if that assumption changes later |

`IOptionsMonitor<TOptions>` is the only one of the three safe to inject into a singleton when you
need current values — injecting `IOptionsSnapshot<TOptions>` into a singleton is a captive-dependency
bug.
