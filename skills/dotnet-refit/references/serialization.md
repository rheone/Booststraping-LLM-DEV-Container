# Serialization

Refit serializes `[Body]` parameters and deserializes response bodies through a pluggable
`IHttpContentSerializer`, configured via `RefitSettings.ContentSerializer`.

## Default: System.Text.Json

`SystemTextJsonContentSerializer` is the default; without any settings, Refit uses
`JsonSerializerOptions` equivalent to `System.Text.Json`'s own library defaults (not the ASP.NET
Core web defaults — case-sensitive property matching, PascalCase property names expected unless you
configure otherwise). Pass your own options to align with your API's casing:

```csharp
var settings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web)), // camelCase, case-insensitive
};

var api = RestService.For<IUsersApi>("https://api.example.com", settings);
```

## Newtonsoft.Json

Needed for `JsonPatchDocument<T>` bodies, `JsonConverter` types written against `Newtonsoft.Json`,
or an existing codebase standardized on it. Requires the separate `Refit.Newtonsoft.Json` package:

```csharp
var settings = new RefitSettings
{
    ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
    }),
};
```

## XML

The `Refit.Xml` package provides `XmlContentSerializer` for APIs that speak XML instead of JSON —
same `RefitSettings.ContentSerializer` slot, swapped for the XML implementation.

## Per-property control

Apply the serializer's own attributes to your DTOs as usual — `[JsonPropertyName]` for
System.Text.Json, `[JsonProperty]` for Newtonsoft.Json. Refit does not introduce its own mapping
attribute layer on top; whatever the configured `ContentSerializer` understands is what governs
DTO shape.

## Query string parameter formatting

Non-body parameter-to-string conversion (route placeholders, query parameters) goes through
`RefitSettings.UrlParameterFormatter`, a separate concern from body serialization. The default
formats `DateTime` with round-trip `"o"` formatting and enums by name; supply a custom
`IUrlParameterFormatter` if an API expects a different date or enum format on the wire.
