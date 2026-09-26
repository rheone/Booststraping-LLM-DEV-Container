# Core Concepts

The options pattern binds a section of configuration (`IConfiguration` — from `appsettings.json`,
environment variables, command-line arguments, or any other configuration provider) to a
strongly-typed class, then hands that class to consumers through DI instead of having consumers
read raw configuration keys directly.

## Defining an options class

An options class is a plain class with public settable properties (or `init` properties). It needs
no base class or interface:

```csharp
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public bool UseSsl { get; set; }
}
```

Declaring `SectionName` as a `const` on the class itself is a convention, not a requirement — it
keeps the configuration key next to the type it configures instead of scattered as a string literal
at every call site.

## Binding configuration to the class

`Configure<TOptions>` binds a configuration section by property-name matching (case-insensitive)
and registers the result for injection:

```csharp
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
```

For `appsettings.json`:

```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UseSsl": true
  }
}
```

You can also configure an options class imperatively, without binding to `IConfiguration` at all —
useful for options computed from other services or environment state:

```csharp
builder.Services.Configure<SmtpOptions>(options =>
{
    options.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "localhost";
});
```

Calling `Configure<TOptions>` more than once for the same `TOptions` composes — each registered
delegate runs in registration order, later ones overwriting properties earlier ones set. This is
how you layer a configuration-bound base with an environment-specific override.

## Consuming options

Inject one of `IOptions<TOptions>`, `IOptionsSnapshot<TOptions>`, or `IOptionsMonitor<TOptions>` —
never the options class itself — and read `.Value` (or, for the monitor, `.CurrentValue`):

```csharp
public sealed class EmailSender(IOptions<SmtpOptions> options)
{
    private readonly SmtpOptions _options = options.Value;

    public Task SendAsync(string to, string subject, string body)
    {
        // use _options.Host, _options.Port, _options.UseSsl
        return Task.CompletedTask;
    }
}
```

Which of the three accessor interfaces to inject depends on your service's lifetime and whether you
need to observe configuration changes after startup — see
[accessor-types.md](accessor-types.md).

## Binding gotchas

- **Property names must match configuration keys** (case-insensitively) for the default binder to
  populate them; a typo in either the class or the JSON silently leaves the property at its default
  value rather than throwing.
- **Collections bind by index-shaped keys** (`"Servers:0:Host"`, `"Servers:1:Host"`) when the source
  is a flat provider like environment variables; a JSON array binds naturally.
- **`Configure<TOptions>` without a section argument** binds the entire root configuration, which is
  rarely what you want — always scope with `GetSection(...)` unless the options class genuinely maps
  to the whole configuration tree.
