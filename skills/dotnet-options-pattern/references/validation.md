# Validating Options

Options validation catches misconfiguration — a missing required setting, an out-of-range value —
at a well-defined point rather than letting a bad value propagate into whatever code first happens
to read it.

## Data-annotation validation

`ValidateDataAnnotations()` runs the options class's `System.ComponentModel.DataAnnotations`
attributes (`[Required]`, `[Range]`, `[Url]`, and so on) as validation:

```csharp
public sealed class SmtpOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 25;
}

builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations();
```

`AddOptions<TOptions>()` returns an `OptionsBuilder<TOptions>` — the fluent surface that
`.Bind(...)`, `.ValidateDataAnnotations()`, `.Validate(...)`, and `.ValidateOnStart()` all chain off
of. It is equivalent to (and composes with) the plain `Configure<TOptions>` calls covered in
[core-concepts.md](core-concepts.md) — both end up registering the same underlying options
pipeline.

## Custom validation with `.Validate(...)`

`.Validate(predicate, failureMessage)` adds an inline validation rule that doesn't need a full
`IValidateOptions<TOptions>` implementation:

```csharp
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .Validate(o => !o.UseSsl || o.Port != 25, "Port 25 does not support SSL; use 465 or 587.")
    .ValidateOnStart();
```

## IValidateOptions\<TOptions\> for reusable or DI-dependent validation

Implement `IValidateOptions<TOptions>` and register it in DI when validation needs injected
dependencies (checking a value against a database or another service) or when the same validation
logic needs to run for options bound in more than one place:

```csharp
public sealed class SmtpOptionsValidator(IHostEnvironment environment) : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        if (environment.IsProduction() && options.UseSsl is false)
        {
            return ValidateOptionsResult.Fail("SMTP must use SSL in production.");
        }

        return ValidateOptionsResult.Success;
    }
}

builder.Services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
```

The `name` parameter lets one validator instance handle named options selectively — return
`ValidateOptionsResult.Skip()` for a name the validator doesn't apply to, since returning `Success`
for every name means the validator claims responsibility for names it never actually checked.

## When validation actually runs

By default, validation runs **lazily** — the first time `.Value` (or `.CurrentValue`, or `.Get(name)`)
is accessed on any accessor for that options type. A configuration error stays silent until
something reads the option, which can be well after startup.

`.ValidateOnStart()` (chained off `OptionsBuilder<TOptions>`) changes this: it registers an
`IHostedService` that resolves and validates the options during application startup, so a bad
configuration value fails fast with a startup exception instead of surfacing later as a runtime
failure in unrelated code:

```csharp
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Reach for `.ValidateOnStart()` on any options class whose misconfiguration you'd rather see as a
deployment failure than a production incident hours later.
