# Core Concepts

FluentValidation expresses validation rules as C# code against a strongly-typed model, instead of
decorating the model's properties with attributes. You write one validator class per model, chain
rules onto each property with `RuleFor`, and run the validator explicitly to get back a structured
result.

## AbstractValidator<T>

Every validator inherits from `AbstractValidator<T>` and declares its rules in the constructor:

```csharp
public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).InclusiveBetween(0, 120);
    }
}
```

`RuleFor(x => x.Property)` returns a rule builder; every call chained onto it (`.NotEmpty()`,
`.EmailAddress()`, `.WithMessage(...)`) adds a validator or customizes the previous one. Rules on
the same property compose left to right — by default, FluentValidation runs **all** rules for all
properties and collects every failure, rather than stopping at the first one (see the
`CascadeMode` option below to change this per rule or per validator).

## Built-in validators

The most commonly used built-in validators, all chainable off `RuleFor`:

| Validator | Checks |
| --- | --- |
| `NotNull()` / `NotEmpty()` | Not null (and, for `NotEmpty`, not the default value / empty string / empty collection) |
| `NotEqual(value)` / `Equal(value)` | Value inequality/equality |
| `Length(min, max)` / `MaximumLength(n)` / `MinimumLength(n)` | String length |
| `Matches(regex)` | Regular expression match |
| `EmailAddress()` | Valid email address shape |
| `GreaterThan`/`GreaterThanOrEqualTo`/`LessThan`/`LessThanOrEqualTo` | Numeric/comparable comparisons |
| `InclusiveBetween(from, to)` / `ExclusiveBetween(from, to)` | Range checks |
| `CreditCardNumber()` | Luhn-valid card number shape |
| `Enum<TEnum>()` | Value is a defined member of the enum |

Every built-in validator accepts `.WithMessage(...)` to override its default error message and
`.WithErrorCode(...)` to attach a machine-readable code distinct from the display message —
prefer setting an error code whenever a caller (a UI, another service) needs to branch on which
rule failed without string-matching the message.

## Running validation and reading the result

```csharp
var validator = new CustomerValidator();
ValidationResult result = validator.Validate(customer);

result.IsValid;                          // bool
result.Errors;                           // IList<ValidationFailure>
result.Errors[0].PropertyName;           // e.g. "Email"
result.Errors[0].ErrorMessage;           // e.g. "'Email' is not a valid email address."
result.Errors[0].ErrorCode;              // e.g. "EmailValidator" unless overridden
```

`Validate` throws nothing on failure — always check `IsValid` (or call `ValidateAndThrow`, which
throws `ValidationException` on failure, for call sites that treat invalid input as exceptional
rather than an expected outcome to branch on).

## CascadeMode: stop-on-first vs run-all

By default (`CascadeMode.Continue`), every rule attached to a property runs even after an earlier
one on the same property fails. Set `CascadeMode.Stop` on a specific rule chain to skip the
remaining rules for that property once one fails — useful when a later rule would throw or produce
a meaningless message against already-invalid data:

```csharp
RuleFor(x => x.Age)
    .Cascade(CascadeMode.Stop)
    .NotNull()
    .GreaterThan(0); // skipped if Age is null
```

Set it validator-wide via the constructor when most rules in the validator should behave this way:

```csharp
public CustomerValidator()
{
    RuleLevelCascadeMode = CascadeMode.Stop;
    // ...
}
```

## Registering validators for dependency injection

The `FluentValidation.DependencyInjectionExtensions` package (matching version to the core
package) adds `AddValidatorsFromAssemblyContaining<T>()`/`AddValidatorsFromAssembly(assembly)` for
scanning and registering every `AbstractValidator<T>` in an assembly as `IValidator<T>`:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CustomerValidator>();
```

Resolve `IValidator<Customer>` (not the concrete class) wherever a class needs to validate a
`Customer` — this keeps call sites decoupled from which validator implementation is registered.
