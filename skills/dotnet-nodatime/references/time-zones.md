# Time zone handling

## `DateTimeZoneProviders`

The entry point for looking up a `DateTimeZone` by identifier. Two providers ship with the core
`NodaTime` package:

- `DateTimeZoneProviders.Tzdb` — resolves IANA/Olson identifiers (`"America/New_York"`,
  `"Europe/London"`, `"Asia/Tokyo"`) against the bundled tz database snapshot for the referenced
  NodaTime release. Use this for any zone identifier coming from user input, configuration, or an
  external system, since IANA identifiers are the portable, cross-platform standard.
- `DateTimeZoneProviders.Bcl` — resolves against `TimeZoneInfo` identifiers as known to the local
  OS, for interop with code that already has a `TimeZoneInfo` (e.g. from
  `TimeZoneInfo.FindSystemTimeZoneById`) and needs the equivalent `DateTimeZone`.

```csharp
DateTimeZone zone = DateTimeZoneProviders.Tzdb["America/New_York"];
```

Indexing with an unknown identifier throws `DateTimeZoneNotFoundException`; use
`DateTimeZoneProviders.Tzdb.GetZoneOrNull(id)` when an unrecognized identifier is an expected,
handled case (e.g. validating user input) rather than a configuration bug.

## Converting an `Instant` to a zone

`Instant.InZone(zone)` always succeeds and returns a `ZonedDateTime` — an instant is unambiguous,
so there is no DST edge case to resolve when going in this direction.

```csharp
Instant now = SystemClock.Instance.GetCurrentInstant();
ZonedDateTime local = now.InZone(DateTimeZoneProviders.Tzdb["Europe/London"]);
```

## Converting a `LocalDateTime` to a zone

Going the other direction is where DST creates ambiguity, because a `LocalDateTime` is just a
calendar/clock reading with no zone attached yet — the same wall-clock reading can occur twice (the
"fall back" hour) or not at all (the "spring forward" hour) in a given zone.

- `zone.AtStrictly(localDateTime)` — throws `SkippedTimeException` or `AmbiguousTimeException` if
  the local time falls in a gap or an overlap. Use this when an ambiguous or skipped input is a bug
  you want to surface immediately, not silently resolve.
- `zone.AtLeniently(localDateTime)` — picks a resolution automatically (the earlier of two mapped
  instants for an ambiguous time, or shifts forward past a gap for a skipped time) without asking.
  Use sparingly — a lenient resolution can silently produce a value the caller didn't ask for.
- `zone.ResolveLocal(localDateTime, resolver)` — takes an explicit `AmbiguousTimeResolver` and
  `SkippedTimeResolver` pair (or one of the ready-made combinations under `Resolvers`) so the
  ambiguous/skipped-time policy is a decision you make and can name, rather than an implicit default.

```csharp
try
{
    ZonedDateTime scheduled = zone.AtStrictly(new LocalDateTime(2026, 3, 8, 2, 30));
}
catch (SkippedTimeException)
{
    // 2:30 AM does not exist on the US spring-forward date in this zone.
}
```

## Converting between zones

Go through the shared `Instant`, not through the local values directly — subtract or reconstruct a
`ZonedDateTime` in the target zone from the source `ZonedDateTime`'s `.ToInstant()`.

```csharp
ZonedDateTime tokyoTime = meeting.WithZone(DateTimeZoneProviders.Tzdb["Asia/Tokyo"]);
```

`WithZone` preserves the underlying instant and recomputes the local representation for the new
zone — it does not reinterpret the same local clock reading in a different zone (that would be
`LocalDateTime` arithmetic, a different and much less common operation).
