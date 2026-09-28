# Extension-Method Fluent APIs

Extension methods (C# 3.0) add a fluent call to a type without that type having to declare the
method itself — the call site reads exactly like an instance method, but the implementation lives
in a static class the chained type never references.

## Basic: a fluent call added to a type you don't own

```csharp
public static class StringBuilderExtensions
{
    public static StringBuilder AppendLineIf(this StringBuilder builder, bool condition, string line)
    {
        return condition ? builder.AppendLine(line) : builder;
    }
}
```

```csharp
var report = new StringBuilder()
    .AppendLine("Order Summary")
    .AppendLineIf(includeNotes, "Notes: customer requested gift wrap");
```

`AppendLineIf` chains onto `StringBuilder` — a BCL type this code doesn't own and can't add members
to directly — exactly as if it were declared on the class itself, because extension method
resolution rewrites `builder.AppendLineIf(...)` into `StringBuilderExtensions.AppendLineIf(builder,
...)` at compile time. The chain keeps working with any other `StringBuilder` member on either side
of the call.

## Advanced: splitting a large fluent surface across files or assemblies

```csharp
// Core.cs
public sealed class QueryBuilder
{
    public List<string> Clauses { get; } = new();
    public QueryBuilder Where(string clause) { Clauses.Add(clause); return this; }
}

// Pagination.cs — a separate file, or a separate NuGet package, adding fluent calls to the same type.
public static class QueryBuilderPagingExtensions
{
    public static QueryBuilder Paginate(this QueryBuilder builder, int page, int pageSize)
    {
        builder.Clauses.Add($"LIMIT {pageSize} OFFSET {page * pageSize}");
        return builder;
    }
}
```

```csharp
var query = new QueryBuilder()
    .Where("status = 'active'")
    .Paginate(page: 2, pageSize: 25);
```

`Paginate` chains onto `QueryBuilder` from an entirely separate static class — useful when an
optional feature area (paging, filtering, sorting) should ship independently of the core builder, or
when a consuming project wants to add its own fluent calls onto a type it doesn't control the source
of. Every extension method still needs the type it extends to expose enough (a public field,
property, or method) for the extension to actually do its job — `Paginate` reaches `Clauses`
because it's a public property, not a private field.

## Requirements and restrictions

- An extension method needs the extended type's `this` parameter to be its exact type (or a
  compatible interface/base type) — it can't extend a `sealed` type's private state, only whatever
  that type already exposes publicly.
- Extension methods resolve based on which namespaces are imported (`using`) at the call site — a
  fluent call added this way silently disappears (falls back to a compile error, "no member found")
  if the extension class's namespace isn't imported where the chain is written, unlike an instance
  member, which is always visible once the type itself is.
- An extension method can't override or hide an instance method of the same name and signature — if
  the extended type ever adds a real instance member with that name, the instance member wins at
  every call site, silently changing which implementation runs.
