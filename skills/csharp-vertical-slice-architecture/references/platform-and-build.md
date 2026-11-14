# Platform and build

## Registration: explicit vs. scanned

| Style | Shape | Trim/AOT | Notes |
| --- | --- | --- | --- |
| Explicit per area | `AddOrdersFeature()` lists handlers; `MapOrders()` lists endpoints | Safe | Default for new projects; a forgotten registration fails the slice's integration test |
| Assembly scanning | Reflection over types implementing `IHandler<,>` or `IEndpoint` | Unsafe without annotations | Convenient; hides registration; breaks under trimming |
| Source-generated | A generator emits registration code at build time | Safe | Use the repo's generator or a mediator library that generates |

Keep each area's registration in the area folder. `Program.cs` calls one method per area.

```csharp
builder.Services.AddOrdersFeature().AddBillingFeature();
app.MapOrders();
app.MapBilling();
```

## Native AOT and trimming

Native AOT "can't use unbounded reflection". Libraries that discover types or generate code at runtime
need source-generated or annotated alternatives
(<https://learn.microsoft.com/aspnet/core/fundamentals/native-aot>).

| Piece | AOT status |
| --- | --- |
| Minimal APIs | Partial support through the Request Delegate Generator; the `webapiaot` template enables it |
| MVC, Razor Pages | Not supported, so a Layout C (Razor Pages) app cannot publish with Native AOT |
| JSON | Needs a `JsonSerializerContext` for every request and response type |
| Reflection mediators (runtime `MakeGenericType`) | Not claimed; expect failures |
| Source-generated mediator | Supported (claimed by `Mediator`) |
| DataAnnotations | Some attributes warn under trimming (`MinLength` on a non-`ICollection` type reads `Count` by reflection: IL2026) |
| Options validation | Use the `[OptionsValidator]` source generator |

When the project publishes with AOT (`<PublishAot>true</PublishAot>`), add every slice's request and
response types to the JSON context, and treat trim warnings as errors in CI.

File-based apps (`dotnet run app.cs`) default to `PublishAot=true`, so reflection-based JSON is disabled
in them. A script that serializes JSON needs a `JsonSerializerContext` or `#:property PublishAot=false`.

## Target frameworks and language versions

- .NET 10 is LTS; .NET 11 is STS (both GA; .NET 11.0.0 on 2026-11-10).
- C# 15 is the default for `net11.0`. Unions and `closed` classes need no preview flag there. Treat both
  as .NET 11-only; on `net10.0`, use the abstract-record fallback in
  [results-and-errors.md](results-and-errors.md).
- In a multi-target project, keep result types to the lowest common language version, or split them with
  `#if NET11_0_OR_GREATER` only when the user asks for it.

## Migrations ownership

- One migrations history per database (or per module's schema in Layout E). Migrations never live inside
  slice folders.
- Name a migration for the schema change (`AddOrderCancelledAt`), not the slice.
- Two slices developed in parallel can create conflicting migrations; resolve by regenerating the later
  one on top of the earlier, not by editing generated snapshots by hand.
- For NHibernate, use the repo's schema tool (migrations library or scripts). Avoid automatic schema
  update in production.

## Build-time enforcement

- An analyzer such as NsDepCop turns illegal namespace dependencies into build errors
  (`IssueKind IllegalDependency Severity="Error"`), which catches violations before tests run.
- `internal` types plus separate projects let the compiler enforce boundaries in Layouts D, E, and F.
- Warnings as errors for nullable reference types keep request and response records honest.
- CA1068, CA2016, and CA2007 as warnings in `src/` enforce the async and cancellation rules. The
  `.editorconfig` block is in [slice-anatomy.md](slice-anatomy.md#async-and-cancellation).
