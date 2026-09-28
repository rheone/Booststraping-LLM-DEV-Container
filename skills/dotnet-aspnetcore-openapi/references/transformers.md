# Document, Operation, and Schema Transformers

Transformers are the extension point for modifying the generated OpenAPI document beyond what
reflection over your endpoints produces automatically — adding metadata, security schemes, or
correcting an inferred schema that doesn't match the real contract.

## Document transformers: whole-document changes

`IOpenApiDocumentTransformer` runs once per document generation and can modify anything at the
document level (info, servers, security schemes, or anything nested inside):

```csharp
public sealed class SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        return Task.CompletedTask;
    }
}

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<SecuritySchemeTransformer>();
});
```

Registering it as a DI-activated class (via `AddDocumentTransformer<T>()`) lets the transformer
take constructor dependencies; register an instance or a delegate directly
(`AddDocumentTransformer((document, context, ct) => { ... })`) for something simple enough not to
need DI.

## Operation transformers: per-endpoint changes

`IOpenApiOperationTransformer` runs once per operation (once per HTTP method+path combination) and
is the right level for anything that varies per endpoint — adding a response example, marking an
operation deprecated, adding an operation-specific parameter description:

```csharp
public sealed class DeprecationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<ObsoleteAttribute>().Any())
        {
            operation.Deprecated = true;
        }
        return Task.CompletedTask;
    }
}

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<DeprecationTransformer>();
});
```

`context` exposes the endpoint's metadata, HTTP method, and route pattern — inspect it to decide
whether/how to transform the specific operation, rather than trying to filter inside a document
transformer by re-deriving which operation is which.

## Schema transformers: per-type changes

`IOpenApiSchemaTransformer` runs once per generated schema (once per type that appears somewhere in
the document's request/response/parameter shapes) — the right level for correcting or enriching
how a specific C# type is represented:

```csharp
public sealed class EnumDescriptionTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Description = $"One of: {string.Join(", ", Enum.GetNames(context.JsonTypeInfo.Type))}";
        }
        return Task.CompletedTask;
    }
}
```

## Generating a schema for an arbitrary type inside a transformer

Since .NET 10, `context.GetOrCreateSchemaAsync(type)` (available in document, operation, and schema
transformer contexts) generates or reuses the schema for a given .NET type using the same
generation logic the library uses for endpoint parameters/responses — use this instead of manually
constructing an `OpenApiSchema` by hand when a transformer needs to reference another type's schema
(e.g. adding an example object that should match a DTO's actual generated shape):

```csharp
var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
```

## Ordering

Transformers of the same kind run in the order they're registered. A later document transformer
sees the document as modified by every document transformer registered before it — order matters
when two transformers touch the same part of the document (e.g. one sets `Info.Title` and a later
one appends to it).
