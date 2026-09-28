# Core types

Each type below models exactly one of "a calendar date," "a wall-clock time," "a precise instant,"
"an instant tied to a zone," or "a length of time" — pick the type that matches what the value
actually represents, not the type that happens to have the fields you need.

## `LocalDate`

A calendar date with no time-of-day and no time zone: a birthday, a due date, a fiscal period
boundary. Construct it with a calendar system (defaulting to ISO) plus year/month/day.

```csharp
LocalDate dueDate = new LocalDate(2026, 12, 31);
LocalDate nextWeek = dueDate.PlusWeeks(1);
```

`LocalDate` arithmetic (`PlusDays`, `PlusMonths`, `PlusYears`) is calendar-aware: adding a month to
January 31 lands on the last valid day of February, not an invalid date.

## `LocalTime`

A wall-clock time with no date and no zone: "the store opens at 09:00." Rarely used standalone —
usually combined into a `LocalDateTime`.

```csharp
LocalTime open = new LocalTime(9, 0);
```

## `LocalDateTime`

A calendar date plus a wall-clock time, still with no time zone attached: "January 1st at noon," as
it would be read off a calendar and a clock with no indication of which time zone's calendar and
clock. This is what you have before you know which zone's rules apply to it.

```csharp
LocalDateTime meeting = new LocalDateTime(2026, 3, 15, 14, 30);
```

A `LocalDateTime` alone cannot answer "how many seconds until this happens" or "what UTC instant
is this" — those questions require a time zone (see
[references/time-zones.md](time-zones.md)).

## `Instant`

An unambiguous point on the global timeline, internally a count of nanoseconds since the Unix
epoch. Two `Instant` values are directly comparable regardless of what zone either was created in.

```csharp
Instant now = SystemClock.Instance.GetCurrentInstant();
Instant epoch = Instant.FromUnixTimeSeconds(0);
```

## `Offset`

A fixed UTC offset (e.g. `+02:00`), with no time zone identity and no DST rule attached — the same
kind of value `DateTimeOffset` carries, isolated into its own type.

```csharp
Offset offset = Offset.FromHours(-5);
```

## `ZonedDateTime`

An `Instant` paired with a `DateTimeZone` identity, letting you ask both "what instant is this" and
"what did the wall clock read in that zone at that instant" — and, because it carries the zone
identity (not just a fixed offset), it can tell you what the next DST transition changes.

```csharp
DateTimeZone zone = DateTimeZoneProviders.Tzdb["America/New_York"];
ZonedDateTime meeting = zone.AtStrictly(new LocalDateTime(2026, 3, 15, 14, 30));
```

`AtStrictly` throws if the local date/time is ambiguous or skipped by a DST transition; see
[references/time-zones.md](time-zones.md) for the resolvers that handle those cases instead of
throwing.

## `Duration`

An exact length of elapsed time, measured in nanoseconds — unaffected by calendars, time zones, or
DST. Two instants always differ by a `Duration` you can compute with simple subtraction.

```csharp
Duration elapsed = laterInstant - earlierInstant;
Duration fiveMinutes = Duration.FromMinutes(5);
```

## `Period`

A calendar-based length of time expressed in human units (years, months, days, hours, ...) that
does *not* have a fixed length — "one month" is 28-31 days depending on which month, and "one day"
can be 23 or 25 hours across a DST transition. Use `Period` for "add one month to this date" and
`Duration` for "exactly how much time has elapsed."

```csharp
Period oneMonth = Period.FromMonths(1);
LocalDate later = dueDate.Plus(oneMonth);

Period difference = Period.Between(startDate, endDate, PeriodUnits.YearMonthDay);
```

Mixing up `Duration` and `Period` is the most common core-type mistake: adding a `Duration` of "30
days" to a date always advances by exactly 30×24 hours, while adding a `Period` of "1 month"
respects the calendar and can land on a different total elapsed time depending on the month and any
DST transitions crossed.
