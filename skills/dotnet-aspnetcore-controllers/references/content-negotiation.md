# Content Negotiation

Content negotiation selects the response body format based on what the client asks for (via the
`Accept` header) and what the server is configured to produce — controllers negotiate this
automatically through the configured output formatters, without the action needing to serialize the
response itself.

## Default behavior: JSON only

By default, `AddControllers()` registers only a JSON output formatter (`System.Text.Json`-based) — an
action returning `Ok(order)` serializes `order` as JSON regardless of what `Accept` header the client
sends, because there is no alternative formatter registered to negotiate toward:

```csharp
builder.Services.AddControllers();
```

## Adding XML output support

Registering the XML formatter makes negotiation actually meaningful — a client sending
`Accept: application/xml` now gets an XML response, while one sending
`Accept: application/json` (or no `Accept` header) still gets JSON:

```csharp
builder.Services.AddControllers()
    .AddXmlSerializerFormatters();
```

Adding a second formatter is what turns content negotiation from a no-op into actual format
selection — with only one formatter registered, every response uses it regardless of the request's
`Accept` header.

## Producing a specific format for one action

`[Produces("application/json")]` on a controller or action restricts what that action can respond
with, overriding whatever the client's `Accept` header requested:

```csharp
[Produces("application/json")]
[HttpGet]
public IActionResult List() => Ok(GetOrders());
```

Use this when an action's response format is a deliberate, fixed API contract rather than something
that should vary per client request.

## Declaring possible response types for documentation

`[ProducesResponseType]` doesn't affect actual negotiation or serialization — it documents, for
OpenAPI generation and for readers of the code, which status codes and body shapes an action can
return:

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType<Order>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public IActionResult GetById(Guid id)
{
    var order = repository.Find(id);
    return order is not null ? Ok(order) : NotFound();
}
```

Declaring every realistic response shape here keeps generated OpenAPI documentation accurate — an
action's actual runtime behavior (what `Ok(...)`/`NotFound()`/etc. it returns) isn't otherwise visible
to the generator without either these attributes or a `Task<ActionResult<T>>` return type that
communicates the primary success shape.

## Accepting a request body format other than JSON

Input formatters mirror output formatters for the request side — `AddXmlSerializerFormatters()`
registers both directions at once, letting an action bound with `[FromBody]` also deserialize an XML
request body if the client sends `Content-Type: application/xml`. A request whose `Content-Type`
doesn't match any registered input formatter fails model binding for that parameter, which (under
`[ApiController]`) surfaces as the automatic 400 response covered in
[model-validation.md](model-validation.md).
