# Detecting Compiler-Lowered Member Shapes: Init-Only, Required Members, Extension Members

Several C# features since C# 9 change what a member looks like *in source* without adding any new
`System.Reflection` API to describe the change — the compiler lowers the new syntax down to plain
IL constructs that predate the feature, and reflection code has to know the lowering to detect the
feature at all. This is a real, recurring gap (each case below started as an open
`dotnet/runtime` issue asking for a direct reflection API that, as of this writing, still doesn't
exist) rather than an oversight specific to one feature.

## Basic: detecting an init-only property (C# 9 / .NET 5, November 2020)

```csharp
public class Order
{
    public string Id { get; init; } = string.Empty;
}
```

There is no `PropertyInfo.IsInitOnly`. An init accessor compiles to a setter whose return
parameter carries a `modreq` (required custom modifier) of
`System.Runtime.CompilerServices.IsExternalInit` — detect it by inspecting that modifier directly:

```csharp
static bool IsInitOnly(PropertyInfo property)
{
    MethodInfo? setMethod = property.SetMethod;
    if (setMethod is null)
    {
        return false;
    }

    return setMethod.ReturnParameter
        .GetRequiredCustomModifiers()
        .Any(m => m == typeof(System.Runtime.CompilerServices.IsExternalInit));
}
```

```csharp
bool idIsInitOnly = IsInitOnly(typeof(Order).GetProperty(nameof(Order.Id))!); // true
```

`GetRequiredCustomModifiers()` (available since
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md)'s baseline
— it's an ordinary `ParameterInfo` member, not new API) is the general-purpose tool; init-only
detection is simply one specific modifier this tier's feature happens to emit, not a case that
needed any new reflection surface.

## Basic: detecting a required member (C# 11 / .NET 7, November 2022)

```csharp
public class Order
{
    public required string CustomerId { get; init; }
}
```

Unlike init-only, `required` *does* have a dedicated, documented attribute to check —
`RequiredMemberAttribute`, applied by the compiler to both the member and its declaring type:

```csharp
bool customerIdIsRequired = typeof(Order)
    .GetProperty(nameof(Order.CustomerId))!
    .IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: false);
```

This follows the same `IsDefined`/`GetCustomAttribute` pattern as any other attribute inspection
from [csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md) — the
only thing to know going in is which attribute type to look for, since `required` reads like new
syntax but is, from reflection's point of view, an ordinary compiler-applied attribute.

## Advanced: what `GetMethods()` sees on a type with extension members (C# 14 / .NET 10, November 2025)

```csharp
public static class OrderExtensions
{
    extension(Order order)
    {
        public bool IsOverdue => order.DueDate < DateTime.UtcNow;
    }
}
```

Extension members (C# 14) do not add themselves to `typeof(Order).GetMethods()` — they're still
lowered to static members on `OrderExtensions`, the same way classic C# 3.0 extension methods
always have been, plus a compiler-generated nested marker type (shaped roughly like
`OrderExtensions.<>E__0<TInstance>` in current Roslyn output) that carries the extension block's
receiver type. Reflecting `typeof(OrderExtensions).GetMethods()` reveals the lowered
implementation — a static method named `get_IsOverdue` taking `Order` as its first parameter —
rather than anything resembling the `extension(Order order) { ... }` source syntax. Don't expect
`typeof(Order).GetProperty("IsOverdue")` to find it; extension members are never actual members of
the extended type from reflection's perspective, exactly as classic extension methods never were.

```csharp
PropertyInfo? found = typeof(Order).GetProperty("IsOverdue"); // null — not a member of Order
MethodInfo? lowered = typeof(OrderExtensions).GetMethod("get_IsOverdue"); // found here instead
```

## Fallback

`GetRequiredCustomModifiers()` and `IsDefined`/`GetCustomAttribute` are both C# 1.0-era reflection
calls — there's no earlier tier to fall back to for the *mechanism*; what changes per C# version is
only which modifier or attribute to look for, and only for code that targets a project actually
compiled with that version's compiler. A project below C# 9 has no init-only properties to detect
in the first place; below C# 11, no `required` members; below C# 14, no extension members — in each
case there's nothing to reflect because the source feature producing that IL shape doesn't exist
on an older compiler, not because the detection technique itself needs a newer reflection API.
