# Localization

FluentValidation's built-in validators ship default error messages translated into many
languages already; the library picks the message for the current thread's culture automatically,
with no configuration required for the common case.

## How the default messages are selected

Every built-in validator's default message comes from `LanguageManager`, which resolves the
message template based on `CultureInfo.CurrentUICulture` at the moment validation runs. Setting
the thread's culture (e.g. via ASP.NET Core's request localization middleware, which sets
`CurrentUICulture` per request from the `Accept-Language` header) is sufficient to get translated
built-in messages with no FluentValidation-specific configuration:

```csharp
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedUICultures("en-US", "fr-FR", "de-DE"));
```

## Overriding or adding a message for a specific culture

`WithMessage` accepts either a fixed string or a delegate that resolves the message per validation
call — use the delegate form to pull from your own resource files (`.resx`) for custom validators
or custom wording on a built-in rule:

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage(_ => Strings.EmailRequired); // resx-generated static property, culture-resolved automatically
```

`Strings.EmailRequired` here is a standard .NET resource-generated accessor — it already resolves
against `CurrentUICulture` the same way any other `.resx`-backed string does; nothing
FluentValidation-specific is needed beyond calling it from the `WithMessage` delegate.

## Disabling localization entirely

Force every message to a single culture's translation regardless of the running thread's culture
by setting `ValidatorOptions.Global.LanguageManager.Enabled = false` (falls back to English) or by
setting a fixed culture on the language manager rather than following the ambient thread culture.
Reach for this only when a service intentionally standardizes all outward messages on one language
regardless of caller locale (e.g. an internal API consumed only by one team) — for anything
user-facing, following the ambient culture is almost always the right default.

## Message placeholders stay language-independent

Placeholders inside a message template (`{PropertyName}`, `{PropertyValue}`, `{ComparisonValue}`,
and any custom placeholder added via `context.MessageFormatter.AppendArgument(...)` in a `Custom`
rule) get substituted after the template's own translation is chosen — you do not need to
duplicate placeholder logic per language when adding a translated message.
