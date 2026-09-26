# `nameof` as a Refactor-Safe Alternative to Reflecting by String (C# 6.0 / .NET Framework 4.6)

C# 6.0 shipped in July 2015 with Visual Studio 2015 and .NET Framework 4.6. It added no new
reflection *API* — `nameof` is a compile-time operator, not a `System.Reflection` member — but it
directly changes how reflection code should be written from this version forward: everywhere a
member is looked up by a hard-coded string (`GetProperty("Total")`, `GetMethod("Validate")`),
`nameof` produces that same string from a compile-time-checked member reference instead, so a
rename that misses the string literal becomes a compiler error rather than a silent runtime
`null` from `GetProperty`.

## Syntax

```csharp
string propertyName = nameof(Order.Total);
PropertyInfo property = typeof(Order).GetProperty(nameof(Order.Total));
```

## Basic use case: renaming-safe member lookup

```csharp
// Before C# 6.0: a magic string that a rename of Total can silently break.
PropertyInfo property = typeof(Order).GetProperty("Total");

// C# 6.0+: nameof(Order.Total) is a compile-time reference to the member —
// renaming Total produces a compiler error at this call site instead of a
// runtime null from GetProperty.
PropertyInfo saferProperty = typeof(Order).GetProperty(nameof(Order.Total));
```

The reflection call itself — `GetProperty`, `GetMethod`, `GetField` — is unchanged from
[csharp1-reflection-fundamentals.md](csharp1-reflection-fundamentals.md); `nameof` only changes
where the string argument comes from.

## Advanced use case: argument validation and generic member lookup

```csharp
public void SetPropertyValue<T>(T target, string propertyName, object value)
{
    PropertyInfo? property = typeof(T).GetProperty(propertyName)
        ?? throw new ArgumentException($"No property named '{propertyName}' on {typeof(T)}.", nameof(propertyName));
    property.SetValue(target, value);
}
```

```csharp
SetPropertyValue(order, nameof(Order.Status), OrderStatus.Shipped);
```

Two independent uses of `nameof` in the same call: `nameof(propertyName)` inside the method names
its own parameter for the `ArgumentException` (the idiomatic use most C# code reaches for
`nameof` first), while the caller's `nameof(Order.Status)` supplies the reflected member name
itself — both are the identical compile-time string-extraction mechanism applied to two different
kinds of symbol (a parameter, a property).

```csharp
public static class ReflectionCache<T>
{
    public static readonly PropertyInfo StatusProperty =
        typeof(T).GetProperty(nameof(Order.Status))!;
}
```

Combining `nameof` with a cached, `static readonly` `PropertyInfo` (see
[specialized/reflection-performance-and-caching.md](../specialized/reflection-performance-and-caching.md))
gets both renaming safety and lookup performance in the same declaration.

## Requirements and restrictions

- `nameof(Order.Total)` requires `Total` to be a real, currently-compiling member of `Order` — it
  cannot express a member that was removed or renamed elsewhere and not yet updated here, which is
  exactly the property that makes it safer than a string literal.
- `nameof` produces only the *simple* name (`"Total"`), never a fully qualified or
  assembly-qualified name — it's a direct substitute for the string argument to `GetProperty`/
  `GetMethod`/`GetField`, not for `Type.GetType`'s assembly-qualified-name argument.
  `nameof(Order)` on a namespaced type still yields just `"Order"`, not
  `"MyApp.Domain.Order"`.
- `nameof` cannot express which *overload* of a method you mean (`nameof(Mapper.Convert)` when
  `Convert` has several overloads still yields the single string `"Convert"`) — disambiguating an
  overloaded method for `GetMethod` still requires passing a `Type[]` of parameter types, per
  [csharp1-reflection-fundamentals.md](csharp1-reflection-fundamentals.md)'s requirements.

## Fallback

Below C# 6.0 / .NET Framework 4.6, `nameof` doesn't exist. Pass the member name as a plain string
literal to `GetProperty`/`GetMethod`/`GetField`, and rely on unit tests (or a code-analysis rule)
rather than the compiler to catch a rename that misses the literal — see
[csharp1-reflection-fundamentals.md](csharp1-reflection-fundamentals.md) for the unchanged
reflection calls themselves.
