# `dynamic` and DLR Call Sites as a Reflection Alternative (C# 4.0 / .NET Framework 4.0)

C# 4.0 shipped in April 2010 with .NET Framework 4.0, introducing the `dynamic` type and the
Dynamic Language Runtime (DLR) that backs it. `dynamic` is not a replacement for `System.Type`
reflection — you still reflect when the *type itself* is only known as a runtime `Type` object —
but for the common case of "I have an `object` and want to call a member on it by name without a
compile-time reference to its type," `dynamic` replaces hand-written
`GetMethod`/`Invoke`/`GetProperty`/`GetValue` calls with ordinary member-access syntax, and the
DLR caches the binding per call site so repeated calls at the same site don't re-resolve the
member every time.

## Syntax

```csharp
dynamic value = GetRuntimeValue();
var result = value.SomeMethod(argument);
var property = value.SomeProperty;
```

## Basic use case: dynamic member access without manual reflection calls

```csharp
// Reflection form (works since C# 1.0, see csharp1-reflection-fundamentals.md):
object order = GetOrder();
Type orderType = order.GetType();
MethodInfo method = orderType.GetMethod("CalculateTotal");
object total = method.Invoke(order, parameters: null);

// dynamic form (C# 4.0+): the compiler emits the equivalent DLR call site for you.
dynamic dynamicOrder = GetOrder();
var dynamicTotal = dynamicOrder.CalculateTotal();
```

Both forms end up doing the same member lookup and invocation at runtime — `dynamic` just moves
the `GetMethod`/`Invoke` machinery into compiler-generated call-site code instead of code you write
by hand, and that call site caches its binding rule (the DLR's L0/L1/L2 cache levels) so a
`dynamic` call repeated on the same call site with the same runtime type resolves faster than
re-running `GetMethod` from scratch each time — though still not as fast as a cached, directly
invoked `MethodInfo` (see
[specialized/reflection-performance-and-caching.md](../specialized/reflection-performance-and-caching.md)).

## Advanced use case: `dynamic` over COM interop and duck-typed member access

```csharp
public static double GetTotalDynamically(dynamic anyOrderLikeObject)
{
    // Works for Order, a COM object, an ExpandoObject, or anything else exposing
    // a CalculateTotal() member — no shared interface or base type required.
    return anyOrderLikeObject.CalculateTotal();
}
```

```csharp
dynamic excelApp = Activator.CreateInstance(Type.GetType("Excel.Application"));
excelApp.Visible = true;
dynamic workbook = excelApp.Workbooks.Add();
```

`dynamic` was designed primarily for COM interop (the second example) and interop with
dynamically-typed languages hosted on the DLR — scenarios where reflecting manually to call
`Workbooks.Add()` on a COM object would be far more verbose than the dynamic call-site form the
compiler generates automatically.

## Requirements and restrictions

- `dynamic` defers *all* member resolution to runtime, including overload resolution — a typo in a
  member name compiles fine and throws `RuntimeBinderException` only when that line actually
  executes; there's no compile-time safety net the way there is with a `dynamic`-free reflection
  call wrapped in your own error handling.
- A `dynamic`-typed value flowing into LINQ or generic code can silently disable static type
  checking for the whole expression it appears in, not just the single member access — keep
  `dynamic` usage narrowly scoped to the member-access site that actually needs it.
- `dynamic` is not the same operation as `MakeGenericType`/`MakeGenericMethod` from
  [csharp2-generics-reflection.md](csharp2-generics-reflection.md) — `dynamic` resolves *member
  access* on an already-obtained object at runtime; it does not construct a generic `Type` or
  `MethodInfo` from an open definition. Use plain reflection, not `dynamic`, when you need to build
  a `Type` you don't have an instance of yet.

## Fallback

Below C# 4.0 / .NET Framework 4.0, there is no `dynamic` keyword or DLR. Write the member lookup
and invocation explicitly with `Type.GetMethod`/`GetProperty` and `MethodInfo.Invoke`/
`PropertyInfo.GetValue` — the same calls the compiler generates for you at a `dynamic` call site —
per [csharp1-reflection-fundamentals.md](csharp1-reflection-fundamentals.md).
