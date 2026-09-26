# Reflecting Nullable Reference Type Annotations: `NullabilityInfoContext` (C# 10 / .NET 6)

Nullable reference types (`string?` vs. `string`) are a C# 8.0 / .NET Core 3.0 (September 2019)
compile-time-only feature — the compiler emits `[Nullable]`/`[NullableContext]` metadata
attributes describing the annotations, but for two full versions there was no supported,
ergonomic runtime API to read them back. That API — `System.Reflection.NullabilityInfoContext` —
did not ship until .NET 6 (November 2021), alongside C# 10. This is the exact "which version
actually shipped this" trap version-gating exists to catch: the nullability *annotations* are
C# 8, but the nullability *reflection API* is two full versions later, C# 10 / .NET 6, not C# 8.
Decoding the raw `[Nullable]` attribute's byte-encoded metadata yourself was technically possible
earlier but never a supported, documented path — treat this as the first version where nullability
reflection was actually available to use.

## Syntax

```csharp
var nullabilityContext = new NullabilityInfoContext();

PropertyInfo property = typeof(Order).GetProperty(nameof(Order.CustomerNote))!;
NullabilityInfo info = nullabilityContext.Create(property);

NullabilityState readState = info.ReadState;   // NotNull, Nullable, or Unknown
NullabilityState writeState = info.WriteState;
```

## Basic use case: checking whether a property is annotated nullable

```csharp
#nullable enable
public class Order
{
    public string CustomerNote { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
}
#nullable restore
```

```csharp
var context = new NullabilityInfoContext();
Type orderType = typeof(Order);

foreach (PropertyInfo property in orderType.GetProperties())
{
    NullabilityInfo info = context.Create(property);
    bool isNullable = info.ReadState == NullabilityState.Nullable;
    Console.WriteLine($"{property.Name}: {(isNullable ? "nullable" : "non-nullable")}");
}
// CustomerNote: non-nullable
// TrackingNumber: nullable
```

This is the pattern a validation framework, an ORM, or a JSON serializer uses to decide whether a
missing or `null` value for a given property is acceptable — reading the same nullable-annotation
metadata the compiler used for its own flow analysis, but at runtime and from reflected
`PropertyInfo`/`FieldInfo`/`ParameterInfo` rather than from source.

## Advanced use case: nullability of generic type arguments and method parameters

```csharp
#nullable enable
public class Repository<T> where T : class
{
    public T? FindOrDefault(int id) => default;
    public Dictionary<string, List<T?>> Cache { get; } = new();
}
#nullable restore
```

```csharp
var context = new NullabilityInfoContext();
MethodInfo method = typeof(Repository<Order>).GetMethod(nameof(Repository<Order>.FindOrDefault))!;

NullabilityInfo returnInfo = context.Create(method.ReturnParameter);
bool returnsNullable = returnInfo.ReadState == NullabilityState.Nullable; // true — T? FindOrDefault

PropertyInfo cacheProperty = typeof(Repository<Order>).GetProperty(nameof(Repository<Order>.Cache))!;
NullabilityInfo cacheInfo = context.Create(cacheProperty);
NullabilityInfo valueListElementInfo = cacheInfo.GenericTypeArguments[1].GenericTypeArguments[0];
bool elementIsNullable = valueListElementInfo.ReadState == NullabilityState.Nullable; // true — List<T?>
```

`NullabilityInfo.GenericTypeArguments` mirrors `Type.GetGenericArguments()` from
[csharp2-generics-reflection.md](csharp2-generics-reflection.md) one level deeper — nullability on
a generic type argument (`List<T?>`'s element, not `List<T?>` itself) is read by walking
`GenericTypeArguments` on the outer `NullabilityInfo`, the same nested-structure idea as walking
`Type.GetGenericArguments()` recursively for a generic type built from other generic types.

## Requirements and restrictions

- Reflected nullability comes from compiler-emitted metadata attributes and reflects source-level
  annotations, not runtime guarantees — `ReadState == NullabilityState.NotNull` describes what the
  author wrote (`string`, not `string?`), not a runtime-enforced non-null invariant; a caller can
  still pass or store `null` through APIs that don't check.
- `NullabilityState.Unknown` is a real, common result — it means the member's declaring assembly
  was compiled without nullable reference types enabled (no `#nullable enable`, no
  `<Nullable>enable</Nullable>` in the project), not an error in your reflection code; don't treat
  it as equivalent to `NotNull`.
- Reuse a single `NullabilityInfoContext` instance across multiple `Create` calls where possible —
  it caches per-context state internally, so constructing a fresh one per property lookup gives up
  some of that caching.

## Fallback

Below C# 10 / .NET 6, `NullabilityInfoContext` doesn't exist. On .NET Core 3.0–.NET 5 / C# 8–9
(where nullable reference type annotations exist but no supported reflection API reads them),
there is no equivalent supported alternative — either upgrade the target framework, or fall back
to a compile-time-only nullability check (the compiler's own flow analysis, which needs no
reflection) instead of a runtime one. Below C# 8 / .NET Core 3.0, nullable reference types don't
exist at all; every reference type is nullable by the pre-C#-8 convention and there is nothing to
reflect.
