# FluentValidation

Guidance on FluentValidation, the strongly-typed validation library for .NET — the routing table
(by task, not FluentValidation version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per FluentValidation version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `AbstractValidator<T>`, `RuleFor`, built-in validators, `ValidationResult`/`ValidationFailure` |
| `custom-validators.md` | `Must`, `Custom`/`CustomAsync`, reusable `PropertyValidator<T,TProperty>` |
| `conditional-validation.md` | `When`, `Unless`, `WhenAsync`, shared conditions across multiple rules |
| `async-validation.md` | `MustAsync`, `CustomAsync`, `ValidateAsync`, mixing sync and async rules |
| `nested-and-collections.md` | `SetValidator`, `RuleForEach`, validating child objects and collection items |
| `aspnetcore-integration.md` | manual validation, minimal APIs, endpoint filters, `ProblemDetails`, deprecated auto-validation |
| `localization.md` | `LanguageManager`, resource-based message translation, per-culture overrides |
| `testing.md` | `TestValidate`, asserting on specific properties and error codes |

## Scope

FluentValidation only — building and running validation rule sets against C# objects. Out of
scope: data annotation attributes, domain invariant enforcement inside entities, and client-side
validation (see [SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline (e.g. the v12 minimum-.NET-8 target,
the v11.1 `Results.ValidationProblem` helper); version is not the file-splitting axis for this
skill (see [SKILL.md](SKILL.md)).
