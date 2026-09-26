# Hot Reload with IOptionsMonitor

`IOptionsMonitor<TOptions>.OnChange` subscribes a callback that fires whenever the underlying
configuration for that options type changes at runtime — most commonly because a configuration
provider that supports change tokens (the JSON file provider with `reloadOnChange: true`, Azure App
Configuration, a custom provider you've wired to fire its own change tokens) detects a change.

## Subscribing to changes

```csharp
public sealed class RateLimiterOptionsWatcher : IDisposable
{
    private readonly IDisposable? _subscription;
    private RateLimiterOptions _current;

    public RateLimiterOptionsWatcher(IOptionsMonitor<RateLimiterOptions> monitor)
    {
        _current = monitor.CurrentValue;
        _subscription = monitor.OnChange(updated =>
        {
            _current = updated;
        });
    }

    public int CurrentLimit => _current.RequestsPerMinute;

    public void Dispose() => _subscription?.Dispose();
}
```

`OnChange` returns an `IDisposable` — dispose it when the subscriber itself is torn down (in a
singleton, typically in its own `Dispose`) to avoid the callback outliving the object it updates.

## What triggers a change notification

- **JSON configuration files** trigger reload when the file provider is registered with
  `reloadOnChange: true` (the default for `appsettings.json` in the ASP.NET Core web host) and the
  file's contents change on disk. A single file save can fire the callback more than once as the OS
  reports multiple change events for one edit — write callbacks to be idempotent against duplicate
  invocations, not just against no-op invocations.
- **Environment variables** do not trigger reload — they're read once at startup by the environment
  variable provider, which has no mechanism to detect a variable changing in the running process's
  environment.
- **Azure App Configuration and similar remote providers** trigger reload on their own polling or
  push mechanism, independent of the local file system.

## Reading `CurrentValue` directly vs. subscribing

Not every consumer needs `OnChange` — a service that just wants the latest value at the moment it's
used, without reacting proactively to a change, can read `monitor.CurrentValue` on each access
instead of maintaining a cached, callback-updated copy:

```csharp
public sealed class RateLimiter(IOptionsMonitor<RateLimiterOptions> monitor)
{
    public bool IsAllowed(int currentCount) => currentCount < monitor.CurrentValue.RequestsPerMinute;
}
```

Reserve `OnChange` for cases where the reaction to a change is itself the point — re-initializing a
connection, restarting a background loop's timer, invalidating a cache — not as a substitute for
simply reading `CurrentValue` each time you need the value.

## Named options and OnChange

`OnChange` fires for changes to **any** named instance of the options type; the callback receives
the name as its second parameter, so a subscriber interested in only one name must filter inside the
callback itself:

```csharp
monitor.OnChange((options, name) =>
{
    if (name == "Backup")
    {
        // react only to the "Backup" named instance changing
    }
});
```
