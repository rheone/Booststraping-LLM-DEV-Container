# BCL interop

Every boundary the process doesn't fully control — a database column, a JSON payload, a third-party
API, `DateTime.UtcNow` itself — still speaks `System.DateTime`/`System.DateTimeOffset`. Convert at
the boundary, immediately, and keep NodaTime types everywhere else in the call graph.

## `Instant` <-> `DateTime`/`DateTimeOffset`

```csharp
Instant instant = Instant.FromDateTimeUtc(someUtcDateTime);
DateTime utc = instant.ToDateTimeUtc();

Instant fromOffset = Instant.FromDateTimeOffset(someDateTimeOffset);
DateTimeOffset offsetValue = instant.ToDateTimeOffset();
```

`Instant.FromDateTimeUtc` throws `ArgumentException` if the incoming `DateTime.Kind` is not `Utc` —
this is intentional: a `DateTime` with `Kind == Local` or `Kind == Unspecified` does not carry
enough information to safely convert to an unambiguous instant without an implicit, easy-to-miss
assumption about which zone "local" or "unspecified" means. Normalize (or reject) non-UTC
`DateTime` values before this call rather than working around the exception.

## `LocalDateTime` <-> `DateTime`

```csharp
LocalDateTime local = LocalDateTime.FromDateTime(someDateTimeWithUnspecifiedKind);
DateTime roundTripped = local.ToDateTimeUnspecified();
```

`ToDateTimeUnspecified()` produces a `DateTime` with `Kind == Unspecified` — deliberately, because a
`LocalDateTime` has no zone attached and `Unspecified` is the only `DateTime.Kind` that doesn't
assert an incorrect one. Do not follow this call with code that treats the result as `Utc` or
`Local`; that reintroduces the exact ambiguity NodaTime's type separated out.

## `LocalDate` <-> `DateTime`

```csharp
LocalDate date = LocalDate.FromDateTime(someDateTime); // time-of-day component is discarded
DateTime midnight = date.ToDateTimeUnspecified(); // time-of-day is always midnight
```

## Database columns

Most .NET data access libraries and providers read/write `DateTime`/`DateTimeOffset` natively.
Store an `Instant` as a UTC `DateTime`/`DateTimeOffset` column (via `ToDateTimeUtc()`/
`ToDateTimeOffset()`) when the column represents a moment in time, and store a `LocalDate`/
`LocalDateTime` as its unspecified-kind BCL equivalent when the column represents a calendar value
with no zone semantics (a birth date, a scheduled wall-clock appointment time before the zone is
resolved). Convert back through the matching `From*` factory method on read — don't store the
`ToString()` text representation as a substitute for a real conversion, since round-tripping through
text depends on picking a pattern that preserves everything the column needs.

## JSON and other wire formats

The core `NodaTime` package's types do not serialize to JSON on their own; a serializer needs an
explicit converter for each NodaTime type it will encounter (typically supplied by a separate
serialization-integration package for the JSON library in use). At a wire boundary without such a
converter configured, convert manually to the BCL equivalent (`Instant.ToDateTimeOffset()`,
`LocalDate.ToDateTimeUnspecified()`) before serializing, and convert back immediately after
deserializing — treat the BCL value as transport-only, never as the type carried through
application logic.

## The one-way information loss to watch for

Converting a `ZonedDateTime` to `DateTimeOffset` keeps the offset but drops the time zone identity
— the result can no longer answer "what happens after the next DST transition in this zone."
Converting it back to a NodaTime type from that `DateTimeOffset` alone recovers an `OffsetDateTime`
at best, not the original `ZonedDateTime`; if the zone identity matters downstream, pass or store
the zone identifier (e.g. `"America/New_York"`) alongside the BCL value rather than relying on the
offset to reconstruct it.
