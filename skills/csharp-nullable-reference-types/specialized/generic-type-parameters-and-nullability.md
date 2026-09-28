# Generic Type Parameters and Nullability

`T?` means a different thing depending on what `T` is constrained to and what the caller
substitutes for it — this is the single most common source of confusion in nullable-aware generic
code, because the same two characters compile to genuinely different semantics depending on
context. This file assumes familiarity with generic type parameters themselves and focuses only on
the nullability-specific behavior; the underlying syntax comes from
[csharp8-nullable-reference-types.md](../references/csharp8-nullable-reference-types.md) and
[csharp9-unconstrained-generic-nullability.md](../references/csharp9-unconstrained-generic-nullability.md).

## Basic: `T?` means two different things depending on the type argument

```csharp
#nullable enable

public static T? FirstOrDefault<T>(this IEnumerable<T> source)
{
    foreach (T item in source)
    {
        return item;
    }

    return default;
}

int? firstInt = new[] { 1, 2, 3 }.FirstOrDefault();       // Nullable<int> -- a boxed-free value type wrapper
string? firstString = new[] { "a", "b" }.FirstOrDefault(); // plain nullable-annotated reference
```

One generic method, one `T?` in its source — but `FirstOrDefault<int>` returns `Nullable<int>` (a
distinct value type with `.HasValue`/`.Value`, four bytes larger than a bare `int`, boxes
differently) while `FirstOrDefault<string>` returns a plain `string` reference annotated nullable
(no wrapper, no extra storage, identical CLR type to `string`). The generic method's author writes
`T?` once; the two instantiations behave like two unrelated types at the call site.

## Basic: `notnull` vs. leaving `T` unconstrained

```csharp
#nullable enable

public class Cache<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, object> _store = new();

    public void Set(TKey key, object value) => _store[key] = value; // key can never legally be null
}

public class Box<T> // unconstrained: T could be a reference type OR a value type, nullable or not
{
    public T? Value { get; set; } // means "nullable reference" or "Nullable<T>" depending on caller
}
```

`where TKey : notnull` rejects both a nullable reference type argument and a nullable value type
argument (`int?`) in a nullable-enabled context — it's the constraint `Dictionary<TKey, TValue>`
itself uses, because a dictionary key that's allowed to be `null` breaks lookup semantics. Leaving
`T` unconstrained (as in `Box<T>`) is a different, equally valid choice: it says "I accept anything,
and my own `T?` members adapt to whatever the caller supplies," which is the right shape for a
general-purpose container rather than a keyed one.

## Advanced: the `Nullable<T>` vs. nullable-reference-`T?` split at the API boundary

```csharp
#nullable enable

public static class Optional
{
    // A method constrained to value types uses Nullable<T> explicitly -- T? here always
    // means the same thing, because struct rules T out as ever being a reference type.
    public static bool TryUnwrap<T>(T? nullable, out T value) where T : struct
    {
        value = nullable.GetValueOrDefault();
        return nullable.HasValue;
    }
}

public class Repository<TEntity> where TEntity : class
{
    // A method constrained to reference types uses T? to mean "nullable reference" --
    // no Nullable<T> wrapper is possible once T is constrained to class.
    public TEntity? FindOrDefault(int id) => default;
}
```

Constraining `T` to `struct` or `class` up front removes the ambiguity entirely: inside a
`where T : struct` method, `T?` can only ever mean `Nullable<T>`; inside a `where T : class` method,
it can only ever mean a nullable reference. The confusing case is specifically the *unconstrained*
middle ground, where the same syntax silently picks one behavior or the other per call site — worth
calling out explicitly in code review, since two readers of the same unconstrained `T?` declaration
can walk away with different mental models of what it does until they check how it's actually
instantiated.

## Advanced: oblivious type arguments flowing into a nullable-enabled generic type

```csharp
#nullable enable
public class Wrapper<T>
{
    public T Value { get; init; } = default!;
}

// A type argument supplied from an oblivious (#nullable disable) assembly carries no
// nullability information of its own -- Wrapper<LegacyType>.Value is NotNull to the analyzer
// here even though LegacyType's own author never made any promise about null.
```

A generic type's own nullable annotations are enforced based on where the generic type itself is
declared, not where its type argument came from — substituting a type argument whose declaring
assembly was compiled without NRT doesn't make the generic type's members any less strict; it just
means the analyzer has no additional information about that specific argument type to combine with
its own annotations.

## Fallback

Every example above needs at minimum
[C# 8.0](../references/csharp8-nullable-reference-types.md) for `notnull` and constrained `T?`; the
fully unconstrained `T?` form needs
[C# 9.0](../references/csharp9-unconstrained-generic-nullability.md). Below C# 8.0, none of this
distinction exists — a generic reference-type parameter is always potentially null with no way to
say otherwise, and `Nullable<T>` for a constrained value-type parameter works exactly as it has
since C# 2.0, independent of nullable reference types entirely.
