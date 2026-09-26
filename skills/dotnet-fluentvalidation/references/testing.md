# Testing

FluentValidation ships its own testing extensions (`FluentValidation.TestHelper`) purpose-built
for asserting against a `ValidationResult` without hand-rolling LINQ queries over `Errors` in every
test.

## TestValidate: the standard entry point

`TestValidate` (an extension method on any `IValidator<T>`) runs validation and returns a
`TestValidationResult<T>` with fluent assertion methods:

```csharp
[Fact]
public void Validate_EmptyCustomerId_HasError()
{
    var validator = new CreateOrderRequestValidator();
    var request = new CreateOrderRequest { CustomerId = "", Total = 10m };

    TestValidationResult<CreateOrderRequest> result = validator.TestValidate(request);

    result.ShouldHaveValidationErrorFor(x => x.CustomerId);
}

[Fact]
public void Validate_ValidRequest_HasNoErrors()
{
    var validator = new CreateOrderRequestValidator();
    var request = new CreateOrderRequest { CustomerId = "cust-1", Total = 10m };

    TestValidationResult<CreateOrderRequest> result = validator.TestValidate(request);

    result.ShouldNotHaveAnyValidationErrors();
}
```

`ShouldHaveValidationErrorFor`/`ShouldNotHaveValidationErrorFor` target a single property via the
same expression syntax as `RuleFor`, so a test reads as directly as the rule it's checking.

## Asserting on the specific failure, not just its presence

Chain onto `ShouldHaveValidationErrorFor` to assert the failure's message, error code, or severity
rather than only that *a* failure exists — this matters whenever a property has more than one
rule attached, since "has an error" alone doesn't tell you *which* rule fired:

```csharp
result.ShouldHaveValidationErrorFor(x => x.Total)
    .WithErrorMessage("'Total' must be greater than '0'.")
    .WithErrorCode("GreaterThanValidator");
```

## Testing conditional rules: cover both branches

A rule gated by `When`/`Unless` needs at least two test cases — one where the condition is true
(the rule should fire) and one where it's false (the rule should not fire), otherwise a broken
condition (e.g. an inverted boolean) passes silently:

```csharp
[Fact]
public void Validate_HasPromotion_RequiresPromoCode() =>
    new OrderValidator().TestValidate(new Order { HasPromotion = true, PromoCode = "" })
        .ShouldHaveValidationErrorFor(x => x.PromoCode);

[Fact]
public void Validate_NoPromotion_PromoCodeNotRequired() =>
    new OrderValidator().TestValidate(new Order { HasPromotion = false, PromoCode = "" })
        .ShouldNotHaveValidationErrorFor(x => x.PromoCode);
```

## Testing async rules

`TestValidateAsync` mirrors `TestValidate` for validators containing `MustAsync`/`CustomAsync`
rules — await it the same way you'd await `ValidateAsync` in production code, and provide a real
or fake implementation of whatever the async rule depends on (a repository, an external service)
rather than trying to force the async rule to run synchronously:

```csharp
[Fact]
public async Task Validate_DuplicateEmail_HasError()
{
    var users = Substitute.For<IUserRepository>();
    users.EmailExistsAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);
    var validator = new CreateUserValidator(users);

    var result = await validator.TestValidateAsync(new CreateUserRequest { Email = "taken@example.com" });

    result.ShouldHaveValidationErrorFor(x => x.Email);
}
```

## Testing nested and collection rules

Target a nested property or a collection element with the same expression syntax `RuleFor`/
`RuleForEach` used to declare the rule:

```csharp
result.ShouldHaveValidationErrorFor(x => x.ShippingAddress.Street);
result.ShouldHaveValidationErrorFor("Lines[0].Quantity"); // string form for indexed collection paths
```

The string-path form is necessary for indexed collection elements, since a lambda expression
cannot express a specific runtime index (`Lines[0]`) the way it can a fixed property path.
