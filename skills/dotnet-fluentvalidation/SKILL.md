---
name: dotnet-fluentvalidation
description: Guidance on the FluentValidation NuGet package (verified current release 12.1.1, Apache-2.0, targets .NET 8.0+) for building strongly-typed validation rules in C#/.NET — AbstractValidator<T>, RuleFor chains and built-in validators, custom validators (Custom/PredicateValidator/reusable validators), conditional rules (When/Unless/WhenAsync), async validation (MustAsync/CustomAsync), validating nested objects and collections (SetValidator/RuleForEach), ASP.NET Core integration (manual and minimal-API validation, endpoint filters, ProblemDetails responses, and why the FluentValidation.AspNetCore auto-validation package is deprecated), localization of error messages, and testing validators with TestValidate. Use when writing or reviewing FluentValidation validators, wiring validation into ASP.NET Core endpoints, deciding between built-in and custom validators, debugging conditional/async rule behavior, or testing a validator's rule set.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# FluentValidation

Guidance on FluentValidation, the strongly-typed validation library for .NET that expresses
validation rules as fluent C# code instead of attributes. Organized by task, not by version — the
core rule-building API (`AbstractValidator<T>`, `RuleFor`) has been stable across major versions;
each reference file notes a version-introduced fact inline where it matters (e.g. the v12
`.NET 8+`-only target, the v11.1 `ToDictionary()` helper).

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Learning `AbstractValidator<T>`, `RuleFor`, and the built-in validators (`NotEmpty`, `Length`, `Matches`, etc.) | [references/core-concepts.md](references/core-concepts.md) |
| Writing a validator that doesn't fit a built-in rule (`Must`, `Custom`, a reusable `PropertyValidator`) | [references/custom-validators.md](references/custom-validators.md) |
| Making a rule apply only in certain conditions (`When`/`Unless`/`WhenAsync`) | [references/conditional-validation.md](references/conditional-validation.md) |
| Validating against a database, an API, or any other I/O-bound check | [references/async-validation.md](references/async-validation.md) |
| Validating a nested object or a collection property | [references/nested-and-collections.md](references/nested-and-collections.md) |
| Wiring validators into ASP.NET Core (minimal APIs, MVC, endpoint filters, ProblemDetails) | [references/aspnetcore-integration.md](references/aspnetcore-integration.md) |
| Translating validation messages for different cultures | [references/localization.md](references/localization.md) |
| Unit testing a validator's rules | [references/testing.md](references/testing.md) |

## Quick start

A minimal validator and manual invocation, current API (v12.1.1):

```csharp
public sealed class CreateOrderRequest
{
    public string CustomerId { get; set; } = "";
    public decimal Total { get; set; }
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Total).GreaterThan(0);
    }
}

// Invocation
var validator = new CreateOrderRequestValidator();
ValidationResult result = validator.Validate(new CreateOrderRequest { Total = -5 });

if (!result.IsValid)
{
    foreach (ValidationFailure failure in result.Errors)
    {
        Console.WriteLine($"{failure.PropertyName}: {failure.ErrorMessage}");
    }
}
```

The single most common miss: assuming `FluentValidation.AspNetCore`'s automatic MVC validation
still applies to minimal APIs, or reaching for it at all in a new project — it's deprecated. See
[references/aspnetcore-integration.md](references/aspnetcore-integration.md) for the current,
manual-invocation approach the library itself now recommends.

## Out of scope

- Data annotation attributes (`[Required]`, `[Range]`) and ASP.NET Core's built-in model-binding
  validation — a different validation mechanism with its own attribute-based API, not part of
  FluentValidation's surface.
- Domain-level invariant enforcement inside entities/aggregates (guard clauses, value-object
  constructors) — FluentValidation targets input/DTO validation at a boundary, not enforcing
  invariants a domain type should never be able to violate in the first place.
- Client-side/JavaScript validation — FluentValidation is a server-side .NET library only.
