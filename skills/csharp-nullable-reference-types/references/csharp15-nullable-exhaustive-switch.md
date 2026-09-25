# Nullable-Governed Switch Exhaustiveness (C# 15 / .NET 11)

**RC caveat:** .NET 11 is at Release Candidate 1 (with a go-live license) as of September 2026;
GA is expected November 10, 2026. The behavior below is documented on the current C# language
reference as of this RC and is not expected to change before GA, but treat it with the same
caution as any pre-GA feature.

C# 15 lets a `switch` over a closed set of subtypes be treated as exhaustive without a discard
(`_`) arm, once every direct case is handled. The nullable-reference-types-specific piece of that:
when the switch's governing expression has a nullable type, the compiler now treats `null` as one
more case the switch must explicitly cover for exhaustiveness — omitting a `null` arm produces a
"not handled" warning even if every non-null case is present, and this applies to both a nullable
reference type and a value type lifted to nullable through the same governing-type mechanism.

## Syntax

```csharp
public closed record class JobStatus;
public record class Queued : JobStatus;
public record class Running(int PercentComplete) : JobStatus;
public record class Completed(TimeSpan Elapsed) : JobStatus;

public static string Describe(JobStatus? status) => status switch
{
    null => "unknown",                              // required for exhaustiveness over JobStatus?
    Queued => "waiting to start",
    Running(var percent) => $"{percent}% complete",
    Completed(var elapsed) => $"finished in {elapsed.TotalSeconds:F1}s",
    // no discard arm needed: every direct case of JobStatus is handled, and null is handled
};
```

## Basic use case: omitting `null` produces a warning, not silent gap-filling

```csharp
public static string DescribeIncomplete(JobStatus? status) => status switch
{
    Queued => "waiting to start",
    Running(var percent) => $"{percent}% complete",
    Completed(var elapsed) => $"finished in {elapsed.TotalSeconds:F1}s",
    // warning: the pattern 'null' is not covered -- status is JobStatus?, so null is a
    // reachable input the switch must account for, exhaustive-over-subtypes or not
};
```

This is a direct consequence of the switch's governing type being nullable, not a new rule specific
to any one hierarchy shape — a plain enum switch over a nullable enum type has needed a `null` arm
(or a discard) for exhaustiveness since exhaustiveness warnings existed; what's new in this tier is
that the closed-set-of-subtypes exhaustiveness check extends the same nullable-aware rule to a
class hierarchy that previously always needed a discard arm regardless of nullability.

## Advanced use case: handling `null` distinctly from every concrete case

```csharp
public static decimal EstimateProgress(JobStatus? status) => status switch
{
    null => 0m,                          // no job at all
    Queued => 0m,
    Running(var percent) => percent / 100m,
    Completed => 1m,
    _ => 0m, // still needed here: JobStatus is closed only within its declaring assembly,
             // so a switch in a different assembly can't assume the case list is complete
};
```

Exhaustiveness-without-a-discard only applies inside the assembly that declares the closed type's
direct descendants; outside it, a consumer's switch still needs a discard arm regardless of how the
`null` case is handled, since that consumer has no compiler-verified guarantee the case list won't
grow.

## Requirements and restrictions

- The nullable-exhaustiveness rule triggers based on the switch's governing expression being a
  nullable type — it has nothing to do with `closed` specifically, but `closed` types are the main
  place a missing discard arm (and therefore a missing `null` arm) becomes load-bearing rather than
  boilerplate.
- Still requires the switch's own type to actually be nullable (`JobStatus?`, not `JobStatus`) —
  switching over a non-nullable governing type never needs a `null` arm, since `null` isn't a
  reachable value.

## Fallback

Below C# 15, closed-hierarchy exhaustiveness doesn't exist at all — every switch over a class
hierarchy needs a discard (`_`) arm regardless of nullability, and a missing `null` case on a
nullable governing type still warns using the ordinary non-exhaustive-switch diagnostic that has
existed since exhaustiveness checking was introduced, not this closed-hierarchy-specific one. The
narrowing and `!`-suppression patterns in
[csharp8-nullable-reference-types.md](csharp8-nullable-reference-types.md) remain the applicable
baseline for handling `null` inside any individual switch arm.
