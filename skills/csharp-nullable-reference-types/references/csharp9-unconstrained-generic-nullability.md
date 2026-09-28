# Unconstrained Generic Nullability and Flow-Analysis Refinements (C# 9.0 / .NET 5, November 2020)

C# 9.0 shipped with .NET 5 and refined nullable reference types in two directions: `T?` became
legal on a type parameter with *no* constraint at all (C# 8.0 required `class` or `struct` on `T`
before `T?` would parse in most positions), and the flow analyzer learned to trust a wider set of
signals — a helper method that assigns a field, and a null-conditional chain that ends in `!`. Both
changes make previously-awkward generic and chained-access code expressible without workarounds; a
C# 8.0 codebase that already opted into NRT keeps working unchanged.

## Syntax

```csharp
// C# 8.0: T? only parsed where T was already constrained to class or struct.
// C# 9.0: T? is legal on a fully unconstrained T.
public interface IComparer<in T>
{
    int Compare(T? left, T? right);
}
```

```csharp
public class Widget
{
    private Connection? _connection;

    [MemberNotNull(nameof(_connection))]
    private void EnsureConnected() => _connection ??= new Connection();

    public void Send(byte[] payload)
    {
        EnsureConnected();
        _connection.Send(payload); // no warning: EnsureConnected() told the analyzer _connection is set
    }
}
```

## Basic use case: `T?` on an unconstrained type parameter

```csharp
#nullable enable

public static class OptionalExtensions
{
    public static T? FirstOrDefaultNullable<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (T item in source)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return default; // T? here means Nullable<T> when T is a value type, plain null when T is a reference type
    }
}
```

When `T` is unconstrained, `T?` means one of two different things depending on what the caller
substitutes: for a reference-type argument, it's an ordinary nullable-annotated reference; for a
value-type argument, it's `Nullable<T>` (`int?`-style), exactly as it always has been since C# 2.0.
The compiler resolves which one applies per call site — the generic method's own source has one
`T?`, but `OptionalExtensions.FirstOrDefaultNullable<string>(...)` and
`OptionalExtensions.FirstOrDefaultNullable<int>(...)` behave like two different return-type shapes
under the hood.

## Advanced use case: `MemberNotNull` closing the constructor-helper gap

```csharp
#nullable enable

public class Repository
{
    private readonly DbConnection _connection;
    private readonly ILogger _logger;

    public Repository(string connectionString, ILogger logger)
    {
        _logger = logger;
        Initialize(connectionString); // compiler can't see this assigns _connection, without the attribute
    }

    [MemberNotNull(nameof(_connection))]
    private void Initialize(string connectionString)
    {
        _connection = new DbConnection(connectionString);
    }
}
```

Before `[MemberNotNull]`, splitting field initialization out of the constructor body into a shared
helper (common when several constructor overloads need the same setup) produced a false-positive
"field is never assigned" warning, because the analyzer only tracked assignment it could see
directly in the constructor. `[MemberNotNull(nameof(_connection))]` on the helper tells the
analyzer: after this method returns, treat the named field as definitely assigned — the same
attribute family as the C# 8.0-era `[NotNull]`/`[MaybeNull]` attributes, but this specific one
(along with `[MemberNotNullWhen]`) is new in this release, shipped in the .NET 5 BCL alongside the
language change, not in .NET Core 3.0 with the rest of the family.

## Requirements and restrictions

- Unconstrained `T?` needs C# 9.0's compiler; using it under an explicit `<LangVersion>8.0</LangVersion>`
  still produces the C# 8.0-era "type parameter must be known to be a reference type" diagnostic.
- `[MemberNotNull]`/`[MemberNotNullWhen]` require .NET 5 or a NuGet polyfill package supplying the
  attribute types on an older target framework; they have no effect without a nullable-enabled
  context to report warnings into, same as the rest of this attribute family.
- A null-conditional chain ending in `!` (`x?.y!.z`) suppresses the warning only at that specific
  link — it does not retroactively make `x` itself non-null for anything after the expression.

## Fallback

Below C# 9.0, an unconstrained `T?` doesn't parse — add `where T : class` (accepting only
reference-type arguments) or `where T : struct` (accepting only value-type arguments) to the type
parameter, splitting the method in two if both need supporting, or drop the annotation and accept
the C# 8.0-era oblivious behavior for that parameter. Below C# 9.0 / .NET 5, `[MemberNotNull]` and
`[MemberNotNullWhen]` don't exist; use the C# 8.0 pattern instead — assign the field directly in
each constructor, or accept the false-positive warning and suppress it with `= null!;` on the field
declaration. See [csharp8-nullable-reference-types.md](csharp8-nullable-reference-types.md) for the
baseline both of these extend.
