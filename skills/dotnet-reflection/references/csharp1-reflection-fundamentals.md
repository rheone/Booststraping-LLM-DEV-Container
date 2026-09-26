# Reflection Fundamentals: `Type`, `MemberInfo`, `Activator` (C# 1.0 / .NET Framework 1.0)

`System.Reflection` and `System.Type` shipped with .NET Framework 1.0 in 2002 — reflection is not a
later addition to C#, it's baseline functionality present in the very first version of the
language and runtime. Every later tier in this skill adds a specific new capability (generic type
reflection, `dynamic`, refactor-safe member names, nullability introspection) on top of this
unchanged foundation: `Type`, the four `MemberInfo` subtypes, and `Activator`.

## Syntax

```csharp
Type type = typeof(Order);
Type runtimeType = order.GetType();
Type byName = Type.GetType("MyApp.Domain.Order");

MethodInfo method = type.GetMethod("Validate");
PropertyInfo property = type.GetProperty("Total");
FieldInfo field = type.GetField("_lineItems", BindingFlags.NonPublic | BindingFlags.Instance);
ConstructorInfo ctor = type.GetConstructor(new[] { typeof(string) });
```

## Basic use case: inspect a type and invoke a member dynamically

```csharp
Type orderType = typeof(Order);
object order = Activator.CreateInstance(orderType);

PropertyInfo statusProperty = orderType.GetProperty("Status");
statusProperty.SetValue(order, OrderStatus.Pending);
object currentStatus = statusProperty.GetValue(order);

MethodInfo validateMethod = orderType.GetMethod("Validate");
object isValid = validateMethod.Invoke(order, parameters: null);
```

`PropertyInfo.GetValue`/`SetValue` and `MethodInfo.Invoke` are the whole story for
expression-tree-free dynamic member access: no `System.Linq.Expressions` dependency, just
`MemberInfo` plus boxed `object` arguments. `Activator.CreateInstance(Type)` constructs an
instance from a runtime-known `Type` without a compile-time reference to the concrete type —
useful for plugin loading, dependency-injection containers, and deserialization, all of which
predate C# itself as a general pattern and needed no later language feature to become possible.

## Advanced use case: custom attribute inspection and constructor selection

```csharp
public sealed class AuditableAttribute : Attribute
{
    public string Category { get; init; } = "General";
}

[Auditable(Category = "Financial")]
public sealed class Invoice { }
```

```csharp
Type invoiceType = typeof(Invoice);

bool isAuditable = invoiceType.IsDefined(typeof(AuditableAttribute), inherit: false);
if (isAuditable)
{
    var attribute = (AuditableAttribute)invoiceType
        .GetCustomAttributes(typeof(AuditableAttribute), inherit: false)
        .Single();
    string category = attribute.Category;
}

// CustomAttributeExtensions' generic overload (.NET Framework 4.5+ BCL addition, not
// a C# language change) reads more naturally when the attribute type is known at compile time:
AuditableAttribute? typed = invoiceType.GetCustomAttribute<AuditableAttribute>();
```

```csharp
Type serviceType = typeof(OrderService);
ConstructorInfo[] constructors = serviceType.GetConstructors();
ConstructorInfo greediest = constructors
    .OrderByDescending(c => c.GetParameters().Length)
    .First();
object service = greediest.Invoke(resolvedArguments);
```

Picking the "greediest" constructor by parameter count is the same pattern a hand-rolled
dependency-injection container uses before it can lean on a real DI framework — plain
`ConstructorInfo`/`ParameterInfo` reflection, nothing version-gated about it.

## Requirements and restrictions

- `Type.GetType(string)` only finds types in the calling assembly and `mscorlib`/`System.Private.CoreLib`
  unless the string is assembly-qualified (`"Namespace.Type, AssemblyName"`) — a bare type name for
  a type in another assembly returns `null` silently unless you search `Assembly.GetTypes()` or use
  `Assembly.GetType(string)` on the specific assembly.
- `Type.GetMethod(string)` throws `AmbiguousMatchException` if more than one overload matches by
  name alone — pass a `Type[]` of parameter types to disambiguate.
- `BindingFlags.NonPublic` alone does not return private members; it must be combined with
  `BindingFlags.Instance` or `BindingFlags.Static` (`GetMethod`/`GetProperty`/`GetField` return
  `null` for a private member if you pass only `BindingFlags.NonPublic`).
- `MethodInfo.Invoke` and `ConstructorInfo.Invoke` box value-type arguments and unwrap the return
  value from `object`, and wrap any exception thrown by the invoked member in a
  `TargetInvocationException` — unwrap `.InnerException` to see the original exception type.
- Reflection calls (`Invoke`, `GetValue`/`SetValue`, `CreateInstance`) are meaningfully slower than
  direct calls per invocation; see
  [specialized/reflection-performance-and-caching.md](../specialized/reflection-performance-and-caching.md)
  before putting any of this on a hot path.

## Fallback

This is the first tier — reflection has no "before." `System.Reflection` and `System.Type` are
present in every C# version and every .NET Framework/.NET release in this skill's scope; there is
no older workaround to fall back to.
