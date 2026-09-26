# Diagnostic Descriptors

A `DiagnosticDescriptor` is the immutable metadata behind every diagnostic an analyzer reports —
its id, message, category, severity, and whether it's on by default. You declare one per distinct
rule and expose all of them through `SupportedDiagnostics`.

## Anatomy

```csharp
private static readonly DiagnosticDescriptor Rule = new(
    id: "MYAN0001",
    title: "Avoid Console.WriteLine",
    messageFormat: "Use the injected ILogger instead of Console.WriteLine in '{0}'",
    category: "Design",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Direct console output bypasses structured logging and log level filtering.",
    helpLinkUri: "https://example.com/rules/MYAN0001");
```

- **`id`** — a short, stable string, conventionally a prefix plus a zero-padded number
  (`MYAN0001`). Never reuse an id for a semantically different rule once shipped; consumers
  reference ids in `.editorconfig` severity overrides and suppression comments.
- **`messageFormat`** — a composite format string; `{0}`, `{1}`, ... are filled from the arguments
  passed to `Diagnostic.Create`. Keep it specific enough to act on without opening the rule's docs.
- **`category`** — a free-text grouping (`"Design"`, `"Performance"`, `"Naming"`) that IDEs group
  rule-configuration UI by. Pick one consistent taxonomy across all of an analyzer package's rules.
- **`defaultSeverity`** vs. **`isEnabledByDefault`** — severity is what a diagnostic reports *as*
  when active (`Error`, `Warning`, `Info`, `Hidden`); `isEnabledByDefault: false` ships a rule a
  consumer must opt into (via `.editorconfig` or a ruleset) rather than one active out of the box.
- **`description`** and **`helpLinkUri`** — optional, but every non-trivial rule benefits from both:
  `description` appears in some IDE tooltips beyond the one-line `messageFormat`, and `helpLinkUri`
  gives a documentation destination for the diagnostic's built-in "learn more" affordance.

## `SupportedDiagnostics`

```csharp
public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule1, Rule2];
```

This must list every descriptor the analyzer can ever report — Roslyn uses it to validate that
`ReportDiagnostic` calls only ever report a declared descriptor (an undeclared one triggers an
analyzer exception, surfaced as `AD0001` to consumers) and to drive `.editorconfig`/ruleset
configuration UI. Return a fixed `ImmutableArray`, not a freshly allocated one per access — this
property is read repeatedly.

## Localizable strings

For an analyzer shipped broadly (a public NuGet package, a multi-team internal package), wrap
`title`/`messageFormat`/`description` in `LocalizableResourceString` backed by a `.resx` file
instead of plain string literals, so consumers running a localized IDE see translated diagnostic
text. For an analyzer scoped to one codebase or team, plain string literals are the pragmatic
default — the localization machinery is overhead with no payoff until there's an actual
multi-language consumer base.

## Severity and configurability from the consumer side

Whatever `defaultSeverity`/`isEnabledByDefault` you pick, a consumer can always override severity
per-project via `.editorconfig` (`dotnet_diagnostic.MYAN0001.severity = error`) or suppress a
specific instance with `#pragma warning disable MYAN0001` / `[SuppressMessage]`. Don't try to
prevent overriding from inside the analyzer itself — there is no supported mechanism to force a
diagnostic to ignore consumer-side severity configuration, and fighting that model produces a
confusing analyzer.
