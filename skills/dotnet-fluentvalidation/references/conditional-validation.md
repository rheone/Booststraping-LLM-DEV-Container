# Conditional Validation

Use `When`/`Unless` to make a rule apply only under a condition evaluated against the object being
validated, rather than encoding the condition inside a `Must`/`Custom` predicate every time.

## When and Unless on a single rule

```csharp
RuleFor(x => x.PromoCode)
    .NotEmpty()
    .When(x => x.HasPromotion);

RuleFor(x => x.CompanyName)
    .NotEmpty()
    .Unless(x => x.CustomerType == CustomerType.Individual);
```

`When`/`Unless` receive the whole `T`, not just the property under validation — the condition can
reference any field on the object, which is the common case (a field is required only when a
sibling field has a particular value).

## Applying a condition to multiple rules at once

Wrap several `RuleFor` calls in a shared `When` block instead of repeating the same condition on
each:

```csharp
When(x => x.ShippingMethod == ShippingMethod.International, () =>
{
    RuleFor(x => x.CustomsDeclaration).NotEmpty();
    RuleFor(x => x.DestinationCountry).NotEmpty();
});
```

Every rule declared inside the block only runs when the condition is true. Nesting these blocks is
supported but hurts readability past one level — prefer flattening the condition (combine both
checks into one boolean expression) over nesting two `When` blocks.

## ApplyConditionTo: controlling which chained rules the condition covers

By default, `When`/`Unless` attached at the end of a chain applies to every validator in that
chain. Pass `ApplyConditionTo.CurrentValidator` to scope the condition to only the validator it's
chained directly onto, leaving earlier validators in the same chain unconditional:

```csharp
RuleFor(x => x.DiscountCode)
    .NotEmpty()
    .Length(4, 10).When(x => x.HasPromotion, ApplyConditionTo.CurrentValidator);
```

Here `NotEmpty()` always runs; `Length(4, 10)` only runs when `HasPromotion` is true. Without the
second argument, `When` would apply to `NotEmpty()` too — a common source of "why did this
required-field rule stop firing" bugs after adding a condition to a chain that already had
unconditional rules on it.

## WhenAsync/UnlessAsync

When the condition itself needs to await something (a database lookup, an external check), use the
async variants — do not call `.Result`/`.GetAwaiter().GetResult()` on a `Task<bool>` inside a
synchronous `When` predicate, which risks a deadlock in a synchronization-context-bound host and
defeats the purpose of validating asynchronously in the first place:

```csharp
RuleFor(x => x.CouponCode)
    .MustAsync(async (code, ct) => await couponService.IsValidAsync(code, ct))
    .WhenAsync(async (request, ct) => await featureFlags.IsEnabledAsync("coupons", ct));
```

A validator with any `WhenAsync`/`MustAsync`/`CustomAsync` rule must be invoked with
`ValidateAsync`, never the synchronous `Validate` — see
[async-validation.md](async-validation.md) for the full rule.
