# Testing Code Built on Decorator

A decorator's whole job is to add behavior around a call to an inner instance — so testing one means
substituting a controllable fake for that inner instance, and asserting on exactly what the
decorator added: did it forward the call, did it add its own side effect, did it change the result,
did it skip forwarding under some condition.

## Testing that a decorator forwards correctly

```csharp
public class CachingReportGeneratorTests
{
    [Fact]
    public void Generate_FirstCall_ForwardsToInner()
    {
        var inner = new RecordingReportGenerator(resultToReturn: "report-text");
        var decorator = new CachingReportGenerator(inner);
        var data = new ReportData(Id: 1);

        var result = decorator.Generate(data);

        Assert.Equal("report-text", result);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public void Generate_SecondCallForSameData_ReturnsCachedResultWithoutCallingInnerAgain()
    {
        var inner = new RecordingReportGenerator(resultToReturn: "report-text");
        var decorator = new CachingReportGenerator(inner);
        var data = new ReportData(Id: 1);

        decorator.Generate(data);
        var secondResult = decorator.Generate(data);

        Assert.Equal("report-text", secondResult);
        Assert.Equal(1, inner.CallCount); // still 1 — the second call hit the cache
    }

    private sealed class RecordingReportGenerator : IReportGenerator
    {
        private readonly string _resultToReturn;
        public int CallCount { get; private set; }

        public RecordingReportGenerator(string resultToReturn) => _resultToReturn = resultToReturn;

        public string Generate(ReportData data)
        {
            CallCount++;
            return _resultToReturn;
        }
    }
}
```

`RecordingReportGenerator` — a hand-written fake that counts calls and returns a fixed value — is
usually all a decorator test needs. The behavior under test (does the decorator skip forwarding on a
cache hit, does it forward exactly once per uncached call) is entirely about call counts and return
values, which a hand-written fake exposes plainly without a mocking framework's setup/verify syntax.

## Testing a decorator that reacts to failure

```csharp
[Fact]
public void Generate_InnerThrows_RetriesUpToMaxAttempts()
{
    var inner = new ThrowingThenSucceedingGenerator(failuresBeforeSuccess: 2);
    var decorator = new RetryReportGenerator(inner, maxAttempts: 3);

    var result = decorator.Generate(new ReportData(Id: 1));

    Assert.Equal("success", result);
    Assert.Equal(3, inner.CallCount); // 2 failures + 1 success
}

[Fact]
public void Generate_InnerAlwaysThrows_StopsAfterMaxAttemptsAndRethrows()
{
    var inner = new AlwaysThrowingGenerator();
    var decorator = new RetryReportGenerator(inner, maxAttempts: 3);

    Assert.Throws<ReportGenerationException>(() => decorator.Generate(new ReportData(Id: 1)));
    Assert.Equal(3, inner.CallCount);
}
```

A retry or circuit-breaker decorator is exactly the kind of behavior a fake with configurable,
call-count-dependent behavior (`ThrowingThenSucceedingGenerator`) tests far more directly than a
mocking framework's sequential-setup syntax — the fake's own state (`failuresBeforeSuccess`) *is*
the test scenario.

## Testing a chain of decorators together

Most decorator behavior is fully covered by testing each decorator in isolation against a fake inner
instance. Reserve a test of the assembled chain (`Logging(Retry(Caching(Basic)))`) for asserting on
the emergent behavior that only exists because of the specific composition order — for example, that
a cache hit really does skip the retry layer entirely, which no single decorator's isolated test can
demonstrate since it depends on which layer sits outside which.

```csharp
[Fact]
public void Chain_CachedResult_NeverReachesRetryOrBasicLayer()
{
    var basic = new RecordingReportGenerator(resultToReturn: "text");
    var caching = new CachingReportGenerator(basic);
    var retry = new RetryReportGenerator(caching, maxAttempts: 3);

    retry.Generate(new ReportData(Id: 1));
    retry.Generate(new ReportData(Id: 1)); // second call should hit the cache

    Assert.Equal(1, basic.CallCount);
}
```
