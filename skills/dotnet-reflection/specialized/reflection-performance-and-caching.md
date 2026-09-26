# Reflection Performance: Caching, `MethodImplOptions`, and Fast-Invocation Escape Hatches

Reflection's per-call cost has two separate sources: the *lookup* (`GetMethod`, `GetProperty` —
walking metadata tables by name/signature) and the *invocation* (`MethodInfo.Invoke`,
`PropertyInfo.GetValue`/`SetValue` — boxing arguments, dynamic dispatch through the runtime's
invocation stub). Caching eliminates the first cost; a compiled-delegate invoker eliminates most
of the second. This file assumes the baseline reflection calls from
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md).

## Basic: caching `MemberInfo` lookups instead of repeating them

```csharp
public static class OrderReflection
{
    public static readonly PropertyInfo StatusProperty =
        typeof(Order).GetProperty(nameof(Order.Status))!;

    public static readonly MethodInfo ValidateMethod =
        typeof(Order).GetMethod(nameof(Order.Validate))!;
}
```

```csharp
object currentStatus = OrderReflection.StatusProperty.GetValue(order);
object isValid = OrderReflection.ValidateMethod.Invoke(order, parameters: null);
```

A `GetProperty`/`GetMethod` lookup walks the type's metadata every call; a `static readonly`
field (or, for reflection keyed by a runtime `Type` rather than a compile-time one, a
`ConcurrentDictionary<Type, PropertyInfo>` populated with `GetOrAdd`) pays that cost once per
process instead of once per call. This is the single highest-value reflection performance fix and
costs nothing in code complexity.

## Basic: `MethodImplOptions` on the *reflected* method, not the reflecting code

```csharp
public class Order
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal CalculateTotal() => LineItems.Sum(i => i.Price * i.Quantity);
}
```

`MethodImplOptions.AggressiveInlining` (.NET Framework 4.5+) and
`MethodImplOptions.AggressiveOptimization` (.NET Core 3.0+) influence how the JIT compiles the
*target* method itself — they have no effect on the cost of reflecting into or invoking that
method through `MethodInfo.Invoke`, which always goes through the runtime's generic invocation
stub regardless of how the target method is JIT-compiled. Don't reach for `MethodImplOptions`
expecting it to speed up reflection calls; it only ever affects direct calls to the method.

## Advanced: a compiled-delegate invoker as the fast-invocation escape hatch

```csharp
public static class FastInvoker
{
    private static readonly ConcurrentDictionary<MethodInfo, Func<object, object?[], object?>> Cache = new();

    public static object? Invoke(MethodInfo method, object target, params object?[] args) =>
        Cache.GetOrAdd(method, BuildInvoker)(target, args);

    private static Func<object, object?[], object?> BuildInvoker(MethodInfo method)
    {
        var openDelegate = (Func<Order, object?>)Delegate.CreateDelegate(
            typeof(Func<Order, object?>), firstArgument: null, method);
        return (target, args) => openDelegate((Order)target);
    }
}
```

`Delegate.CreateDelegate` binds a `MethodInfo` to a strongly-typed delegate once; every call
through that delegate afterward runs at close to direct-call speed, because the delegate's
invocation no longer goes through `MethodInfo.Invoke`'s boxing-and-dispatch path — only the
one-time `CreateDelegate` binding pays a reflection cost. This is the same idea .NET's own
high-performance serializers and mappers use internally: reflect once to discover shape, then
build a fast, reusable invoker instead of re-invoking through `MethodInfo` on every call. Two
common ways to build that invoker: `Delegate.CreateDelegate` against a known delegate shape (shown
above — the most direct option, no extra dependency) or an `System.Reflection.Emit.DynamicMethod`
emitting IL by hand for cases `CreateDelegate` can't express (e.g. a single invoker signature that
must work across many unrelated target-method shapes). A compiled `System.Linq.Expressions`
lambda is a third option with the same performance profile as `DynamicMethod`, useful when the
call shape varies more than `CreateDelegate` allows for — building and compiling one is its own
topic outside this skill's scope.

## Advanced: measure before optimizing

Reflection's overhead only matters on a hot path — a lookup or invocation running per-item in a
tight loop, per-request in a hot API, or per-frame in a game loop. A one-time startup reflection
scan (discovering plugin types, building a DI container's registration graph) essentially never
needs this file's techniques; profile first, and apply caching or a compiled invoker only to the
specific call site the profile actually flags.

## Fallback

Caching a `MemberInfo` in a `static readonly` field or `ConcurrentDictionary` works from C# 1.0
onward — no version gate. `Delegate.CreateDelegate` has existed since .NET Framework 1.1 (its
generic-delegate-friendly overloads since .NET Framework 2.0, alongside the generics reflection
in [csharp2-generics-reflection.md](../references/csharp2-generics-reflection.md)).
`MethodImplOptions.AggressiveOptimization` needs .NET Core 3.0+; on an older target, omit it — its
absence only affects the target method's own JIT tier-up behavior, never correctness.
