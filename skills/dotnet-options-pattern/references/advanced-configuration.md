# PostConfigure Ordering and IOptionsFactory\<TOptions\>

## Configure vs. PostConfigure ordering

`Configure<TOptions>` delegates run in the order they're registered. `PostConfigure<TOptions>`
delegates always run **after every** `Configure<TOptions>` delegate, regardless of where
`PostConfigure` itself is registered relative to them:

```csharp
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

builder.Services.Configure<SmtpOptions>(options =>
{
    options.Port = 587; // runs after the configuration bind above, in registration order
});

builder.Services.PostConfigure<SmtpOptions>(options =>
{
    // Runs after both Configure calls above, no matter where this line sits in Program.cs.
    if (options.UseSsl && options.Port == 25)
    {
        options.Port = 465;
    }
});
```

Reach for `PostConfigure` when you need a value to win regardless of registration order elsewhere in
the app — a final normalization or override step that shouldn't depend on where in `Program.cs`
someone else's `Configure` call happens to sit. `PostConfigureAll<TOptions>` applies the same
post-configuration to every named instance of the type, unnamed and named alike.

## IOptionsFactory\<TOptions\>: how instances actually get built

`IOptionsFactory<TOptions>` is the low-level service that `IOptions<TOptions>`,
`IOptionsSnapshot<TOptions>`, and `IOptionsMonitor<TOptions>` all ultimately call into to produce an
options instance: it runs the registered `Configure` delegates, then the `PostConfigure` delegates,
then any registered `IValidateOptions<TOptions>` validators, for a given name.

You rarely need to touch this directly — it exists as an extension point for scenarios the standard
`Configure`/`PostConfigure`/`Validate` surface doesn't cover, such as constructing an options
instance from a source `Configure`/`PostConfigure` can't express (a non-DI-friendly external SDK
object) or intercepting every instance creation across all names for a cross-cutting concern (e.g.
telemetry on every options materialization). Implement `IOptionsFactory<TOptions>` and register it
to replace the default:

```csharp
public sealed class InstrumentedOptionsFactory<TOptions>(
    IEnumerable<IConfigureOptions<TOptions>> setups,
    IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
    IEnumerable<IValidateOptions<TOptions>> validations,
    ILogger<InstrumentedOptionsFactory<TOptions>> logger)
    : OptionsFactory<TOptions>(setups, postConfigures, validations)
    where TOptions : class
{
    public override TOptions Create(string name)
    {
        logger.LogDebug("Materializing options {Type} (name: {Name})", typeof(TOptions).Name, name);
        return base.Create(name);
    }
}

builder.Services.AddSingleton(typeof(IOptionsFactory<>), typeof(InstrumentedOptionsFactory<>));
```

Deriving from the framework's `OptionsFactory<TOptions>` (rather than implementing
`IOptionsFactory<TOptions>` from scratch) keeps the standard configure/post-configure/validate
pipeline intact while letting you wrap it — implementing the interface directly means reproducing
that pipeline yourself, which is rarely worth it.
