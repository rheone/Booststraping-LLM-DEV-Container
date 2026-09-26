# Named Options

Named options let you bind multiple distinct instances of the same options class — each identified
by a string name — instead of being limited to a single configured instance per type. Use this when
one options shape genuinely applies to several independently configured targets: several outbound
HTTP endpoints, several tenants, several storage accounts.

## Registering named instances

`Configure<TOptions>(name, ...)` binds a named instance; the unnamed overload is shorthand for the
name `Options.DefaultName` (an empty string):

```csharp
builder.Services.Configure<StorageOptions>("Primary",
    builder.Configuration.GetSection("Storage:Primary"));
builder.Services.Configure<StorageOptions>("Backup",
    builder.Configuration.GetSection("Storage:Backup"));
```

## Consuming named instances

All three accessor interfaces expose `.Get(name)` for retrieving a specific named instance;
`IOptionsMonitor<TOptions>.OnChange` also accepts a name filter:

```csharp
public sealed class BackupUploader(IOptionsMonitor<StorageOptions> monitor)
{
    public StorageOptions Backup => monitor.Get("Backup");
}
```

`IOptions<TOptions>.Value` always resolves the unnamed (`Options.DefaultName`) instance — it has no
way to select a name, since it exposes no `Get` method. If your consumer needs a named instance,
inject `IOptionsSnapshot<TOptions>` or `IOptionsMonitor<TOptions>` instead, even if you don't need
their reload behavior, purely for the `.Get(name)` method.

## When to reach for named options vs. separate types

Named options are the right tool when the *shape* is identical and only the *values* differ per
target — the `StorageOptions` example above. When the targets have genuinely different structure or
validation rules, define separate options classes instead of forcing them through one named-options
shape with optional/nullable properties that only apply to some names — that produces a class whose
valid-property-combinations aren't expressible in its own type, which named options doesn't fix and
arguably makes worse by hiding the divergence behind a shared type.
