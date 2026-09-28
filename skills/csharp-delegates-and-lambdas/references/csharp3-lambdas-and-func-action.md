# Lambda Expressions, `Func<>`, and `Action<>` (C# 3.0 / .NET Framework 3.5)

C# 3.0 (.NET Framework 3.5, November 2007) introduced lambda expression syntax as the terse
successor to the anonymous-method syntax from
[csharp2-anonymous-methods-and-generic-delegates.md](csharp2-anonymous-methods-and-generic-delegates.md),
and the BCL shipped the first `Func<>`/`Action<>` generic delegate family alongside it —
`Action`/`Action<T1..T4>` and `Func<TResult>`/`Func<T1..T4, TResult>` (0–4 input parameters).
Higher arities (5–16 parameters) don't arrive until .NET Framework 4.0/C# 4.0, covered in
[csharp4-variance-and-extended-func-action.md](csharp4-variance-and-extended-func-action.md).
Lambdas were built specifically to support LINQ (also new in C# 3.0), but they're a general-purpose
delegate-construction syntax independent of LINQ.

## Syntax

```csharp
Func<int, int, int> add = (x, y) => x + y;        // expression-bodied
Action<string> log = message => { Console.WriteLine(message); }; // statement-bodied
```

Expression-bodied form (`params => expr`) is the common case: the expression's value is the
lambda's return value, no `return` keyword. Statement-bodied form (`params => { statements }`)
allows multiple statements and an explicit `return` when the delegate isn't `void`-returning.

## Basic use case

```csharp
Func<int, bool> isEven = n => n % 2 == 0;
Func<string, int> length = s => s.Length;
Action<Order> markShipped = order => order.Status = OrderStatus.Shipped;
Predicate<int> isNegative = n => n < 0; // Predicate<T> still works — lambdas target ANY compatible delegate type
```

A lambda isn't tied to `Func<>`/`Action<>` specifically — it can be assigned to `Predicate<T>`,
`Comparison<T>`, or any custom delegate type whose signature matches, including a generic one:

```csharp
public delegate TResult Transformer<TInput, TResult>(TInput input);

Transformer<int, string> describe = n => n > 0 ? "positive" : "non-positive";
```

## Advanced use case: replacing anonymous methods, and composing with generic methods

```csharp
public static IEnumerable<TResult> Map<TSource, TResult>(
    IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    foreach (TSource item in source)
    {
        yield return selector(item);
    }
}

IEnumerable<string> labels = Map(new[] { 1, 2, 3 }, n => $"#{n}");
```

`Map<TSource, TResult>` is a generic method taking a `Func<TSource, TResult>` parameter — the same
shape LINQ's own `Select` uses. The anonymous-method equivalent of the lambda above,
`delegate(int n) { return $"#{n}"; }`, still compiles under C# 3.0+; lambdas complement anonymous
methods rather than replacing the syntax outright, but new code almost always prefers the lambda
form for its brevity.

The compiler picks the lambda's **underlying delegate type** from the target context (the parameter
or variable type it's assigned to) — `Func<int, bool>` above, a custom `Transformer<int, string>`
above that. A lambda assigned directly to a local `var` with no other context has no target type to
infer from in C# 3.0–9.0; that gap is closed by natural typing in
[csharp10-natural-type-and-lambda-annotations.md](csharp10-natural-type-and-lambda-annotations.md).

A lambda's underlying type isn't always a delegate, either: the exact same syntax compiles to an
`Expression<TDelegate>` instead when the target type is an expression tree type (this is how LINQ
providers like EF Core see your query as data instead of compiled IL) — the full mechanics of that
conversion (building and walking an expression tree) are out of scope for this skill.

## Requirements and restrictions

- Multi-parameter lambdas require parentheses (`(x, y) => ...`); a single parameter may omit them
  (`x => ...`), except once an explicit type or modifier is present (later tiers).
- A statement-bodied lambda assigned to a non-`void` delegate type must return on every code path,
  same rule as an ordinary method body.
- Lambdas capture enclosing-scope variables exactly like the anonymous methods they extend — same
  closure rules, same loop-variable pitfalls; see
  [specialized/closures-and-variable-capture.md](../specialized/closures-and-variable-capture.md).

## Fallback

On .NET Framework 2.0 (C# 2.0), write the anonymous-method form instead —
`delegate(int x, int y) { return x + y; }` in place of `(x, y) => x + y` — against a hand-declared
generic delegate type, since `Func<>`/`Action<>` don't exist yet; see
[csharp2-anonymous-methods-and-generic-delegates.md](csharp2-anonymous-methods-and-generic-delegates.md).
