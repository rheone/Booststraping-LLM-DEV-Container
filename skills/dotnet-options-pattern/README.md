# Options Pattern

Guidance on `Microsoft.Extensions.Options` — the routing table (by task) is in
[SKILL.md](SKILL.md).

**`references/`** — one file per topic

| File | Covers |
| --- | --- |
| `core-concepts.md` | Options classes, `Configure<T>`, binding configuration sections |
| `accessor-types.md` | `IOptions<T>` vs `IOptionsSnapshot<T>` vs `IOptionsMonitor<T>` lifetimes and reload semantics |
| `named-options.md` | Binding and consuming multiple named instances of one options class |
| `validation.md` | Data-annotation validation, `.Validate(...)`, `IValidateOptions<T>`, `ValidateOnStart()` |
| `advanced-configuration.md` | `Configure`/`PostConfigure` ordering, `IOptionsFactory<T>` |
| `hot-reload.md` | `IOptionsMonitor.OnChange`, what triggers a reload |
| `testing.md` | Testing options consumers, faking `IOptionsMonitor<T>`, integration-testing the binding pipeline |

## Scope

Binding configuration to strongly-typed classes and consuming them through DI. Out of scope:
configuration providers themselves (how values get into `IConfiguration`) and secrets management —
neither is part of the binding/validation/reload mechanics documented here.

Each reference file notes a version-specific fact inline where one applies; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
