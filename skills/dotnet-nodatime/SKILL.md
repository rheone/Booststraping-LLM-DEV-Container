---
name: dotnet-nodatime
description: Guidance on NodaTime, a third-party date/time library for C#/.NET (current stable release 3.3.4, targets .NET 8.0 and .NET Standard 2.0). Covers why NodaTime's type set exists as a factual alternative to System.DateTime/DateTimeOffset's ambiguity around time zones and offsets, the core types (Instant, LocalDate, LocalTime, LocalDateTime, ZonedDateTime, Offset, Duration, Period), time zone handling via DateTimeZoneProviders (Tzdb/Bcl), parsing and formatting with NodaTime.Text patterns, and interop with BCL DateTime/DateTimeOffset at system boundaries (serialization, database columns, external APIs). Use when writing, reviewing, or debugging code that models a point in time, a calendar date, a time zone conversion, or a duration/period using NodaTime's types, or when deciding how to convert between NodaTime and System.DateTime/DateTimeOffset at an I/O boundary.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# NodaTime

Guidance on NodaTime, a third-party date and time library for .NET. Current stable release as of
this writing: **3.3.4**, targeting **.NET 8.0** and **.NET Standard 2.0**
(the latter covers .NET Framework 4.6.1+ and .NET Core 2.0+). Organized by concern/topic — each
reference file notes a version-introduced fact inline rather than splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Choosing which type models a value: a calendar date, a wall-clock time, an instant, a duration | `LocalDate`, `LocalTime`, `LocalDateTime`, `Instant`, `ZonedDateTime`, `Offset`, `Duration`, `Period` | [references/core-types.md](references/core-types.md) |
| Converting a `LocalDateTime` to a real moment in time, or handling DST transitions | `DateTimeZone`, `DateTimeZoneProviders.Tzdb`, `IDateTimeZoneProvider`, ambiguous/skipped local time resolvers | [references/time-zones.md](references/time-zones.md) |
| Parsing user/external input or formatting output text | `NodaTime.Text` pattern types (`LocalDatePattern`, `InstantPattern`, `ZonedDateTimePatterns`, etc.), `ParseResult<T>` | [references/parsing-and-formatting.md](references/parsing-and-formatting.md) |
| Sending/receiving a value across a boundary that only knows `DateTime`/`DateTimeOffset` (a database column, JSON payload, third-party API) | `.ToDateTimeUtc()`, `Instant.FromDateTimeUtc`, `LocalDateTime.ToDateTimeUnspecified()`, round-trip pitfalls | [references/bcl-interop.md](references/bcl-interop.md) |
| Unit testing code that depends on the current time or a time zone conversion | `IClock`, `FakeClock`, injecting a clock instead of calling `SystemClock.Instance` directly | [references/testing-with-nodatime.md](references/testing-with-nodatime.md) |

## Quick start

The type to reach for depends on what kind of "time" the value actually represents — NodaTime's
core design decision is refusing to let one type stand in for several distinct concepts.

```csharp
using NodaTime;

// A specific, unambiguous point on the global timeline.
Instant now = SystemClock.Instance.GetCurrentInstant();

// A calendar date with no time-of-day or zone attached: a birthday, a due date.
LocalDate dueDate = new LocalDate(2026, 12, 31);

// An instant expressed in a particular time zone's wall-clock terms, DST-aware.
DateTimeZone zone = DateTimeZoneProviders.Tzdb["America/New_York"];
ZonedDateTime meeting = now.InZone(zone);

Console.WriteLine(meeting.ToString("yyyy-MM-dd HH:mm x", null));
```

## Why NodaTime's type set exists

`System.DateTime` carries a `Kind` (`Utc`, `Local`, `Unspecified`) but no time zone identifier, and
arithmetic across two `DateTime` values with different `Kind`s silently ignores that difference
rather than erroring — adding a `Utc` value to an `Unspecified` one compiles and runs without
warning. `System.DateTimeOffset` fixes the "what UTC offset was this captured at" question but
still carries no time zone identity, so it cannot answer "what will this local wall-clock time be
after this zone's next DST transition" — an offset is a fixed number, not a rule. Both BCL types
conflate "a calendar date," "a wall-clock time," "a precise instant," and "a length of elapsed
time" into overlapping constructors and properties on the same handful of types, so a method
signature like `DateTime` alone does not tell a reader which of those four concepts it holds.

NodaTime's type set exists to make each of those four concepts (plus "an instant tied to a
specific zone identity," `ZonedDateTime`) its own type, so a method signature states which kind of
time-related value it needs, and the compiler rejects mixing them by accident.

## Out of scope

- Calendar systems other than the ISO calendar (`CalendarSystem.Iso`) — NodaTime supports several
  non-ISO calendar systems (Hebrew, Islamic, Persian, etc.) as a narrow, deep domain of its own.
- The `NodaTime.Serialization.*` packages' wiring into a specific JSON/serialization framework's
  converter registration — this skill covers the underlying conversion operations those converters
  call, not a specific framework's converter-registration API.
- Time zone database maintenance/updates (the `NodaTime.TimeZones` compiler tooling) — an
  operational concern distinct from using the types this skill covers.
