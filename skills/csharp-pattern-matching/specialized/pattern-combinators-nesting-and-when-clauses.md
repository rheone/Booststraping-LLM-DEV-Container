# Pattern Combinators, Nesting, and `when` Clauses

`when` guards have existed since
[csharp7-is-and-switch-patterns.md](../references/csharp7-is-and-switch-patterns.md); the `and`/
`or`/`not` combinators arrived in
[csharp9-relational-and-logical-patterns.md](../references/csharp9-relational-and-logical-patterns.md).
This file works through how they interact — when to reach for a combinator inside the pattern
itself versus a `when` guard beside it, deep nesting across property/positional/list patterns, and
matching on generic types and type parameters.

## Basic: `when` guard vs. combinator — same result, different tools

```csharp
// A combinator expresses a condition entirely IN the pattern language.
string ClassifyByCombinator(int score) => score switch
{
    < 0 or > 100 => "invalid",
    >= 90 => "A",
    _ => "other",
};

// A `when` guard expresses a condition that ISN'T expressible as a pattern at all —
// here, a relationship between two different values.
string ClassifyByGuard(Order order) => order switch
{
    { } o when o.Total > o.Customer.CreditLimit => "over limit",
    _ => "ok",
};
```

Prefer a combinator when the condition is *about the value being matched itself* and expressible
in pattern syntax (a range, a set of alternatives, a negation) — the compiler can reason about
combinator-based conditions for exhaustiveness and reachability warnings in ways it can't reason
about an opaque `when` guard. Reach for `when` when the condition compares the matched value to
something *external* (another parameter, a method call, a cross-property relationship) that no
pattern kind can express.

## Basic: combinator precedence pitfalls

```csharp
// WRONG: 'not' binds to '>= 'a'' alone, not to the whole 'and' expression.
static bool IsNotLowerCaseLetter(char c) => c is not >= 'a' and <= 'z';
// parses as: (not >= 'a') and <= 'z', i.e. "c < 'a' AND c <= 'z'" — not the intended
// "c is outside the 'a'..'z' range". It wrongly returns false for a non-letter character
// past 'z' (like '{', immediately after 'z' in ASCII), which the intended negation should
// have matched.

// RIGHT: parenthesize to force 'not' over the whole range check.
static bool IsNotLowerCaseLetterFixed(char c) => c is not (>= 'a' and <= 'z');
```

Precedence, loosest to tightest binding: `or`, then `and`, then `not` (binds to its immediate
operand only). This is the single most common pattern-combinator mistake — parenthesize any time
`not` combines with `and`/`or` rather than trusting default precedence, even when the default
happens to be correct, since a future edit can silently change which grouping is needed.

## Advanced: deep nesting across property, positional, and list patterns

```csharp
public record Address(string City, string State);
public record Customer(string Name, Address Address);
public record LineItem(string Sku, int Quantity);
public record Order(Customer Customer, LineItem[] Items, decimal Total);

public static string Classify(Order order) => order switch
{
    {
        Customer.Address.State: "CA" or "NY",
        Items: [{ Quantity: > 10 }, ..],
        Total: > 500m and < 5000m,
    } => "priority review",

    { Items: [] } => "empty order",

    { Customer.Address: { State: "CA" or "NY" }, Items.Length: > 0 } when order.Total <= 0m =>
        "zero-total order needs manual review",

    _ => "standard",
};
```

Nothing here is a new pattern kind — it's the property pattern
([csharp8-switch-expressions-and-recursive-patterns.md](../references/csharp8-switch-expressions-and-recursive-patterns.md)),
extended dot-notation
([csharp10-extended-property-patterns.md](../references/csharp10-extended-property-patterns.md)),
list pattern
([csharp11-list-and-slice-patterns.md](../references/csharp11-list-and-slice-patterns.md)), and
logical/relational combinators all composing freely, because every recursive pattern kind accepts
*any* other pattern as a nested pattern. The practical limit on nesting depth is readability, not
the language — past two or three levels, extracting a named local function (`bool
IsPriorityCustomer(Customer c) => ...`) used inside a `when` guard is usually clearer than pushing
the whole condition into pattern syntax.

## Advanced: patterns on generic types and constrained type parameters

```csharp
public static bool TryGetFirst<T>(IReadOnlyList<T> items, out T first)
{
    if (items is [var head, ..])
    {
        first = head;
        return true;
    }

    first = default!;
    return false;
}

public static string Summarize<T>(T value) where T : notnull => value switch
{
    IReadOnlyCollection<int> { Count: 0 } => "empty numeric collection",
    IReadOnlyCollection<int> ints => $"{ints.Count} numbers",
    string s => $"string \"{s}\"",
    _ => value.ToString() ?? "(null)",
};

public static string DescribeConstrained<T>(T value) where T : IComparable<T>
{
    return value switch
    {
        var v when v.CompareTo(default!) < 0 => "negative-ish",
        _ => "non-negative-ish",
    };
}
```

`TryGetFirst<T>` matches a list pattern directly against a generic-parameter-typed value — the
list-pattern requirements (`Length`/`Count` plus an indexer) apply to whatever concrete type `T`
resolves to at each call site, exactly as they would for a non-generic list pattern.
`Summarize<T>`'s `IReadOnlyCollection<int>` arms are ordinary type/property patterns matching
against an open type parameter, which has been legal since
[csharp7.1-generic-type-parameter-patterns.md](../references/csharp7.1-generic-type-parameter-patterns.md).
`DescribeConstrained<T>` shows a `when` guard calling a constrained interface member
(`IComparable<T>.CompareTo`) directly on the `var`-captured value — the constraint on `T` is what
makes `CompareTo` available inside the guard at all.

## Fallback

`when` guards work on every tier this skill covers, back to
[csharp7-is-and-switch-patterns.md](../references/csharp7-is-and-switch-patterns.md). The `and`/
`or`/`not` combinators need C# 9+; below that, express the same condition as chained `||`/`&&`
inside a `when` guard instead of inside the pattern. Extended dot-notation nesting needs C# 10+;
below that, nest an explicit property pattern per level. List patterns inside a nested pattern need
C# 11+ and a .NET Standard 2.1+/.NET Core 3.0+ target; below that, fall back to explicit
`Length`/indexed-access checks as shown in
[csharp11-list-and-slice-patterns.md#fallback](../references/csharp11-list-and-slice-patterns.md#fallback).
