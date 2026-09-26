---
name: dotnet-options-pattern
description: Guidance on the Microsoft.Extensions.Options pattern (part of the .NET runtime libraries, verified current against .NET 10) for binding configuration to strongly-typed classes — IOptions<T> vs IOptionsSnapshot<T> vs IOptionsMonitor<T> and their singleton/scoped/live-reload semantics, Configure<T>/PostConfigure<T> ordering, named options, IValidateOptions<T> and data-annotation validation with ValidateOnStart, IOptionsFactory<T> for advanced instance-creation scenarios, and IOptionsMonitor.OnChange hot-reload behavior. Use when writing or reviewing code that reads configuration through options classes, choosing which options accessor interface to inject, debugging why a configuration change isn't taking effect, or adding validation to an options class.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Options Pattern

Guidance on `Microsoft.Extensions.Options`, the .NET pattern for binding configuration sections to
strongly-typed classes and consuming them through DI. Organized by task, not by .NET version — the
core API surface has been stable across recent releases; each reference file notes a
version-specific fact inline where one applies.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Defining an options class and binding it to configuration with `Configure<T>` | [references/core-concepts.md](references/core-concepts.md) |
| Choosing between `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` | [references/accessor-types.md](references/accessor-types.md) |
| Binding multiple named instances of the same options class | [references/named-options.md](references/named-options.md) |
| Adding validation with data annotations, `.Validate(...)`, or `IValidateOptions<T>`, or failing fast on startup | [references/validation.md](references/validation.md) |
| Understanding `Configure`/`PostConfigure` ordering, or customizing instance creation with `IOptionsFactory<T>` | [references/advanced-configuration.md](references/advanced-configuration.md) |
| Reacting to a configuration change at runtime with `IOptionsMonitor.OnChange` | [references/hot-reload.md](references/hot-reload.md) |
| Unit or integration testing code that consumes options | [references/testing.md](references/testing.md) |

## Quick start

```csharp
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 25;
}

// Program.cs
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Consumer
public sealed class EmailSender(IOptions<SmtpOptions> options)
{
    private readonly SmtpOptions _options = options.Value;
}
```

The single most common mistake: injecting `IOptions<T>` into a singleton and then expecting a
configuration file change to take effect — `IOptions<T>.Value` is computed once and cached for the
application's lifetime. Reach for `IOptionsMonitor<T>` when live updates matter. See
[references/accessor-types.md](references/accessor-types.md).

## Out of scope

- Configuration providers themselves (JSON files, environment variables, Azure App Configuration,
  command-line arguments) — this skill covers binding an already-loaded `IConfiguration` to a
  strongly-typed class, not how configuration values get loaded into `IConfiguration` in the first
  place.
- Secrets management (user secrets, key vaults) — orthogonal to the binding/validation/reload
  mechanics covered here.
