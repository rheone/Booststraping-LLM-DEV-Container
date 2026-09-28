# Triggers

A trigger decides when a job fires. Quartz.NET's two everyday trigger types cover most scheduling
needs: cron triggers for calendar-based schedules, and simple triggers for fixed-interval repetition.

## Cron triggers

```csharp
q.AddTrigger(t => t
    .ForJob(jobKey)
    .WithIdentity("SendDailyDigest-trigger")
    .WithCronSchedule("0 0 8 * * ?"));
```

A cron trigger fires according to a cron expression — Quartz.NET's cron format has six or seven
fields (seconds, minutes, hours, day-of-month, month, day-of-week, and an optional year), not the
five-field Unix cron format. `0 0 8 * * ?` means "at 8:00:00 AM every day" — the `?` in the
day-of-week field means "no specific value," used when day-of-month already specifies the day.

Use a cron trigger whenever the schedule is naturally calendar-based: "every day at 8am," "every
weekday at noon," "on the first of the month." `CronScheduleBuilder` also exposes named helpers
(`CronScheduleBuilder.DailyAtHourAndMinute(8, 0)`) for common patterns that don't require reasoning
about raw cron syntax.

## Simple triggers

```csharp
q.AddTrigger(t => t
    .ForJob(jobKey)
    .WithIdentity("Poll-trigger")
    .StartNow()
    .WithSimpleSchedule(x => x
        .WithInterval(TimeSpan.FromMinutes(5))
        .RepeatForever()));
```

A simple trigger fires at a fixed interval, optionally a fixed number of times
(`WithRepeatCount(n)`) or indefinitely (`RepeatForever()`). Use a simple trigger for
interval-based work that isn't naturally calendar-aligned: "poll every 5 minutes," "retry every 30
seconds up to 3 times" — anything better expressed as "every N time units" than as a point on a
calendar.

## Choosing between them

Reach for a cron trigger when the schedule is expressed in terms of specific times or dates; reach
for a simple trigger when it's expressed in terms of a repeating interval starting from some point.
A job that needs both — a fixed daily start plus periodic retries around it — is usually two
triggers on the same job, not one trigger trying to express both semantics.

## Multiple triggers, one job

`ForJob(jobKey)` lets more than one trigger point at the same job — a job registered once can be
fired by a cron schedule and a separate on-demand simple trigger without duplicating the job
implementation. Each trigger's own `JobDataMap` can supply different parameters to the same job body
per trigger (see [job-data-maps.md](job-data-maps.md)).

## StartNow vs. StartAt

`StartNow()` makes a trigger eligible to fire as soon as the scheduler starts (subject to its
schedule); `StartAt(DateTimeOffset)` delays first eligibility until a specific point in time. Use
`StartAt` when a trigger should exist (and be visible in the scheduler) before it's actually allowed
to fire.
