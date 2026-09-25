# Reflection as a Test-Authoring Tool

This file is about *using reflection to write tests* — invoking private members under test,
asserting on a type's shape, and generating test data from a type's declared properties — not
about testing this skill's own syntax examples. See
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md) and
[csharp2-generics-reflection.md](../references/csharp2-generics-reflection.md) for the underlying
reflection calls these patterns build on.

## Basic: invoking a private method under test

```csharp
public class OrderProcessor
{
    private decimal ApplyDiscount(decimal total, decimal percentage) => total * (1 - percentage);
}
```

```csharp
[Fact]
public void ApplyDiscount_Test_ReducesTotalByPercentage()
{
    var processor = new OrderProcessor();
    MethodInfo applyDiscount = typeof(OrderProcessor).GetMethod(
        "ApplyDiscount", BindingFlags.NonPublic | BindingFlags.Instance)!;

    object? result = applyDiscount.Invoke(processor, new object[] { 100m, 0.1m });

    Assert.Equal(90m, result);
}
```

Prefer testing through the public API wherever one exists — reaching for private-member reflection
in a test is usually a sign the class under test should expose (or be split to expose) a seam that
makes the behavior testable directly. Reflection-invoked private-member tests are still a
legitimate, common fallback when the private logic is genuinely internal and refactoring the
production type solely to make it testable isn't warranted.

## Basic: asserting on a type's declared shape

```csharp
[Fact]
public void Order_Test_AllPropertiesHaveJsonPropertyNameAttribute()
{
    PropertyInfo[] properties = typeof(Order).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    Assert.All(properties, p =>
        Assert.True(p.IsDefined(typeof(JsonPropertyNameAttribute)),
            $"{p.Name} is missing [JsonPropertyName]."));
}
```

Shape assertions like this catch a class of mistake unit tests on individual values can't: a new
property added later without the attribute convention the rest of the type follows. This is the
same `IsDefined`/`GetCustomAttribute` mechanism from
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md), applied to
every member of a type in a loop rather than to one known member.

## Advanced: a reusable convention-checking assertion helper

```csharp
public static class TypeShapeAssertions
{
    public static void ShouldHaveParameterlessConstructor(this Type type)
    {
        ConstructorInfo? ctor = type.GetConstructor(Type.EmptyTypes);
        Assert.True(ctor is not null, $"{type.Name} has no parameterless constructor.");
    }

    public static void ShouldImplement<TInterface>(this Type type)
    {
        Assert.True(typeof(TInterface).IsAssignableFrom(type),
            $"{type.Name} does not implement {typeof(TInterface).Name}.");
    }
}
```

```csharp
[Theory]
[InlineData(typeof(OrderDto))]
[InlineData(typeof(CustomerDto))]
public void Dto_Test_FollowsSerializationConventions(Type dtoType)
{
    dtoType.ShouldHaveParameterlessConstructor();
    dtoType.ShouldImplement<IEquatable<object>>();
}
```

A generic `[Theory]` driven by `typeof(...)` arguments, paired with reusable `Type`-shape
assertion extensions, checks the same convention across every DTO in the codebase without a
per-type test method — the reflection cost here is a non-issue since shape assertions run once
per test, not on a hot path (see
[reflection-performance-and-caching.md](reflection-performance-and-caching.md) for when caching
actually starts to matter).

## Advanced: generating theory data from a type's declared members

```csharp
public static class OrderTestData
{
    public static IEnumerable<object[]> NumericProperties =>
        typeof(Order)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(int))
            .Select(p => new object[] { p });
}
```

```csharp
[Theory]
[MemberData(nameof(OrderTestData.NumericProperties), MemberType = typeof(OrderTestData))]
public void NumericProperty_Test_DefaultsToZero(PropertyInfo property)
{
    var order = new Order();
    object? value = property.GetValue(order);

    Assert.Equal(0, System.Convert.ToInt32(value));
}
```

Reflecting over a type's own properties to *generate* the theory data (rather than hand-listing
each property name) keeps the test automatically covering every numeric property the class ever
grows — a new property is picked up by the reflection-driven data source without editing the test.

## Fallback

Every example above is C# 1.0-era reflection (`GetMethod`, `GetProperties`, `Invoke`,
`GetValue`, `IsDefined`) plus ordinary xUnit `[Fact]`/`[Theory]` — none of it needs any tier past
[csharp1-reflection-fundamentals.md](../references/csharp1-reflection-fundamentals.md), so there is
no older-target fallback to state.
