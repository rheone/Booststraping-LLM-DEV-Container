# Async Validation

Reach for an async rule whenever validating a property requires I/O — a database uniqueness check,
a call to another service, a file-system check. Mixing sync and async rules in the same validator
is fully supported; the constraint is entirely about how you *invoke* the validator afterward.

## MustAsync

`MustAsync` mirrors `Must` but takes a `Func<TProperty, CancellationToken, Task<bool>>` (or the
whole-object overload `Func<T, TProperty, CancellationToken, Task<bool>>`):

```csharp
public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator(IUserRepository users)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) => !await users.EmailExistsAsync(email, ct))
            .WithMessage("Email is already registered.");
    }
}
```

Always accept and forward the `CancellationToken` FluentValidation passes in — don't swap in
`CancellationToken.None`, which would make the async rule ignore the caller's cancellation
altogether.

## CustomAsync

Use `CustomAsync` (the async counterpart to `Custom`) when the async check needs to add a failure
with a custom property name or add more than one failure — the same reasoning as
`Must` vs. `Custom`, just with an awaited check:

```csharp
RuleFor(x => x.WarehouseId).CustomAsync(async (warehouseId, context, ct) =>
{
    var warehouse = await warehouseService.FindAsync(warehouseId, ct);
    if (warehouse is null)
    {
        context.AddFailure("WarehouseId", "Warehouse does not exist.");
    }
    else if (!warehouse.IsActive)
    {
        context.AddFailure("WarehouseId", "Warehouse is not active.");
    }
});
```

## The invocation rule: ValidateAsync only

A validator containing **any** `MustAsync`, `CustomAsync`, or `WhenAsync` rule must be invoked with
`ValidateAsync`:

```csharp
ValidationResult result = await validator.ValidateAsync(request, cancellationToken);
```

Calling the synchronous `Validate` on such a validator throws
`AsyncValidatorInvokedSynchronouslyException` at run time — FluentValidation refuses to silently
block on the async rule for you. This is the reason ASP.NET Core auto-validation approaches that
call `Validate` synchronously cannot support async rules; see
[aspnetcore-integration.md](aspnetcore-integration.md) for the concrete recommendation this drives
(await `ValidateAsync` explicitly in your endpoint/handler).

## Don't mix sync-looking code that's secretly blocking

A `Must` predicate that internally calls `.Result` or `.Wait()` on a task to fake synchronous
async work is worse than declaring the rule with `MustAsync` in the first place — it removes
FluentValidation's own safety check (the synchronous path looks legitimate to call `Validate` on)
while still carrying deadlock risk on synchronization-context-bound hosts. If a check needs to
await something, declare it as an async rule and invoke the validator with `ValidateAsync` — there
is no correct shortcut around this.
