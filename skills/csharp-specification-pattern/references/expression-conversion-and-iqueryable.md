# Expression Conversion and IQueryable

## Why `IsSatisfiedBy` alone isn't enough for queries

A specification whose only member is `bool IsSatisfiedBy(T candidate)` can only ever run against an
object already sitting in memory — evaluating it requires the full `T` instance to have already
been loaded. Passing `IsSatisfiedBy` to `IQueryable<T>.Where` compiles, but a query provider that
translates queries into a store's native query language (rather than executing them against an
already-materialized in-memory sequence) cannot translate an opaque compiled delegate back into that
language — at best, this forces loading the entire table into memory before filtering; at worst, it
throws at run time because the provider refuses to execute an untranslatable delegate.

## The expression-typed alternative

`Expression<Func<T, bool>>` is not a compiled delegate — it's a data structure describing the
predicate's logic as an expression tree, which a query provider can walk and translate into its own
query language before ever loading a row. A specification that exposes this shape composes with
`IQueryable<T>` correctly:

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
}

public sealed class ActiveCustomerSpecification : ISpecification<Customer>
{
    public Expression<Func<Customer, bool>> ToExpression() =>
        customer => customer.Status == CustomerStatus.Active;
}
```

```csharp
ISpecification<Customer> spec = new ActiveCustomerSpecification();
IQueryable<Customer> query = dbSet.Where(spec.ToExpression());
```

The provider sees the lambda's expression tree, not a compiled delegate, and can translate
`customer.Status == CustomerStatus.Active` into whatever the underlying store's native filter syntax
is — the filtering happens at the store, not after loading everything into memory.

## Getting `IsSatisfiedBy` back for in-memory use

A specification that exposes `ToExpression()` can still support in-memory evaluation by compiling
the expression on demand:

```csharp
public static class SpecificationExtensions
{
    public static bool IsSatisfiedBy<T>(this ISpecification<T> specification, T candidate) =>
        specification.ToExpression().Compile().Invoke(candidate);
}
```

This keeps one canonical definition of the rule (the expression tree) serving both use cases —
store-side filtering through `ToExpression()` directly, and in-memory checks through the compiled
delegate — rather than maintaining the same condition twice in two different forms that could drift
apart.

## Composing expression-typed specifications

`And`/`Or`/`Not` composition (see [composition-and-or-not.md](composition-and-or-not.md)) on
expression-typed specifications needs to combine two expression trees into one new tree, not just
call both compiled delegates and combine the booleans — otherwise the composite loses query
translatability even though its two components had it individually. Combining expression trees by
hand means rewriting parameter references so both sides share one parameter:

```csharp
public sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = ReplaceParameter(_left.ToExpression(), parameter);
        var rightBody = ReplaceParameter(_right.ToExpression(), parameter);
        var combined = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(Expression<Func<T, bool>> expression, ParameterExpression parameter) =>
        new ParameterReplacer(expression.Parameters[0], parameter).Visit(expression.Body);
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly ParameterExpression _target;

    public ParameterReplacer(ParameterExpression source, ParameterExpression target)
    {
        _source = source;
        _target = target;
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _source ? _target : base.VisitParameter(node);
}
```

This is real complexity compared to the delegate-combining version in
[composition-and-or-not.md](composition-and-or-not.md) — it exists specifically to preserve
translatability through composition. A specification only ever evaluated in memory never needs this
machinery; a specification composed and then handed to `IQueryable<T>.Where` does.

## Practical guidance

Design a specification's public contract around expression conversion (`ToExpression()`) from the
start if it will ever be used against `IQueryable<T>` — retrofitting expression support onto a
specification hierarchy already built purely around `IsSatisfiedBy` means rewriting every
composition operator, not just the leaf specifications.
