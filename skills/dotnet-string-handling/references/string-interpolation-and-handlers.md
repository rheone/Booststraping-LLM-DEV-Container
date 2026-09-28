# String Interpolation and Handlers

Ordinary `$"..."` interpolated strings compile down to a call through
`System.Runtime.CompilerServices.DefaultInterpolatedStringHandler`, not to a naive chain of
`string.Format`/concatenation calls — this is what makes the C# 10 / .NET 6 interpolated string
handler mechanism relevant even to code that never writes a custom handler: an ordinary `$"..."`
already benefits from it.

## Ordinary interpolation

```csharp
string message = $"User {userId} logged in at {timestamp:O}";
```

The compiler lowers this into calls against `DefaultInterpolatedStringHandler`'s
`AppendLiteral`/`AppendFormatted` methods, writing directly into a pooled internal buffer rather
than boxing each interpolated value and routing it through `string.Format`'s composite-format
parsing — meaning an ordinary interpolated string is already reasonably efficient without any
special effort, as long as the interpolated string itself is only actually evaluated (not, for
example, passed as a `string` parameter to a logging call that might skip formatting it entirely —
see the logging example below).

## Writing a custom interpolated string handler

A custom handler lets code accepting an interpolated string as a parameter control formatting, and
critically, decide whether to format the string's holes at all — this is how structured logging
APIs avoid the cost of formatting a message that a disabled log level will just discard. Apply
`[InterpolatedStringHandler]` to a type with a constructor taking `int literalLength, int
formattedCount` (plus any additional context parameters needed) and `AppendLiteral`/
`AppendFormatted` methods:

```csharp
[InterpolatedStringHandler]
public ref struct LogInterpolatedStringHandler
{
    private readonly StringBuilder? _builder;
    public bool IsEnabled { get; }

    public LogInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, LogLevel level)
    {
        IsEnabled = logger.IsEnabled(level);
        _builder = IsEnabled ? new StringBuilder(literalLength) : null;
    }

    public void AppendLiteral(string s) => _builder?.Append(s);

    public void AppendFormatted<T>(T value) => _builder?.Append(value);

    public override string ToString() => _builder?.ToString() ?? string.Empty;
}

public static void LogDebug(this ILogger logger, [InterpolatedStringHandlerArgument("logger", "")] ref LogInterpolatedStringHandler message)
{
    if (message.IsEnabled)
    {
        logger.Write(message.ToString());
    }
}
```

```csharp
logger.LogDebug($"Processing order {order.Id} with {order.Items.Count} items");
```

When `logger.IsEnabled(LogLevel.Debug)` is `false`, the handler's constructor sets `IsEnabled` to
`false` and every `AppendFormatted` call becomes a no-op — `order.Id` and `order.Items.Count` are
still evaluated as method-call arguments (C# always evaluates arguments before the call), but the
*string formatting* work (padding, culture-aware number formatting, `ToString()` calls on each
interpolated value) never happens. This is the entire value proposition of a custom handler: for a
handler wired to something conditionally expensive, skip the formatting work, not skip evaluating
the interpolated expressions themselves.

## `[InterpolatedStringHandlerArgument]`

This attribute (used above) tells the compiler which of the *calling method's* other parameters to
forward into the handler's constructor — `"logger"` and `""` (the receiver, i.e. `logger` itself
for an extension method) let the handler's constructor see the `ILogger`/`LogLevel` context needed
to decide whether logging is enabled, without the caller having to pass anything beyond the
interpolated string itself.

## When to reach for a custom handler vs. `DefaultInterpolatedStringHandler`

Write a custom handler only when a method genuinely needs to intercept formatting conditionally
(logging, assertion messages that shouldn't format unless the assertion fails,
`StringBuilder.Append`-style buffer targeting). For a plain method parameter that always needs the
fully formatted string, take a `string` parameter and let the compiler's default lowering handle
it — introducing a custom handler purely for its own sake, with no conditional-skip behavior to
gain, adds a maintenance surface with no payoff.
