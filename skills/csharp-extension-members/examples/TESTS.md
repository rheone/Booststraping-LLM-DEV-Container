# Test Examples

xUnit-style tests for each syntax tier, including generic cases. See the
[xunit-csharp skill](../../csharp-test-sweep/skills/xunit-csharp/SKILL.md) in this repository for
xUnit conventions (`TheoryData<T>`, naming, `Assert.Multiple`) beyond what's shown here.

## Classic extension methods (C# 3.0+) — basic

```csharp
public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("hi", false)]
    public void IsNullOrBlank_Test(string? value, bool expected)
    {
        Assert.Equal(expected, value.IsNullOrBlank());
    }

    [Fact]
    public void Truncate_Test_LongerThanMax_AppendsEllipsis()
    {
        Assert.Equal("hello wo…", "hello world".Truncate(8));
    }

    [Fact]
    public void Truncate_Test_ShorterThanMax_ReturnsUnchanged()
    {
        Assert.Equal("hi", "hi".Truncate(8));
    }
}
```

## Generic classic extension methods

```csharp
public class CollectionExtensionsTests
{
    public static TheoryData<int[], bool> IsEmpty_Test_Data => new()
    {
        { Array.Empty<int>(), true },
        { new[] { 1 }, false },
    };

    [Theory]
    [MemberData(nameof(IsEmpty_Test_Data))]
    public void IsEmpty_Test(int[] source, bool expected)
    {
        Assert.Equal(expected, source.IsEmpty());
    }

    [Fact]
    public void ToDictionarySafe_Test_DuplicateKey_LastWriteWins()
    {
        var pairs = new[]
        {
            KeyValuePair.Create("a", 1),
            KeyValuePair.Create("a", 2),
        };

        var result = pairs.ToDictionarySafe();

        Assert.Equal(2, result["a"]);
    }
}
```

## C# 14 extension members (.NET 10) — properties, statics, operators

```csharp
#if NET10_0_OR_GREATER
public class RectangleExtensionsTests
{
    [Fact]
    public void Area_Test()
    {
        var rect = new Rectangle(Width: 3, Height: 4);
        Assert.Equal(12, rect.Area);
    }

    [Theory]
    [InlineData(3, 3, true)]
    [InlineData(3, 4, false)]
    public void IsSquare_Test(double width, double height, bool expected)
    {
        Assert.Equal(expected, new Rectangle(width, height).IsSquare);
    }
}

public class MoneyExtensionsTests
{
    [Fact]
    public void OperatorPlus_Test_SameCurrency_Sums()
    {
        var total = new Money(10m, "USD") + new Money(5m, "USD");
        Assert.Equal(15m, total.Amount);
    }

    [Fact]
    public void OperatorPlus_Test_DifferentCurrency_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Money(10m, "USD") + new Money(5m, "EUR"));
    }
}
#endif
```

## Generic extension blocks (C# 14) — constrained max/min

```csharp
#if NET10_0_OR_GREATER
public class ReadOnlyListExtensionsTests
{
    public static TheoryData<int[], int?> Max_Test_Data => new()
    {
        { Array.Empty<int>(), null },
        { new[] { 3, 1, 2 }, 3 },
    };

    [Theory]
    [MemberData(nameof(Max_Test_Data))]
    public void Max_Test(int[] items, int? expected)
    {
        Assert.Equal(expected, items.Max());
    }

    [Fact]
    public void IsSorted_Test_Ascending_ReturnsTrue()
    {
        Assert.True(new[] { 1, 2, 3 }.IsSorted);
    }

    [Fact]
    public void IsSorted_Test_Unordered_ReturnsFalse()
    {
        Assert.False(new[] { 3, 1, 2 }.IsSorted);
    }
}
#endif
```

## C# 15 extension indexers (.NET 11)

```csharp
#if NET11_0_OR_GREATER
public class GarageExtensionsTests
{
    [Fact]
    public void Indexer_Test_Get_ReturnsParkedCar()
    {
        var garage = new Garage();
        var car = new Car("ABC-123");
        garage.ParkCarInBay(2, car);

        Assert.Equal(car, garage[2]);
    }

    [Fact]
    public void Indexer_Test_Set_ParksCarInBay()
    {
        var garage = new Garage();
        var car = new Car("XYZ-789");

        garage[1] = car;

        Assert.Equal(car, garage.GetCarInBay(1));
    }
}
#endif
```

## Notes

- `NET10_0_OR_GREATER` / `NET11_0_OR_GREATER` are target-framework preprocessor symbols the SDK
  defines automatically — not `LangVersion` symbols. If a project targets `net10.0` with
  `LangVersion` pinned below 14, guard with a custom symbol instead (define it yourself via
  `<DefineConstants>` in the `.csproj`), since the SDK does not synthesize a `LangVersion`-based
  symbol.
- Tests for the pre-C#3 fallback pattern
  ([pre-csharp3-no-extensions.md](../references/pre-csharp3-no-extensions.md)) look identical to
  the generic classic tests above but call `StringUtil.IsNullOrBlank(value)` /
  `CollectionUtil.IsEmpty(source)` instead of extension syntax — same assertions, different call
  shape.
