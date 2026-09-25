# Naming policies

A `JsonNamingPolicy` transforms C# member names into JSON property names on write, and is used to
match JSON property names back to members on read (unless a member has an explicit
`[JsonPropertyName]`, which always wins — see `attributes.md`).

## Built-in policies

Set via `JsonSerializerOptions.PropertyNamingPolicy` (property names) or
`JsonSerializerOptions.DictionaryKeyPolicy` (`Dictionary<string, TValue>` keys are a separate
setting from property names).

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
```

| Policy | `TempCelsius` becomes | Since |
| --- | --- | --- |
| `JsonNamingPolicy.CamelCase` | `tempCelsius` | .NET Core 3.0 |
| `JsonNamingPolicy.SnakeCaseLower` | `temp_celsius` | .NET 8 |
| `JsonNamingPolicy.SnakeCaseUpper` | `TEMP_CELSIUS` | .NET 8 |
| `JsonNamingPolicy.KebabCaseLower` | `temp-celsius` | .NET 8 |
| `JsonNamingPolicy.KebabCaseUpper` | `TEMP-CELSIUS` | .NET 8 |

There is no built-in `PascalCase` policy — omit `PropertyNamingPolicy` entirely (the default
behavior is to use the C# member name exactly as declared, which is typically already
`PascalCase`).

`JsonSerializerOptions.Web` is a ready-made preset using `CamelCase` plus case-insensitive
property matching — the same defaults ASP.NET Core's minimal APIs apply — worth reaching for
instead of hand-assembling those two settings for a typical JSON API.

## Case sensitivity is a separate setting

A naming policy only controls the name **produced on write**. Matching an **incoming** JSON
property name back to a member during deserialization is governed independently by
`JsonSerializerOptions.PropertyNameCaseInsensitive` (default `false`). Setting
`PropertyNamingPolicy = JsonNamingPolicy.CamelCase` does not, by itself, make deserialization
accept `TempCelsius` for a `camelCase`-configured type — see `pitfalls.md`.

## Writing a custom naming policy

Derive from `JsonNamingPolicy` and override `ConvertName`:

```csharp
public sealed class ScreamingSnakeCasePolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) =>
        string.Concat(
            name.Select((ch, i) =>
                i > 0 && char.IsUpper(ch) ? "_" + ch : ch.ToString()))
            .ToUpperInvariant();
}
```

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = new ScreamingSnakeCasePolicy(),
};
```

`ConvertName` receives the exact C# member name (or the string an explicit `[JsonPropertyName]`
would have overridden, if one is present — but as noted above, `[JsonPropertyName]` bypasses the
policy entirely for that member) and must return the JSON name to use. A custom policy instance,
like a `JsonSerializerOptions` instance, should be created once and reused, not allocated per
call.

## Per-type override

A naming policy set on `JsonSerializerOptions` applies to every type serialized with those
options. To use a different convention for one specific type while keeping a project-wide policy
for everything else, annotate individual members with `[JsonPropertyName]` (see `attributes.md`)
rather than trying to scope a `JsonNamingPolicy` to one type — the policy itself has no per-type
targeting mechanism.
