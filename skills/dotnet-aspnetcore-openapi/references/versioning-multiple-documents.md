# Versioning Multiple OpenAPI Documents

An app can generate more than one OpenAPI document from the same set of endpoints — the standard
shape for an API that exposes multiple versions (`v1`, `v2`) and wants a separate, accurate
document per version rather than one document mixing every version's routes together.

## Naming a document

`AddOpenApi(documentName)` registers a named document; `MapOpenApi()` serves whichever documents
have been registered, each at its own path:

```csharp
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

var app = builder.Build();
app.MapOpenApi(); // serves /openapi/v1.json and /openapi/v2.json
```

Each `AddOpenApi(name)` call gets its own independent set of options and transformers — a
transformer registered against `"v1"`'s options does not run for `"v2"`'s document unless
registered separately for it too.

## Restricting which endpoints appear in which document

Tag endpoints with `.WithGroupName(...)` and match that group name in each document's options via
`ShouldInclude`:

```csharp
app.MapGet("/v1/orders/{id}", GetOrderV1).WithGroupName("v1");
app.MapGet("/v2/orders/{id}", GetOrderV2).WithGroupName("v2");

builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
});

builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
});
```

Without a `ShouldInclude` filter, every named document includes every mapped endpoint regardless of
group — the filter is what actually splits endpoints between documents; naming the document alone
does not.

## Combining with ASP.NET Core's API versioning route groups

When routes are already organized into versioned `RouteGroupBuilder` groups (a common pattern for
URL-segment or header-based API versioning), apply `.WithGroupName(...)` at the group level so
every endpoint mapped through that group inherits the same group name automatically, rather than
tagging each individual endpoint:

```csharp
var v1 = app.MapGroup("/v1").WithGroupName("v1");
v1.MapGet("/orders/{id}", GetOrderV1);
v1.MapGet("/orders", ListOrdersV1);

var v2 = app.MapGroup("/v2").WithGroupName("v2");
v2.MapGet("/orders/{id}", GetOrderV2);
```

## Per-document transformers and metadata

Give each version its own `Info.Title`/`Info.Version` via a document transformer scoped to that
document's `AddOpenApi` call, so consumers of `/openapi/v1.json` and `/openapi/v2.json` see
distinct, correctly labeled documents rather than two documents that differ only in included
routes:

```csharp
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Orders API";
        document.Info.Version = "v2";
        return Task.CompletedTask;
    });
});
```
