# Testing Specifications

## How to test it

A specification's entire contract is "does this candidate satisfy the rule" — that makes it one of
the simplest things in a codebase to unit test: construct a candidate, construct the specification,
assert on the boolean (or on the compiled expression's result).

- **Test each leaf specification independently**, with both a satisfying and a non-satisfying
  candidate — a specification with only a happy-path test hasn't verified it excludes anything.
- **Test a composite specification's combination logic, not its leaves' logic again.** Once
  `ActiveCustomerSpecification` and `HasOrdersSpecification` each have their own tests, a test of
  `isActive.And(hasOrders)` only needs to verify the four true/false combinations produce the
  correct combined result — it doesn't need to re-derive what makes a customer active.
- **If the specification exposes `ToExpression()`, test both the compiled in-memory path and (with
  an integration test) the query-provider-translated path**, since an expression that evaluates
  correctly when compiled and invoked directly can still fail to translate against a real query
  provider if it uses a construct the provider doesn't support — that failure is provider-specific
  and only an integration test against a real query provider catches it.
- **No mocking is usually needed.** A specification typically has no external dependencies to fake;
  most specification tests are plain data-in/boolean-out assertions against real candidate objects.

## Most likely scenarios

**1. Testing a leaf specification's true and false cases**

```csharp
[Theory]
[InlineData(CustomerStatus.Active, true)]
[InlineData(CustomerStatus.Suspended, false)]
public void ActiveCustomerSpecification_MatchesOnlyActiveCustomers(CustomerStatus status, bool expected)
{
    var spec = new ActiveCustomerSpecification();
    var customer = new Customer(status: status);

    Assert.Equal(expected, spec.IsSatisfiedBy(customer));
}
```

**2. Testing a composed specification's combination logic**

```csharp
[Theory]
[InlineData(true, true, true)]
[InlineData(true, false, false)]
[InlineData(false, true, false)]
[InlineData(false, false, false)]
public void AndSpecification_CombinesBothOperands(bool leftResult, bool rightResult, bool expected)
{
    var composite = new AndSpecification<object>(
        new StubSpecification<object>(leftResult),
        new StubSpecification<object>(rightResult));

    Assert.Equal(expected, composite.IsSatisfiedBy(new object()));
}

internal sealed class StubSpecification<T> : Specification<T>
{
    private readonly bool _result;
    public StubSpecification(bool result) => _result = result;
    public override bool IsSatisfiedBy(T candidate) => _result;
}
```

Using a stub specification with a fixed result — rather than two real business specifications — for
this test isolates the composite's `And` logic from any particular business rule's own correctness,
which is already covered by that specification's own leaf test.

**3. Testing an expression-typed specification translates correctly against a real query provider**

```csharp
[Fact]
public void ActiveCustomerSpecification_TranslatesAgainstQueryProvider()
{
    IQueryable<Customer> customers = TestDataSource.SeededCustomers(); // a real, provider-backed IQueryable<T>
    var spec = new ActiveCustomerSpecification();

    List<Customer> result = customers.Where(spec.ToExpression()).ToList();

    Assert.All(result, c => Assert.Equal(CustomerStatus.Active, c.Status));
}
```

This test needs a real, provider-backed `IQueryable<T>` — an in-memory `List<Customer>.AsQueryable()`
does not exercise translation at all, since `System.Linq.Enumerable`'s `IQueryable` implementation
never converts the expression tree into anything other than a compiled delegate.
