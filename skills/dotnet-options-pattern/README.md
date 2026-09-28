# Options Pattern

Guidance on `Microsoft.Extensions.Options` for binding configuration sections to strongly-typed
classes: choosing between `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`, and
validating options on startup.

## When to reach for it

- You're defining an options class and binding it to a configuration section.
- You're choosing which options accessor interface to inject, given the consuming service's
  lifetime.
- You're debugging why a configuration change isn't taking effect at runtime, or adding validation
  to an options class.

## Using it

This skill fires automatically when your request involves binding configuration to a class,
choosing an accessor interface, or adding options validation. You can also invoke it directly with
`/dotnet-options-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| Options classes, `Configure<T>`, binding configuration sections | [references/core-concepts.md](references/core-concepts.md) |
| `IOptions<T>` vs. `IOptionsSnapshot<T>` vs. `IOptionsMonitor<T>` lifetimes and reload semantics | [references/accessor-types.md](references/accessor-types.md) |
| Binding and consuming multiple named instances of one options class | [references/named-options.md](references/named-options.md) |
| Data-annotation validation, `.Validate(...)`, `IValidateOptions<T>`, `ValidateOnStart()` | [references/validation.md](references/validation.md) |
| `Configure`/`PostConfigure` ordering, `IOptionsFactory<T>` | [references/advanced-configuration.md](references/advanced-configuration.md) |
| `IOptionsMonitor.OnChange` and what triggers a reload | [references/hot-reload.md](references/hot-reload.md) |
| Testing options consumers and the binding pipeline | [references/testing.md](references/testing.md) |

## Example prompts

- "Bind this Smtp configuration section to a strongly-typed options class with validation."
- "Why doesn't my singleton pick up configuration changes after I edit appsettings.json?"
- "Add data-annotation validation that fails startup if these options are invalid."
