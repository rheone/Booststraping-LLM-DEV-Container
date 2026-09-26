# FluentValidation

Guidance on FluentValidation, the .NET library for expressing validation rules as fluent C# code
instead of data-annotation attributes, covering rule authoring, conditional and async rules,
nested/collection validation, ASP.NET Core wiring, and testing validators.

## When to reach for it

- Writing or reviewing an `AbstractValidator<T>` and its `RuleFor` chains.
- A built-in validator doesn't fit and you need a custom rule (`Must`, `Custom`, or a reusable
  `PropertyValidator`).
- A rule should only apply under certain conditions, or needs to check something asynchronously
  (a database lookup, an API call).
- Validating a nested object or a collection property, or wiring validators into ASP.NET Core
  endpoints.
- Deciding how to test a validator's rule set with `TestValidate`.

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or debugging
FluentValidation validators, or wiring validation into ASP.NET Core. You can also invoke it
directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| AbstractValidator\<T>, RuleFor, built-in validators, ValidationResult/ValidationFailure | [references/core-concepts.md](references/core-concepts.md) |
| Must, Custom/CustomAsync, reusable PropertyValidator\<T,TProperty> | [references/custom-validators.md](references/custom-validators.md) |
| When, Unless, WhenAsync, and sharing a condition across rules | [references/conditional-validation.md](references/conditional-validation.md) |
| MustAsync, CustomAsync, ValidateAsync, mixing sync and async rules | [references/async-validation.md](references/async-validation.md) |
| SetValidator, RuleForEach, validating child objects and collection items | [references/nested-and-collections.md](references/nested-and-collections.md) |
| Manual validation, minimal APIs, endpoint filters, ProblemDetails, deprecated auto-validation | [references/aspnetcore-integration.md](references/aspnetcore-integration.md) |
| Translating validation messages for different cultures | [references/localization.md](references/localization.md) |
| Unit testing a validator's rules with TestValidate | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a FluentValidation validator for this order request DTO."
- "Make this rule only run when the customer type is Business."
- "How do I hook FluentValidation into a minimal API endpoint and return ProblemDetails?"
