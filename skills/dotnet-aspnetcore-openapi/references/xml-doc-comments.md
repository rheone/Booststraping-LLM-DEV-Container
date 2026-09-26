# XML Doc Comment Integration

`Microsoft.AspNetCore.OpenApi` can pull `<summary>`, `<remarks>`, and `<param>` XML documentation
comments directly into the generated document's operation descriptions and parameter descriptions,
so a project that already documents its handlers/actions with XML comments doesn't need to
duplicate that text as separate OpenAPI-specific attributes.

## Enabling XML documentation generation

The project must actually emit an XML documentation file for the library to read from — set this
in the project file:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

`CS1591` ("missing XML comment for publicly visible type or member") fires for every public
member lacking a doc comment once `GenerateDocumentationFile` is on — suppress it project-wide
unless the project intends to enforce complete XML documentation coverage as a separate policy;
otherwise every undocumented public member becomes a build warning unrelated to OpenAPI generation.

## Writing doc comments the generator picks up

Standard C# XML doc comments on a minimal API handler method or a controller action:

```csharp
/// <summary>
/// Retrieves an order by its identifier.
/// </summary>
/// <remarks>
/// Returns 404 if no order with the given id exists.
/// </remarks>
/// <param name="id">The order's unique identifier.</param>
app.MapGet("/orders/{id}", (string id) => Results.Ok(GetOrder(id)));
```

- **`<summary>`** maps to the operation's `summary` field.
- **`<remarks>`** maps to the operation's `description` field.
- **`<param name="...">`** maps to the matching parameter's `description` field, matched by
  parameter name.

## A minimal API handler needs a named delegate or a documented local method for comments to attach

XML doc comments only attach to a declared member (a method), not to an anonymous lambda passed
inline — a doc comment placed directly above an inline lambda in `MapGet(...)` does not get picked
up the way it does above a named method. Extract the handler into a local function, a static
method, or a method group when you want XML comments to reach the generated document:

```csharp
/// <summary>Retrieves an order by its identifier.</summary>
/// <param name="id">The order's unique identifier.</param>
static IResult GetOrderHandler(string id) => Results.Ok(GetOrder(id));

app.MapGet("/orders/{id}", GetOrderHandler);
```

## Doc comments on request/response DTOs flow into schema descriptions

XML comments on a type's properties also flow into the generated schema's per-property
descriptions, independent of the operation-level comments above:

```csharp
public sealed class CreateOrderRequest
{
    /// <summary>The customer placing the order.</summary>
    public string CustomerId { get; set; } = "";

    /// <summary>The order total, in the account's default currency.</summary>
    public decimal Total { get; set; }
}
```

## Doc comments from a referenced project/assembly

The generator only reads XML documentation from assemblies that actually ship an XML doc file
alongside their DLL. If request/response types live in a separate class library project, that
project also needs `GenerateDocumentationFile` enabled and its `.xml` file needs to be present next
to its `.dll` at run time (the default SDK build/publish behavior already copies it alongside the
assembly) — a missing doc file for a referenced assembly means that assembly's types show up in
the document with no descriptions, silently, rather than an error.
