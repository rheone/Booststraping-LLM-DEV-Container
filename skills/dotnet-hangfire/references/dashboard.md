# Dashboard

The Hangfire Dashboard is a bundled web UI, mounted directly in your ASP.NET Core pipeline, that
shows queued/scheduled/processing/succeeded/failed jobs, recurring job schedules, and server status
against the same storage your application is configured against.

## Mounting the dashboard

```csharp
app.UseHangfireDashboard("/hangfire");
```

This adds middleware serving the dashboard at the given path. It reads directly from the configured
storage backend, so it reflects live job state — no separate data pipeline to keep in sync.

## Authorization is not optional

By default, `UseHangfireDashboard` allows only local requests. Before deploying to any environment
reachable beyond localhost, supply an authorization filter that checks the current user against
your own authentication/authorization scheme:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Administrator");
    }
}
```

The dashboard exposes job arguments, exception details, and the ability to trigger, delete, or
requeue jobs — treat it as an administrative surface requiring the same access control as any other
admin page, not as a read-only status page safe to leave open.

## What the dashboard is for

Use it to diagnose a specific failed job (its exception, its arguments, its retry history), confirm
a recurring job's next scheduled run, or manually trigger/requeue a job during an incident. It is an
operational tool for a human investigating job behavior, not an API — build a proper application
API for anything a program needs to query or act on programmatically.
