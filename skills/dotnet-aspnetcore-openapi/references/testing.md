# Testing

The output this library produces is a document (JSON), not application behavior in the usual
sense — testing it means asserting on the generated document's shape, not on HTTP responses from
the endpoints it describes.

## Retrieving the generated document in a test

Use `WebApplicationFactory<TEntryPoint>` (from `Microsoft.AspNetCore.Mvc.Testing`) to spin up the
app in-memory and request the document from its actual served endpoint — this exercises the real
generation pipeline, including every registered transformer, rather than trying to invoke document
generation APIs directly and out of context:

```csharp
[Fact]
public async Task OpenApiDocument_IncludesGetOrderOperation()
{
    await using var factory = new WebApplicationFactory<Program>();
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/openapi/v1.json");
    response.EnsureSuccessStatusCode();

    var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
    var paths = document!.RootElement.GetProperty("paths");

    paths.TryGetProperty("/orders/{id}", out _).Should().BeTrue();
}
```

## Asserting a transformer's effect

For a document or operation transformer with real logic worth testing (a conditional deprecation
flag, a conditionally added security scheme), assert on the specific field the transformer sets,
using the same in-memory-document approach above rather than unit-testing the transformer class in
isolation — a transformer's effect is only meaningful in the context of the actual generated
document it modifies:

```csharp
[Fact]
public async Task OpenApiDocument_MarksObsoleteEndpointAsDeprecated()
{
    await using var factory = new WebApplicationFactory<Program>();
    using var client = factory.CreateClient();

    var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
    var operation = document!.RootElement
        .GetProperty("paths").GetProperty("/orders/legacy")
        .GetProperty("get");

    operation.GetProperty("deprecated").GetBoolean().Should().BeTrue();
}
```

## Testing that a schema matches an expected shape

Assert on a specific schema under `components.schemas` when a DTO's generated shape (property
names, required fields, nested types) is a contract worth protecting against accidental breaking
changes — particularly useful as a regression test after adding or renaming a DTO property:

```csharp
[Fact]
public async Task OpenApiDocument_OrderSchema_HasExpectedProperties()
{
    await using var factory = new WebApplicationFactory<Program>();
    using var client = factory.CreateClient();

    var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
    var schema = document!.RootElement
        .GetProperty("components").GetProperty("schemas").GetProperty("Order");

    schema.GetProperty("properties").TryGetProperty("total", out _).Should().BeTrue();
}
```

## What this does not need

Testing the endpoints' own runtime behavior (does `GET /orders/{id}` actually return the right
order) belongs to the application's ordinary integration test suite and has nothing to do with
OpenAPI generation — keep document-shape assertions separate from behavioral endpoint tests so a
failing document assertion clearly points at a documentation regression, not a behavioral one.
