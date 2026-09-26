# Localization

## Culture selection

Every Humanizer method that produces language-specific text accepts an optional trailing
`CultureInfo` parameter. When omitted, Humanizer falls back to
`Thread.CurrentThread.CurrentUICulture` at the moment the call executes.

```csharp
TimeSpan.FromDays(1).Humanize();                                  // uses the current thread's UI culture
TimeSpan.FromDays(1).Humanize(culture: new CultureInfo("fr-FR"));  // explicitly French
```

- **Rely on the current thread culture** for genuinely user-facing text in an application that
  already sets `CurrentUICulture` per request/session to match the actual end user (a typical
  ASP.NET Core app with request localization middleware configured) — the implicit default then
  does the right thing without every call site repeating the culture.
- **Pass an explicit `CultureInfo`** for anything where the output's language must not depend on
  whatever thread happens to run the code: background jobs, logging, generated reports meant for a
  fixed audience, or any code running outside a request pipeline that sets culture per-request.
  Background/worker threads often inherit a default invariant or machine-locale culture rather than
  an end user's actual preference, so relying on the implicit default there produces
  environment-dependent output.

## Installing additional languages

The main `Humanizer` package bundles every supported language's resources. A project referencing
only `Humanizer.Core` (the English-only base package) needs the specific
`Humanizer.Core.<locale>` package added (e.g. `Humanizer.Core.fr` for French) for a given culture's
`CultureInfo` to actually change the output — passing a `CultureInfo` for a language whose resource
package isn't referenced falls back to Humanizer's default (English) behavior rather than throwing.

## Culture-sensitive vs. culture-invariant output

Not every Humanizer operation is language-sensitive to the same degree — `Ordinalize()` and
`ToQuantity()`'s number-vs-word choice depend on culture-specific grammar rules, while
`Truncate()`'s character-counting behavior does not depend on culture at all. When in doubt about
whether a specific method's output changes with culture, check that method's own reference file
(this skill's [SKILL.md](../SKILL.md) routes to each) rather than assuming uniformly that "it takes
a `CultureInfo` parameter" implies the output actually varies for every input.

## Platform globalization data source

Humanizer delegates to the .NET runtime's own globalization data (month/day names, decimal
separators, and other culture-specific text elements it doesn't hard-code itself) rather than
shipping a fully independent locale database — modern .NET builds typically source this from ICU,
while .NET Framework sources it from the Windows NLS APIs. A value formatted through Humanizer for
the same culture can differ subtly between a .NET Framework host and a modern .NET host, or between
operating systems, when the underlying globalization data itself differs — verify culture-specific
output on the actual target runtime/OS combination if an application needs guaranteed exact text,
rather than assuming a passing test on one platform generalizes to every deployment target.
