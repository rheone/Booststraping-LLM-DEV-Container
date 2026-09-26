# Misfire Handling

A misfire happens when a trigger's scheduled fire time passes without the scheduler actually firing
it — the scheduler was down, every worker thread was busy, or the process was otherwise unable to
service the trigger on time. Each trigger type has its own set of misfire instructions that decide
what happens once the scheduler notices.

## The default: smart policy

Every trigger defaults to `MisfirePolicy.SmartPolicy`, under which the trigger picks a
type-appropriate behavior on your behalf (generally: fire once immediately to catch up, then resume
its normal schedule) rather than requiring you to reason about misfire behavior for every trigger
you create. Leave this as the default unless a specific trigger's catch-up behavior would be wrong
for what it does.

## Cron trigger misfire instructions

```csharp
q.AddTrigger(t => t
    .ForJob(jobKey)
    .WithCronSchedule("0 0 8 * * ?", x => x
        .WithMisfireHandlingInstructionFireAndProceed()));
```

- **`WithMisfireHandlingInstructionFireAndProceed`** — fires once immediately to make up the missed
  run, then continues on the regular cron schedule. Appropriate when the job should still run for
  the day/period it missed.
- **`WithMisfireHandlingInstructionDoNothing`** — skips the missed fire entirely and waits for the
  next regularly scheduled time. Appropriate when a missed run is simply obsolete once its window
  has passed (a report for a specific hour that's meaningless to generate late).
- **`WithMisfireHandlingInstructionIgnoreMisfires`** — instructs Quartz.NET to fire every missed
  occurrence back-to-back until caught up. Rarely what you want for a cron schedule; a scheduler down
  for a day could fire an entire day's worth of missed runs in rapid succession.

## Simple trigger misfire instructions

```csharp
q.AddTrigger(t => t
    .ForJob(jobKey)
    .WithSimpleSchedule(x => x
        .WithIntervalInMinutes(5)
        .RepeatForever()
        .WithMisfireHandlingInstructionNextWithExistingCount()));
```

- **`WithMisfireHandlingInstructionFireNow`** — fires immediately once, then recomputes the
  remaining repeat count/schedule from now.
- **`WithMisfireHandlingInstructionNextWithExistingCount`** / **`NextWithRemainingCount`** — skips
  the missed fire(s) and resumes at the next interval, either preserving or reducing the originally
  configured repeat count.
- **`WithMisfireHandlingInstructionNowWithExistingCount`** / **`NowWithRemainingCount`** — fires
  immediately to catch up, using the current time as the new baseline for either the original or the
  remaining repeat count.

## Choosing an instruction deliberately

Pick a misfire instruction by asking what a missed fire *means* for that specific job: is a late
run still useful (fire-and-catch-up), or worthless once its window passes (skip and resume)? Do not
leave a job on `SmartPolicy` by default and then discover in production that its catch-up behavior
(or lack of one) was wrong for a job whose missed runs matter — a nightly billing job and a
five-minute health-check poll usually want opposite answers.
