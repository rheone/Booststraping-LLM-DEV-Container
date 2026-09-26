# Testing

Code that consumes options depends on an accessor interface (`IOptions<T>`, `IOptionsSnapshot<T>`,
or `IOptionsMonitor<T>`), not on the DI container or a configuration source — test that dependency
directly rather than standing up real configuration binding in most tests.

## Unit testing a consumer with `Options.Create`

`Microsoft.Extensions.Options.Options.Create(value)` builds an `IOptions<TOptions>` wrapping a value
you construct by hand — no configuration, no DI container:

```csharp
[Fact]
public async Task SendAsync_UsesConfiguredHost()
{
    var options = Options.Create(new SmtpOptions { Host = "smtp.test", Port = 587, UseSsl = true });
    var sender = new EmailSender(options);

    await sender.SendAsync("a@b.com", "subject", "body");

    // assert on the sender's observable behavior (e.g. a fake transport it wrote to)
}
```

## Faking IOptionsMonitor for change-reaction tests

`IOptionsMonitor<TOptions>` has no built-in test constructor; fake it directly (with a mocking
library, or a small hand-written stub) when a test needs to simulate a configuration change firing
mid-test:

```csharp
public sealed class FakeOptionsMonitor<T>(T initialValue) : IOptionsMonitor<T>
{
    private T _current = initialValue;
    private Action<T, string?>? _listener;

    public T CurrentValue => _current;
    public T Get(string? name) => _current;

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _listener = listener;
        return new NoopDisposable();
    }

    public void Change(T newValue)
    {
        _current = newValue;
        _listener?.Invoke(newValue, null);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
```

```csharp
[Fact]
public void OnChange_UpdatesCurrentLimit()
{
    var monitor = new FakeOptionsMonitor<RateLimiterOptions>(new RateLimiterOptions { RequestsPerMinute = 10 });
    var watcher = new RateLimiterOptionsWatcher(monitor);

    monitor.Change(new RateLimiterOptions { RequestsPerMinute = 50 });

    watcher.CurrentLimit.Should().Be(50);
}
```

## Integration testing the binding and validation pipeline

Reserve this for tests that specifically want to verify configuration binding, `PostConfigure`
ordering, or `IValidateOptions<T>` wiring end-to-end — build a real `ServiceCollection` with an
in-memory configuration source rather than a file on disk:

```csharp
[Fact]
public void ValidateOnStart_ThrowsForInvalidConfiguration()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "",
            ["Smtp:Port"] = "25",
        })
        .Build();

    var services = new ServiceCollection();
    services.AddOptions<SmtpOptions>()
        .Bind(configuration.GetSection("Smtp"))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    services.AddHostedService<OptionsValidationTrigger>(); // or resolve IOptions<SmtpOptions>.Value directly

    using var provider = services.BuildServiceProvider();

    var act = () => provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

    act.Should().Throw<OptionsValidationException>();
}
```

## Choosing the right layer

| What you're verifying | Test approach |
| --- | --- |
| A consumer's logic given a fixed set of option values | `Options.Create(...)` wrapping a hand-built instance |
| A consumer's reaction to a configuration change at runtime | A fake `IOptionsMonitor<T>` with a manually triggered `OnChange` |
| Binding, `PostConfigure` ordering, or validator wiring together | A real `ServiceCollection` with an in-memory configuration source |

Most tests belong in the first row — options consumers are ordinary classes taking a value, and
testing them doesn't require exercising the binding pipeline at all.
