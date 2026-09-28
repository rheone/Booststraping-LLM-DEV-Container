# NodaTime

Guidance on NodaTime, the third-party date and time library for .NET that splits "a calendar
date," "a wall-clock time," "a precise instant," and "a duration" into distinct types so a method
signature states which kind of time value it actually needs.

## When to reach for it

- Deciding which type models a value (a calendar date, a wall-clock time, an instant, or a
  duration) instead of reaching for a bare `DateTime`.
- Converting a local date/time to a real moment in time, or handling a daylight-saving-time
  transition correctly.
- Parsing external input or formatting output text with `NodaTime.Text` patterns.
- Sending or receiving a value across a boundary that only knows `DateTime`/`DateTimeOffset`: a
  database column, a JSON payload, a third-party API.
- Unit testing code that depends on the current time or a time zone conversion.

## Using it

This skill is model-invoked: it fires automatically when you're modeling a point in time, a
calendar date, a time zone conversion, or a duration/period using NodaTime's types. You can also
invoke it directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| LocalDate, LocalTime, LocalDateTime, Instant, ZonedDateTime, Offset, Duration, Period | [references/core-types.md](references/core-types.md) |
| DateTimeZone, DateTimeZoneProviders, ambiguous/skipped local time resolution | [references/time-zones.md](references/time-zones.md) |
| NodaTime.Text pattern types, ParseResult\<T>, custom pattern strings | [references/parsing-and-formatting.md](references/parsing-and-formatting.md) |
| Converting to/from System.DateTime and System.DateTimeOffset at I/O boundaries | [references/bcl-interop.md](references/bcl-interop.md) |
| IClock, FakeClock, injecting a clock instead of SystemClock.Instance | [references/testing-with-nodatime.md](references/testing-with-nodatime.md) |

## Example prompts

- "What NodaTime type should I use for a user's birthday versus a meeting time in their zone?"
- "Convert this ZonedDateTime to UTC for storage and back again on read."
- "Make this service testable by injecting a clock instead of calling SystemClock.Instance directly."
