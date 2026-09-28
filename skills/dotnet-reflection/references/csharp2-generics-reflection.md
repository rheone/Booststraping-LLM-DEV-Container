# Generic Type and Method Reflection: `MakeGenericType`, `MakeGenericMethod`, `IsGenericType` (C# 2.0 / .NET Framework 2.0)

.NET Framework 2.0 shipped in November 2005 alongside Visual Studio 2005, and generics were its
headline addition — both as C# 2.0 syntax and as a matching set of `Type`/`MethodInfo` reflection
members needed to inspect and construct generic types and methods whose type arguments are only
known at runtime. This file assumes you already know what generics are; it covers only the
reflection surface that lets code discover and construct them dynamically, not generics
fundamentals themselves.

## Syntax

```csharp
Type openGeneric = typeof(Repository<>);
Type[] typeArguments = { typeof(Order) };
Type closedGeneric = openGeneric.MakeGenericType(typeArguments);

MethodInfo openMethod = typeof(Mapper).GetMethod("Convert");
MethodInfo closedMethod = openMethod.MakeGenericMethod(typeof(OrderDto));
```

## Basic use case: constructing a closed generic type at runtime

```csharp
Type repositoryType = typeof(Repository<>);
Type entityType = typeof(Order);

Type closedRepositoryType = repositoryType.MakeGenericType(entityType);
object repository = Activator.CreateInstance(closedRepositoryType);
```

This is the mechanism a generic dependency-injection container uses to satisfy a request for
`IRepository<Order>` when it only has the open generic `Repository<>` registered: look up the
open generic `Type`, call `MakeGenericType` with the requested type argument(s), then
`Activator.CreateInstance` the result — combining this tier with
[csharp1-reflection-fundamentals.md](csharp1-reflection-fundamentals.md)'s `Activator` usage.

## Advanced use case: invoking a generic method with a runtime-determined type argument

```csharp
public static class Mapper
{
    public static TDto Convert<TDto>(object source) where TDto : new()
    {
        var dto = new TDto();
        // mapping logic
        return dto;
    }
}
```

```csharp
Type dtoType = ResolveDtoTypeFor(entity.GetType());

MethodInfo openConvert = typeof(Mapper).GetMethod(nameof(Mapper.Convert));
MethodInfo closedConvert = openConvert.MakeGenericMethod(dtoType);

object dto = closedConvert.Invoke(obj: null, parameters: new[] { entity });
```

A generic method's `MethodInfo` from `GetMethod` is the *open* generic method definition —
`IsGenericMethodDefinition` is `true` on it — and calling it directly throws
`InvalidOperationException`. `MakeGenericMethod` closes it with concrete type arguments first,
exactly as `MakeGenericType` closes an open generic type; both follow the identical
open-definition-then-close pattern.

## Inspecting generic shape and constraints

```csharp
Type dictionaryType = typeof(Dictionary<string, Order>);

bool isGeneric = dictionaryType.IsGenericType;                 // true
bool isDefinition = dictionaryType.IsGenericTypeDefinition;     // false — it's closed
Type[] typeArgs = dictionaryType.GetGenericArguments();         // [string, Order]

Type openDictionary = dictionaryType.GetGenericTypeDefinition(); // Dictionary<,>
bool openIsDefinition = openDictionary.IsGenericTypeDefinition;  // true

Type constrainedParam = typeof(Repository<>).GetGenericArguments()[0];
Type[] constraints = constrainedParam.GetGenericParameterConstraints();
GenericParameterAttributes attrs = constrainedParam.GenericParameterAttributes; // new(), class, struct, etc.
```

`IsGenericTypeDefinition` distinguishes an *open* generic (`Dictionary<,>`, no type arguments
bound) from a *closed* one (`Dictionary<string, Order>`) — both have `IsGenericType == true`, so
code that needs to tell them apart (before calling `MakeGenericType`, which requires an open
definition) must check `IsGenericTypeDefinition`, not `IsGenericType`.

## Requirements and restrictions

- `MakeGenericType` throws `ArgumentException` if the number of supplied type arguments doesn't
  match the generic type's arity, or if a supplied argument violates a `where` constraint
  (e.g. missing a parameterless constructor for a `new()` constraint).
- `MakeGenericMethod` requires the `MethodInfo` it's called on to be a generic method definition
  (`IsGenericMethodDefinition == true`); calling it on an already-closed generic method throws
  `InvalidOperationException`.
- `GetMethod(string)` on a type with multiple generic-arity overloads of the same name (e.g.
  `Convert<T>` and `Convert<T1, T2>`) is ambiguous by name alone — use `GetMethods()` and filter by
  `GetGenericArguments().Length`, or pass a `Type[]` of parameter types alongside the name.
- Reflecting over `Nullable<T>` needs care: `typeof(int?).IsGenericType` is `true`
  (`Nullable<Int32>`), which surprises code that expects `int?` to look like a plain value type —
  check `Nullable.GetUnderlyingType(type)` (non-`null` result means it's a `Nullable<T>`) before
  treating a generic `Type` as a "real" generic.

## Fallback

Below C# 2.0 / .NET Framework 1.0–1.1, there is no generics — neither the language feature nor its
reflection surface (`MakeGenericType`, `MakeGenericMethod`, `IsGenericType`, and the rest did not
exist). Runtime-resolved type construction has to work against a fixed, non-generic base type or
interface instead (e.g. an `object`-typed or `ArrayList`-based repository rather than
`Repository<T>`), the same substitute pattern pre-generics C# code used everywhere generics are
common today; see
[csharp1-reflection-fundamentals.md](csharp1-reflection-fundamentals.md) for the non-generic
reflection primitives that substitute pattern still relies on.
