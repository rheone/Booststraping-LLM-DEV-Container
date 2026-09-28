# Custom Validators

Reach for a custom validator whenever the built-in set (`NotEmpty`, `Matches`, `InclusiveBetween`,
...) can't express the check — a cross-field comparison, a domain-specific format, or a rule
you'll reuse across several validators.

## Must: a predicate against the property value

`Must` takes a `Func<TProperty, bool>` (or a `Func<T, TProperty, bool>` when the check needs the
whole object, not just the property) and fails when it returns `false`:

```csharp
RuleFor(x => x.Password)
    .Must(password => password.Any(char.IsDigit))
    .WithMessage("Password must contain at least one digit.");

RuleFor(x => x.ConfirmPassword)
    .Must((request, confirmPassword) => confirmPassword == request.Password)
    .WithMessage("Passwords do not match.");
```

`Must` is the right first reach for a one-off predicate. It has no access to the validation
context (no way to add more than one error, no way to attach a custom property name to the
failure) — reach for `Custom` when you need either of those.

## Custom: full control over the failure

`Custom` (and its async counterpart, `CustomAsync`) hands you a `ValidationContext<T>` and lets
you add zero, one, or multiple `ValidationFailure` instances directly:

```csharp
RuleFor(x => x.ShippingAddress).Custom((address, context) =>
{
    if (address.Country == "US" && string.IsNullOrEmpty(address.State))
    {
        context.AddFailure("ShippingAddress.State", "State is required for US addresses.");
    }

    if (address.PostalCode.Length > 10)
    {
        context.AddFailure(new ValidationFailure(nameof(address.PostalCode), "Postal code too long.")
        {
            ErrorCode = "PostalCodeTooLong"
        });
    }
});
```

Use `Custom` over `Must` whenever a single property's validation can produce more than one
distinct failure, or when the failure needs a `PropertyName` that doesn't match the `RuleFor`
target (as in the nested `ShippingAddress.State` example above).

## Reusable validators: a custom PropertyValidator<T, TProperty>

When the same non-built-in check appears in more than one validator, extract it into a
`PropertyValidator<T, TProperty>` rather than copy-pasting a `Must`/`Custom` lambda:

```csharp
public sealed class NoConsecutiveWhitespaceValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "NoConsecutiveWhitespaceValidator";

    public override bool IsValid(ValidationContext<T> context, string value) =>
        !System.Text.RegularExpressions.Regex.IsMatch(value, @"\s{2,}");

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "'{PropertyName}' must not contain consecutive whitespace.";
}
```

Expose it as an extension method so it reads like a built-in validator at the call site — this is
the idiomatic FluentValidation shape for a reusable rule:

```csharp
public static class CustomValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> NoConsecutiveWhitespace<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new NoConsecutiveWhitespaceValidator<T>());
}

// usage
RuleFor(x => x.DisplayName).NotEmpty().NoConsecutiveWhitespace();
```

## Choosing between the three

| Need | Reach for |
| --- | --- |
| A single boolean check against one property, used once | `Must` |
| A check that needs to add a custom-named or multiple failures, used once | `Custom` |
| The same check reused across two or more validators/properties | A `PropertyValidator<T, TProperty>` extension method |
