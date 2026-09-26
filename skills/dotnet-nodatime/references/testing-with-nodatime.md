# Testing code that uses NodaTime

## How to test it

Code that reads "the current time" is only deterministically testable if it gets that value from an
injected seam rather than a static call. NodaTime provides that seam as `IClock`.

- **Inject `IClock`, never call `SystemClock.Instance` from inside logic you intend to unit test.**
  Accept `IClock` as a constructor parameter and call `clock.GetCurrentInstant()` where the code
  needs "now." Production composition passes `SystemClock.Instance`; a test passes a fake.
- **Use `NodaTime.Testing.FakeClock`** (from the separate `NodaTime.Testing` package) to control
  "now" precisely in a test: construct it with a fixed `Instant`, optionally advance it explicitly
  between assertions with `clock.AdvanceInstant(duration)` or `clock.AdvanceSeconds(n)`, and never
  rely on wall-clock time passing during test execution.

```csharp
public sealed class ReminderService
{
    private readonly IClock _clock;

    public ReminderService(IClock clock) => _clock = clock;

    public bool IsOverdue(Instant dueAt) => _clock.GetCurrentInstant() > dueAt;
}
```

```csharp
[Fact]
public void IsOverdue_returns_true_after_due_instant()
{
    var fixedNow = Instant.FromUtc(2026, 6, 1, 12, 0);
    var clock = new FakeClock(fixedNow);
    var service = new ReminderService(clock);

    clock.AdvanceSeconds(1);

    Assert.True(service.IsOverdue(fixedNow));
}
```

- **Assert on NodaTime values directly** rather than converting to `DateTime` first for comparison —
  the NodaTime types implement value equality and `IComparable<T>`, so
  `Assert.Equal(expectedInstant, actualInstant)` and `Assert.True(a < b)` work without a conversion
  step that could itself hide a bug.
- **Test DST edge cases with explicit `LocalDateTime` values known to fall in a gap or overlap** for
  the zone under test, rather than trusting that "some date in March/November" exercises the
  transition — tz database transition dates and times vary by zone and by year, so pick a date the
  test can name and verify against the zone's actual rules for the years the test claims to cover.
- **Avoid asserting on formatted text produced by a culture-sensitive pattern** unless the test
  fixes both the pattern's culture and the exact expected string — formatting output otherwise
  becomes an environment-dependent assertion.

## Common scenarios

### Testing a method that schedules something relative to "now"

Inject `IClock`, fix it to a known `Instant` in the test, and assert the computed result against a
value derived from that same fixed instant — never against `DateTime.Now`/`Instant.FromDateTimeUtc
(DateTime.UtcNow)` computed independently inside the test, which reintroduces the exact race the
fake clock exists to remove.

### Testing a time-zone conversion across a DST boundary

Pick a `LocalDateTime` immediately before and immediately after a known transition for the zone
under test, convert both with `zone.AtStrictly(...)`, and assert on the resulting `Instant`/offset
difference — this catches an accidental use of `AtLeniently` or a hardcoded offset standing in for
a real zone lookup.

### Testing round-trip conversion at a BCL interop boundary

Construct the NodaTime value, convert to the BCL type used at the boundary, convert back, and
assert the round-tripped NodaTime value equals the original — this is the cheapest way to catch a
lossy conversion (e.g. losing the zone identity, discarding sub-second precision) before it reaches
production data.
