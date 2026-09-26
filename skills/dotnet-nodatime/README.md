# NodaTime

Guidance on NodaTime, a third-party date and time library for C#/.NET — the routing table (by
situation, not by NodaTime/C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per NodaTime/C# version

| File | Covers |
| --- | --- |
| `core-types.md` | LocalDate, LocalTime, LocalDateTime, Instant, ZonedDateTime, Offset, Duration, Period |
| `time-zones.md` | DateTimeZone, DateTimeZoneProviders.Tzdb/Bcl, ambiguous/skipped local time resolution |
| `parsing-and-formatting.md` | NodaTime.Text pattern types, ParseResult\<T>, custom pattern strings |
| `bcl-interop.md` | Converting to/from System.DateTime and System.DateTimeOffset at I/O boundaries |
| `testing-with-nodatime.md` | IClock, FakeClock, injecting a clock instead of SystemClock.Instance |

## Scope

NodaTime's core type system (`NodaTime` package): the point-in-time, calendar, zone, and
duration/period types, time zone providers, and text parsing/formatting. Out of scope: non-ISO
calendar systems, serialization-framework-specific converter packages, and time zone database
compiler tooling.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
3.3.4, targeting .NET 8.0 and .NET Standard 2.0.
